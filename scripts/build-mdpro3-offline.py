#!/usr/bin/env python3
"""Build Yu-Gi-Oh! Fight with the preserved MDPro3 duel runtime atomically."""
from __future__ import annotations

from contextlib import contextmanager
import fcntl
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import plistlib
import re
import shutil
import subprocess
import sys
import tempfile

from source_build_state import atomic_exchange, validate_generation, validate_source_inputs
from native_server_host import HOST_PATH, compile_host, verify_host
from native_file_browser import (LIBRARY_PATH as FILE_BROWSER_PATH, MANAGED_LINK as FILE_BROWSER_LINK,
                                 LINK_TARGET as FILE_BROWSER_LINK_TARGET, compile_file_browser, verify_file_browser)
from client_ui_assets import content_hashes as client_ui_hashes

ROOT = Path(__file__).resolve().parents[1]
NAME = "Yu-Gi-Oh! Fight"
IDENTIFIER = "io.github.keriae.yugiohfight"
EXECUTABLE_NAME = "YuGiOhFight"
LAUNCHER_SOURCE = "Sources/IndependentGameBootstrap.swift"
LAUNCHER_ARCHITECTURES = ("arm64", "x86_64")
LAUNCHER_SWIFT_VERSION = "6"
LAUNCHER_MINIMUM_MACOS = "13.0"
RELEASE_DOCUMENTS = (
    "README.md", "THIRD_PARTY_NOTICES.md", "COPYRIGHT.md", "docs/BUILDING.md", "docs/ASSETS.md", "native/README.md",
    "docs/media/preview.jpg", "docs/media/launch-trailer.mp4", "docs/media/launch-trailer.srt",
    "docs/media/full-duel.mp4",
)
RELEASE_LICENSES = (
    ("vendor/Mono-LICENSE.txt", "Mono-LICENSE.txt"),
    ("content/ai/tokenizers/LICENSE.tiktoken", "LICENSE.tiktoken"),
    ("native/StandaloneFileBrowser/LICENSE.txt", "StandaloneFileBrowser-LICENSE.txt"),
)


def file_digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def run(*args: str | Path) -> None:
    subprocess.run([str(arg) for arg in args], cwd=ROOT, check=True)


def launcher_build_record() -> dict:
    # Keep this source record inside the signed bundle. A hash of the signed
    # main executable would become stale when the outer app is signed again.
    return {"schema": 1, "source_path": LAUNCHER_SOURCE,
            "source_sha256": file_digest(ROOT / LAUNCHER_SOURCE),
            "builder_sha256": file_digest(Path(__file__).resolve()),
            "architectures": list(LAUNCHER_ARCHITECTURES),
            "swift_version": LAUNCHER_SWIFT_VERSION,
            "minimum_macos": LAUNCHER_MINIMUM_MACOS}


def package_documents(resources: Path) -> None:
    """Only install public documentation and retained component notices."""
    docs = resources / "docs"
    if docs.exists():
        shutil.rmtree(docs)
    for relative in RELEASE_DOCUMENTS:
        target = resources / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative, target)
    licenses = resources / "licenses"
    licenses.mkdir(exist_ok=True)
    for relative, name in RELEASE_LICENSES:
        shutil.copy2(ROOT / relative, licenses / name)


def copy_client(source: Path, destination: Path) -> None:
    source = source.resolve(strict=True)
    if not source.is_dir():
        raise ValueError("The client copy source must be a directory.")
    if destination.exists() or destination.is_symlink():
        raise ValueError("Client staging must start at an absent path; refusing to merge or replace existing files.")
    # APFS clones preserve bytes while avoiding a second physical copy of large assets.
    cloned = subprocess.run(["cp", "-cR", str(source), str(destination)],
                            stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
    if cloned.returncode:
        if destination.is_symlink():
            raise ValueError("The client copy did not produce an independent staging directory.")
        if destination.exists():
            shutil.rmtree(destination)
        run("ditto", source, destination)
    if destination.is_symlink() or not destination.is_dir():
        raise ValueError("The client copy did not produce an independent staging directory.")


def package_client_ui(client: Path, manifest: dict) -> dict:
    """Bind each new menu image/font to an exact packaged path and digest."""
    source = ROOT / "content/client-ui"
    hashes = client_ui_hashes(source)
    if hashes != manifest.get("client_ui_content"):
        raise ValueError("Client UI content changed after compilation; rebuild first.")
    changes = []
    for relative, expected in hashes.items():
        path = source / relative
        target = client / "Data/ClientUI" / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(path, target)
        if file_digest(target) != expected:
            raise ValueError("Client UI asset changed during packaging: " + relative)
        changes.append({"path": target.relative_to(client).as_posix(), "sha256": expected})
    return {"ok": True, "files": len(changes), "changes": changes}


@contextmanager
def application_lock(support: Path):
    support.mkdir(parents=True, exist_ok=True)
    with (support / ".native-game-lock").open("a+") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise SystemExit("Close " + NAME + " before replacing its application bundle.")
        try:
            yield
        finally:
            fcntl.flock(lock, fcntl.LOCK_UN)


def check_swift_toolchain() -> None:
    message = "The macOS launcher requires a Swift 6 or later compiler, such as Xcode 16+ Command Line Tools; check xcrun swiftc --version"
    try:
        result = subprocess.run(["xcrun", "swiftc", "--version"], capture_output=True,
                                text=True, check=True, timeout=15)
    except (OSError, subprocess.SubprocessError) as error:
        raise ValueError(message) from error
    version = re.search(r"\bSwift version (\d+)", result.stdout + result.stderr, re.IGNORECASE)
    if version is None or int(version[1]) < int(LAUNCHER_SWIFT_VERSION):
        raise ValueError(message)


def validate_release_inputs(app: Path, snapshot: Path, manifest: dict, host: dict, file_browser: dict, launcher: dict) -> None:
    """Check editable build inputs again immediately before publication."""
    if manifest.get("build_profile") != "release" or "FIGHT_UI_PROOF" in manifest.get("compiler_defines", []):
        raise ValueError("Only the release profile may be published; rerun python3 build.py.")
    validate_source_inputs(ROOT, manifest.get("source_files"))
    resources = app / "Contents/Resources"
    client = resources / "Client"
    try:
        if validate_generation(snapshot) != manifest or json.loads((resources / "Source-Build.json").read_text()) != manifest:
            raise ValueError("Compiled source evidence changed during packaging; rerun python3 build.py.")
        spec = importlib.util.spec_from_file_location("final_academy_inventory", Path(__file__).with_name("seed-deck-academy.py"))
        academy = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(academy)
        for source, packaged, expected, inventory in (
                (ROOT / "content/client-ui", client / "Data/ClientUI", manifest.get("client_ui_content"), client_ui_hashes),
                (ROOT / "content/deck-academy", client / "Data/DeckAcademy", manifest.get("deck_academy_content"), academy.content_hashes)):
            if inventory(source) != expected or inventory(packaged) != expected:
                raise ValueError("Client content changed during packaging; rerun python3 build.py.")
        for relative in RELEASE_DOCUMENTS:
            if file_digest(ROOT / relative) != file_digest(resources / relative):
                raise ValueError("Release documentation changed during packaging; rerun python3 build.py: " + relative)
        for relative, name in RELEASE_LICENSES:
            if file_digest(ROOT / relative) != file_digest(resources / "licenses" / name):
                raise ValueError("Release license changed during packaging; rerun python3 build.py: " + relative)
        if launcher_build_record() != launcher or json.loads((resources / "Launcher-Build.json").read_text()) != launcher:
            raise ValueError("Launcher inputs changed during packaging; rerun python3 build.py.")
        try:
            verify_host(client, host)
            verify_file_browser(client, file_browser)
        except ValueError as error:
            raise ValueError("Native build inputs changed during packaging; rerun python3 build.py.") from error
    except (OSError, KeyError, subprocess.SubprocessError) as error:
        raise ValueError("Release inputs changed or became unavailable during packaging; rerun python3 build.py.") from error


def publish_application(app: Path, destination: Path, snapshot: Path, manifest: dict, host: dict, file_browser: dict, launcher: dict) -> None:
    validate_release_inputs(app, snapshot, manifest, host, file_browser, launcher)
    if destination.exists():
        # Keep the prior application until all editable inputs pass verification.
        atomic_exchange(app, destination)
    else:
        os.rename(app, destination)


def main() -> None:
    check_swift_toolchain()
    launcher_record = launcher_build_record()
    (ROOT / "artifacts").mkdir(parents=True, exist_ok=True)
    destination = ROOT / "dist" / (NAME + ".app")
    support = Path.home() / "Library/Application Support" / NAME
    # Reject an active game before compilation or any package work.
    with application_lock(support):
        if destination.exists() and not (destination / "Contents/Resources/.mdpro3-source-build").is_file():
            raise ValueError("Refusing to replace an application not made by this source build.")
        spec = importlib.util.spec_from_file_location("mdpro3_source_builder", ROOT / "scripts/build-mdpro3-source.py")
        assert spec and spec.loader
        builder = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(builder)
        snapshot = builder.build_source()
        manifest_path = snapshot / "manifest.json"
        manifest = validate_generation(snapshot)
        run(sys.executable, ROOT / "scripts/verify-native-client.py", "--output", ROOT / "artifacts/mdpro3-publisher-verification.json")
        destination.parent.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix=".independent-build.", dir=destination.parent) as work:
            stage = Path(work)
            app = stage / (NAME + ".app")
            contents = app / "Contents"
            resources = contents / "Resources"
            resources.mkdir(parents=True)
            (contents / "MacOS").mkdir()
            (resources / ".independent-build").touch()
            (resources / ".mdpro3-source-build").touch()
            client = resources / "Client"
            copy_client(ROOT / "vendor/MDPro3-mac", client)
            run(sys.executable, ROOT / "scripts/prune-staged-client.py", "--client", client,
                "--output", ROOT / "artifacts/mdpro3-staged-inventory.json")
            run(sys.executable, ROOT / "scripts/verify-native-client.py", "--client", client,
                "--output", ROOT / "artifacts/mdpro3-staged-verification.json")
            managed = client / "MDPro3.app/Contents/Resources/Data/Managed"
            for extension in ("dll", "pdb"):
                shutil.copy2(snapshot / ("Assembly-CSharp." + extension), managed)
            for extension, key in (("dll", "assembly_sha256"), ("pdb", "pdb_sha256")):
                if file_digest(managed / ("Assembly-CSharp." + extension)) != manifest[key]:
                    raise ValueError("Packaged source " + extension + " digest mismatch.")
            spec = importlib.util.spec_from_file_location("mdpro3_defaults", ROOT / "scripts/seed-offline-defaults.py")
            assert spec and spec.loader
            defaults = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(defaults)
            default_report = defaults.seed(client, manifest_path)
            spec = importlib.util.spec_from_file_location("mdpro3_academy_seed", ROOT / "scripts/seed-deck-academy.py")
            academy = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(academy)
            academy_report = academy.seed(client, manifest_path)
            ui_report = package_client_ui(client, manifest)
            (ROOT / "artifacts/mdpro3-deck-academy-seed.json").write_text(json.dumps(academy_report, indent=2, ensure_ascii=False) + "\n")
            (ROOT / "artifacts/mdpro3-offline-defaults.json").write_text(json.dumps(default_report, indent=2, ensure_ascii=False) + "\n")
            (client / "Video/Art").mkdir(parents=True, exist_ok=True)
            native = client / "MDPro3.app"
            native_info = native / "Contents/Info.plist"
            info = plistlib.loads(native_info.read_bytes())
            info["CFBundleIdentifier"] = IDENTIFIER
            info["CFBundleName"] = NAME
            info["CFBundleDisplayName"] = NAME
            native_info.write_bytes(plistlib.dumps(info, sort_keys=False))
            shutil.copy2(native / "Contents/Resources/PlayerIcon.icns", resources / "MDPro3.icns")
            # Reuse the exact signed binary also used by temporary duel proofs.
            host_metadata = compile_host(ROOT / "build/ygoserver-host")
            host = client / HOST_PATH
            host.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / "build/ygoserver-host", host)
            verify_host(client, host_metadata)
            file_browser_metadata = compile_file_browser(ROOT / "build/libStandaloneFileBrowser.dylib")
            shutil.copy2(ROOT / "build/libStandaloneFileBrowser.dylib", client / FILE_BROWSER_PATH)
            (client / FILE_BROWSER_LINK).symlink_to(FILE_BROWSER_LINK_TARGET)
            verify_file_browser(client, file_browser_metadata)
            for architecture in LAUNCHER_ARCHITECTURES:
                run("xcrun", "swiftc", "-swift-version", LAUNCHER_SWIFT_VERSION, "-parse-as-library", "-O", "-target",
                    architecture + "-apple-macos" + LAUNCHER_MINIMUM_MACOS, ROOT / LAUNCHER_SOURCE, "-o", stage / architecture)
            run("xcrun", "lipo", "-create", stage / "arm64", stage / "x86_64", "-output", contents / "MacOS" / EXECUTABLE_NAME)
            (contents / "Info.plist").write_bytes(plistlib.dumps({
                "CFBundleExecutable": EXECUTABLE_NAME, "CFBundleIconFile": "MDPro3.icns", "CFBundleIdentifier": IDENTIFIER,
                "CFBundleName": NAME, "CFBundleDisplayName": NAME, "CFBundlePackageType": "APPL", "CFBundleShortVersionString": "0.1.0",
                "CFBundleVersion": "1", "LSMinimumSystemVersion": LAUNCHER_MINIMUM_MACOS, "NSHighResolutionCapable": True,
                "MDPro3StateFolder": NAME,
            }, sort_keys=False))
            shutil.copy2(manifest_path, resources / "Source-Build.json")
            shutil.copy2(snapshot / "compatibility.json", resources / "compatibility.json")
            (resources / "Launcher-Build.json").write_text(json.dumps(launcher_record, indent=2) + "\n")
            package_documents(resources)
            # The copied native bundle's resource signature is stale after replacing
            # its assembly and Info.plist. Sign before dyld loads its Mono runtime.
            run("codesign", "--force", "--sign", "-", "--timestamp=none", native)
            for path in (contents / "MacOS" / EXECUTABLE_NAME, app):
                run("codesign", "--force", "--sign", "-", "--timestamp=none", path)
            run("codesign", "--verify", "--deep", "--strict", app)
            run(sys.executable, ROOT / "scripts/verify-source-runtime.py", "--client", client,
                "--assembly", managed / "Assembly-CSharp.dll", "--manifest", manifest_path,
                "--output", ROOT / "artifacts/mdpro3-source-runtime-packaged.json")
            verify_host(client, host_metadata)
            verify_file_browser(client, file_browser_metadata)
            publisher_manifest = ROOT / "vendor/MDPro3-mac-metadata/checksums.txt"
            publisher = {name: value for value, name in
                         (line.split("  ", 1) for line in publisher_manifest.read_text().splitlines())}
            permitted = {
                "MDPro3.app/Contents/Info.plist", "MDPro3.app/Contents/MacOS/MDPro3",
                "MDPro3.app/Contents/_CodeSignature/CodeResources",
                "MDPro3.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll",
                "MDPro3.app/Contents/Resources/Data/Managed/Assembly-CSharp.pdb",
                "Data/config.conf",
                HOST_PATH,
                FILE_BROWSER_PATH, FILE_BROWSER_LINK,
            } | {item["path"] for item in default_report["changes"]} | {item["path"] for item in academy_report["changes"]} | {item["path"] for item in ui_report["changes"]}
            changes = {}
            for path in sorted(client.rglob("*")):
                if not path.is_file():
                    continue
                relative = path.relative_to(client).as_posix()
                actual = file_digest(path)
                before = publisher.get(relative)
                if actual != before:
                    if relative not in permitted:
                        raise ValueError("Unexpected packaged resource change: " + relative)
                    changes[relative] = {"publisher_sha256": before, "sha256": actual}
            change_report = {"ok": True, "source_build_sha256": file_digest(manifest_path),
                             "publisher_manifest_sha256": file_digest(publisher_manifest), "changes": changes,
                             "native_server_host": host_metadata, "native_file_browser": file_browser_metadata,
                             "client_ui": ui_report}
            (resources / "Client-Changes.json").write_text(json.dumps(change_report, indent=2) + "\n")
            (ROOT / "artifacts/mdpro3-client-changes.json").write_text(json.dumps(change_report, indent=2) + "\n")
            # The final change inventory is part of the outer application's signed resources.
            run("codesign", "--force", "--sign", "-", "--timestamp=none", app)
            run("codesign", "--verify", "--deep", "--strict", app)
            publish_application(app, destination, snapshot, manifest, host_metadata, file_browser_metadata, launcher_record)
    report = {"ok": True, "application": str(destination), "source_compiled": True,
              "assembly_sha256": manifest["assembly_sha256"], "publisher_version": "1.4.7-2", "state_folder": str(support),
              "native_server_host": host_metadata, "native_file_browser": file_browser_metadata}
    (ROOT / "artifacts/mdpro3-offline-build.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, ensure_ascii=False))


if __name__ == "__main__":
    main()
