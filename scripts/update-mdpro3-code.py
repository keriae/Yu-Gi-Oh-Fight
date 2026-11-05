#!/usr/bin/env python3
"""Atomically refresh only compiled game code in an existing verified macOS app.

Compile successfully with build-mdpro3-source.py first. Resource/dependency,
launcher, or native-host changes require the complete offline builder instead.
"""
from __future__ import annotations

import copy
from contextlib import contextmanager
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import plistlib
import shutil
import signal
import subprocess
import sys
import tempfile

from native_server_host import HOST_PATH, verify_host
from source_build_state import atomic_exchange, game_source_paths, source_snapshot, validate_generation
from client_ui_assets import content_hashes as client_ui_hashes

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "dist/Yu-Gi-Oh! Fight.app"
ARTIFACTS = ROOT / "artifacts"
SUPPORT = Path.home() / "Library/Application Support/Yu-Gi-Oh! Fight"
MANAGED = "MDPro3.app/Contents/Resources/Data/Managed"
MUTABLE_CLIENT_FILES = (
    MANAGED + "/Assembly-CSharp.dll",
    MANAGED + "/Assembly-CSharp.pdb",
    "MDPro3.app/Contents/MacOS/MDPro3",
    "MDPro3.app/Contents/_CodeSignature/CodeResources",
)


class IncompleteRollback(RuntimeError):
    """Keep the staging directory when automatic recovery could not finish."""


@contextmanager
def update_workspace():
    work = Path(tempfile.mkdtemp(prefix=".mdpro3-code-update.", dir=APP.parent))
    preserve = False
    try:
        yield work
    except IncompleteRollback:
        preserve = True
        raise
    finally:
        if not preserve and not (work / ".preserve-recovery").exists():
            shutil.rmtree(work)


@contextmanager
def deferred_publication_signals():
    # Deliver ordinary termination signals only after exchange/publication (or
    # rollback) completes, including the gap between a swap and its state flag.
    previous = signal.pthread_sigmask(signal.SIG_BLOCK, {signal.SIGINT, signal.SIGTERM, signal.SIGHUP})
    try:
        yield
    finally:
        signal.pthread_sigmask(signal.SIG_SETMASK, previous)


def require(value: bool, message: str) -> None:
    if not value:
        raise ValueError(message)


def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def read(path: Path) -> dict:
    result = json.loads(path.read_text())
    require(isinstance(result, dict), "Expected an object: " + path.name)
    return result


def run(*arguments: str | Path) -> None:
    subprocess.run([str(value) for value in arguments], cwd=ROOT, check=True)


def signature(app: Path) -> None:
    run("codesign", "--verify", "--deep", "--strict", app)


def bound_file(root: Path, relative: str) -> Path:
    value = Path(relative)
    require(not value.is_absolute() and ".." not in value.parts, "Unsafe inventory path")
    path = root / value
    require(path.resolve(strict=True).is_relative_to(root.resolve(strict=True)), "Inventory leaves application")
    return path


def current_sources() -> dict:
    return {path.relative_to(ROOT / "game").as_posix(): digest(path)
            for path in game_source_paths(ROOT)}


def source_and_resources(snapshot: Path, manifest: dict, client: Path) -> None:
    require("FIGHT_UI_PROOF" not in manifest.get("compiler_defines", []), "UI proof builds cannot be packaged; compile the release profile first")
    require(validate_generation(snapshot) == manifest, "Compiled source generation changed")
    require(current_sources() == manifest["source_files"], "Compile the current game sources before updating")
    managed = client / MANAGED
    dependencies = {path.name: digest(path) for path in managed.glob("*.dll")
                    if path.name != "Assembly-CSharp.dll"}
    expected = {name: value for name, value in manifest["shipping_dependencies"].items()
                if name != "Assembly-CSharp.dll"}
    require(dependencies == expected, "Packaged managed dependencies changed; use the complete builder")
    content = manifest["deck_academy_content"]
    actual_paths = {path.relative_to(client / "Data/DeckAcademy").as_posix()
                    for path in (client / "Data/DeckAcademy").rglob("*") if path.is_file()}
    require(actual_paths == set(content), "Packaged academy inventory changed; use the complete builder")
    for relative, expected_sha in content.items():
        require(digest(bound_file(ROOT / "content/deck-academy", relative)) == expected_sha
                and digest(bound_file(client / "Data/DeckAcademy", relative)) == expected_sha,
                "Academy content changed; use the complete builder: " + relative)
    require(client_ui_hashes(ROOT / "content/client-ui") == manifest.get("client_ui_content")
            and client_ui_hashes(client / "Data/ClientUI") == manifest.get("client_ui_content"),
            "Client UI resources changed; use the complete builder")


def verify_launcher(app: Path, expected: dict) -> None:
    record = app / "Contents/Resources/Launcher-Build.json"
    require(record.is_file(), "Launcher build record is missing; use the complete builder")
    require(read(record) == expected, "Launcher sources or build settings changed; use the complete builder")


def verify_package(app: Path, manifest: dict, changes: dict, launcher: dict) -> None:
    resources = app / "Contents/Resources"
    client = resources / "Client"
    require(app.is_dir() and not app.is_symlink()
            and (resources / ".mdpro3-source-build").is_file()
            and (resources / ".independent-build").is_file(), "Existing application is not a project source build")
    verify_launcher(app, launcher)
    require(changes.get("ok") is True and manifest.get("ok") is True
            and manifest.get("source_compiled") is True and manifest.get("compatibility_verified") is True,
            "Existing package manifests do not report a verified source build")
    require(changes.get("source_build_sha256") == digest(resources / "Source-Build.json")
            and changes.get("publisher_manifest_sha256") == manifest["publisher_manifest_sha256"],
            "Existing change inventory does not match its build")
    publisher_path = ROOT / "vendor/MDPro3-mac-metadata/checksums.txt"
    require(digest(publisher_path) == manifest["publisher_manifest_sha256"],
            "Publisher baseline changed; use the complete builder")
    publisher = {name: value for value, name in
                 (line.split("  ", 1) for line in publisher_path.read_text().splitlines())}
    for extension, key in (("dll", "assembly_sha256"), ("pdb", "pdb_sha256")):
        require(digest(bound_file(client, MANAGED + "/Assembly-CSharp." + extension)) == manifest[key],
                "Existing compiled code does not match its manifest")
    require(digest(resources / "compatibility.json") == manifest["compatibility_report_sha256"],
            "Existing compatibility report does not match its manifest")
    for relative, record in changes["changes"].items():
        require(record["publisher_sha256"] == publisher.get(relative), "Change baseline differs: " + relative)
        require(digest(bound_file(client, relative)) == record["sha256"], "Existing change differs: " + relative)
    require(not any(client.rglob("credentials.json*")), "Application bundle contains private credentials")
    require(changes["changes"].get(HOST_PATH, {}).get("sha256") == changes["native_server_host"]["sha256"],
            "Native host inventory is incomplete")
    verify_host(client, changes["native_server_host"])
    signature(app)


def publish_evidence(app: Path, destination: Path, work: Path, payloads: dict[Path, bytes]) -> None:
    """Publish the bundle and evidence together, restoring the old state on failure."""
    previous = {}
    prepared = {}
    for number, (output, data) in enumerate(payloads.items()):
        require(not output.is_symlink(), "Refusing to replace linked update evidence")
        previous[output] = output.read_bytes() if output.exists() else None
        staged = work / ("evidence-" + str(number) + ".json")
        staged.write_bytes(data)
        prepared[output] = staged
    backups = {}
    for number, (output, data) in enumerate(previous.items()):
        backup = work / ("previous-evidence-" + str(number) + ".json")
        if data is not None:
            backup.write_bytes(data)
        backups[str(output)] = str(backup) if data is not None else None
    (work / "recovery.json").write_bytes(encoded({"staged_application": str(app),
                                                "destination_application": str(destination),
                                                "previous_evidence": backups}))
    recovery_marker = work / ".preserve-recovery"
    recovery_marker.touch()
    with deferred_publication_signals():
        exchanged = False
        changed = []
        try:
            atomic_exchange(app, destination)
            exchanged = True
            for output, staged in prepared.items():
                changed.append(output)
                os.replace(staged, output)
            recovery_marker.unlink()
        except BaseException:
            try:
                if exchanged:
                    atomic_exchange(app, destination)
                for output in changed:
                    data = previous[output]
                    if data is None:
                        output.unlink(missing_ok=True)
                    else:
                        restored = work / ("restore-" + output.name)
                        restored.write_bytes(data)
                        os.replace(restored, output)
                recovery_marker.unlink()
            except BaseException as rollback_error:
                raise IncompleteRollback("Automatic rollback could not finish. Application and evidence backups "
                                         "are retained at " + str(work)) from rollback_error
            raise


def encoded(value: dict) -> bytes:
    return (json.dumps(value, ensure_ascii=False, indent=2) + "\n").encode()


def main() -> None:
    specification = importlib.util.spec_from_file_location("offline_app_builder", ROOT / "scripts/build-mdpro3-offline.py")
    builder = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(builder)
    with builder.application_lock(SUPPORT):
        snapshot = source_snapshot(ROOT)
        manifest = validate_generation(snapshot)
        resources = APP / "Contents/Resources"
        previous = read(resources / "Source-Build.json")
        changes = read(resources / "Client-Changes.json")
        launcher = builder.launcher_build_record()
        verify_package(APP, previous, changes, launcher)
        for key in ("publisher_manifest_sha256", "publisher_version", "source_commit", "source_archive_manifest_sha256",
                    "shipping_dependencies", "compiler_defines", "deck_academy_content", "client_ui_content"):
            require(manifest[key] == previous[key], "Code-only update cannot change " + key)
        source_and_resources(snapshot, manifest, resources / "Client")
        ARTIFACTS.mkdir(exist_ok=True)
        with update_workspace() as work:
            app = work / APP.name
            builder.copy_client(APP, app)
            contents = app / "Contents"
            updated = contents / "Resources"
            client = updated / "Client"
            require(digest(contents / "MacOS" / builder.EXECUTABLE_NAME) == digest(APP / "Contents/MacOS" / builder.EXECUTABLE_NAME),
                    "Cloned launcher code differs from the installed application")
            for extension in ("dll", "pdb"):
                shutil.copy2(snapshot / ("Assembly-CSharp." + extension), client / MANAGED)
            shutil.copy2(snapshot / "manifest.json", updated / "Source-Build.json")
            shutil.copy2(snapshot / "compatibility.json", updated / "compatibility.json")
            builder.package_documents(updated)
            info_path = contents / "Info.plist"
            info = plistlib.loads(info_path.read_bytes())
            require(info.get("CFBundleIdentifier") == builder.IDENTIFIER
                    and info.get("CFBundleExecutable") == builder.EXECUTABLE_NAME, "Unexpected application identity")
            version = str(info["CFBundleVersion"])
            require(version.isdigit(), "Non-numeric bundle version needs the complete builder")
            info["CFBundleVersion"] = str(int(version) + 1)
            info_path.write_bytes(plistlib.dumps(info, sort_keys=False))
            runtime_path = work / "source-runtime.json"
            run(sys.executable, ROOT / "scripts/verify-source-runtime.py", "--client", client,
                "--assembly", client / MANAGED / "Assembly-CSharp.dll", "--manifest", snapshot / "manifest.json",
                "--output", runtime_path)
            runtime = read(runtime_path)
            require(runtime.get("ok") is True, "Staged runtime checks failed")
            run("codesign", "--force", "--sign", "-", "--timestamp=none", client / "MDPro3.app")
            updated_changes = copy.deepcopy(changes)
            updated_changes["source_build_sha256"] = digest(updated / "Source-Build.json")
            for relative in MUTABLE_CLIENT_FILES:
                require(relative in updated_changes["changes"], "Missing existing code/signature inventory entry")
                updated_changes["changes"][relative]["sha256"] = digest(bound_file(client, relative))
            (updated / "Client-Changes.json").write_bytes(encoded(updated_changes))
            run("codesign", "--force", "--sign", "-", "--timestamp=none", contents / "MacOS" / builder.EXECUTABLE_NAME)
            run("codesign", "--force", "--sign", "-", "--timestamp=none", app)
            source_and_resources(snapshot, manifest, client)
            require(builder.launcher_build_record() == launcher, "Launcher inputs changed during the update")
            verify_package(app, manifest, updated_changes, launcher)
            runtime["publication"] = {"application": str(APP), "verified_in_staging": True}
            report = {"ok": True, "kind": "source-code-only-update", "application": str(APP),
                      "source_compiled": True, "assembly_sha256": manifest["assembly_sha256"],
                      "previous_assembly_sha256": previous["assembly_sha256"],
                      "source_file_count": manifest["source_file_count"], "bundle_version": info["CFBundleVersion"],
                      "publisher_version": manifest["publisher_version"], "state_folder": str(SUPPORT),
                      "native_server_host": updated_changes["native_server_host"], "deep_strict_signature_verified": True,
                      "resources_and_dependencies_retained": True}
            publish_evidence(app, APP, work, {
                ARTIFACTS / "mdpro3-source-runtime-packaged.json": encoded(runtime),
                ARTIFACTS / "mdpro3-client-changes.json": encoded(updated_changes),
                ARTIFACTS / "mdpro3-offline-build.json": encoded(report),
                ARTIFACTS / "mdpro3-code-update.json": encoded(report),
            })
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
