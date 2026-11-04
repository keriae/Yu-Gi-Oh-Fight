#!/usr/bin/env python3
"""Restore the pinned MDPro3 authoring tools from verified, reusable archives."""
from __future__ import annotations

import argparse
import contextlib
import fcntl
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import shutil
import stat
import subprocess
import tarfile
import tempfile
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_CACHE = Path.home() / "Library/Caches/MDPro3/source-toolchain"


def digest(path: Path, algorithm: str) -> str:
    value = hashlib.new(algorithm)
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def relative_path(name: str) -> str:
    path = PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or "\\" in name or ":" in name:
        raise ValueError("Unsafe archive or metadata path: " + name)
    return path.as_posix()


def current_rid() -> str:
    machine = platform.machine().lower()
    if platform.system() != "Darwin" or machine not in ("arm64", "aarch64", "x86_64", "amd64"):
        raise ValueError("The source toolchain supports macOS arm64 and x64.")
    return "osx-arm64" if machine in ("arm64", "aarch64") else "osx-x64"


def build_specs(metadata: dict, rid: str) -> list[dict]:
    if metadata.get("schema_version") != 1:
        raise ValueError("Unsupported source toolchain metadata schema")
    sdk, cecil = metadata["dotnet_sdk"], metadata["mono_cecil"]
    if rid not in sdk["archives"]:
        raise ValueError("No pinned SDK archive for " + rid)
    archive = sdk["archives"][rid]
    result = []
    for name, kind, item, package, algorithm in (
            ("dotnet_sdk", "tar", sdk, archive, "sha512"),
            ("mono_cecil", "zip", cecil, cecil, "sha256")):
        value = package[algorithm]
        if len(value) != hashlib.new(algorithm).digest_size * 2 or any(char not in "0123456789abcdef" for char in value):
            raise ValueError("Invalid pinned " + algorithm + " for " + name)
        archive_name = relative_path(package["archive_name"])
        if "/" in archive_name or archive_name == ".":
            raise ValueError("Archive cache name must be a filename")
        install_path = relative_path(item["install_path"])
        if install_path == "." or not install_path.startswith(".tools/"):
            raise ValueError("Tool installations must be under .tools")
        result.append({"name": name, "kind": kind, "version": item["version"],
                       "install_path": install_path, "required_files": [relative_path(path) for path in item["required_files"]],
                       "url": package.get("url", package.get("source")),
                       "hash_algorithm": algorithm, "hash_value": value, "archive_name": archive_name,
                       "local_archives": [relative_path(path) for path in package.get("local_archives", [])]})
    return result


def cache_archive_path(cache_root: Path, spec: dict) -> Path:
    return cache_root / "archives" / spec["hash_algorithm"] / spec["hash_value"] / spec["archive_name"]


@contextlib.contextmanager
def exclusive_lock(path: Path):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a") as stream:
        fcntl.flock(stream, fcntl.LOCK_EX)
        yield


def download_archive(url: str, destination: Path) -> None:
    if not url or not url.startswith("https://"):
        raise ValueError("Toolchain downloads require a pinned HTTPS URL")
    request = urllib.request.Request(url, headers={"User-Agent": "MDPro3-Source-Toolchain/1"})
    with urllib.request.urlopen(request, timeout=60) as response, destination.open("wb") as output:
        shutil.copyfileobj(response, output, 1024 * 1024)


def verified(path: Path, spec: dict) -> bool:
    return path.is_file() and digest(path, spec["hash_algorithm"]) == spec["hash_value"]


def publish_archive(source: Path | None, cache: Path, spec: dict, downloader) -> None:
    # A cache entry is replaced only after its replacement passes the pinned hash.
    with exclusive_lock(cache.parent / ".archive.lock"):
        if verified(cache, spec):
            return
        descriptor, name = tempfile.mkstemp(prefix=".download-", suffix=".part", dir=cache.parent)
        os.close(descriptor)
        temporary = Path(name)
        try:
            if source is not None:
                with source.open("rb") as stream, temporary.open("wb") as output:
                    shutil.copyfileobj(stream, output, 1024 * 1024)
            else:
                downloader(spec["url"], temporary)
            if not verified(temporary, spec):
                raise ValueError("Archive checksum mismatch for " + spec["name"])
            os.replace(temporary, cache)
        finally:
            temporary.unlink(missing_ok=True)


def select_archive(workspace: Path, cache_root: Path, spec: dict, *, check: bool, offline: bool, downloader) -> dict:
    cache = cache_archive_path(cache_root, spec)
    candidates = [("cache", cache)] + [("workspace", workspace / path) for path in spec["local_archives"]]
    issues = []
    for source, candidate in candidates:
        if not candidate.is_file():
            continue
        if not verified(candidate, spec):
            issues.append({"path": str(candidate), "reason": "Pinned archive checksum mismatch"})
            continue
        if not check and source != "cache":
            publish_archive(candidate, cache, spec, downloader)
            candidate = cache
        return {"status": "verified", "path": str(candidate), "source": source, "issues": issues}
    if not check and not offline:
        try:
            publish_archive(None, cache, spec, downloader)
            return {"status": "verified", "path": str(cache), "source": "download", "issues": issues}
        except (OSError, ValueError) as error:
            issues.append({"path": str(cache), "reason": str(error)})
    return {"status": "unavailable", "path": None, "issues": issues}


def file_record(stream, mode: int) -> dict:
    value, size = hashlib.sha256(), 0
    for block in iter(lambda: stream.read(1024 * 1024), b""):
        value.update(block)
        size += len(block)
    return {"sha256": value.hexdigest(), "size": size, "mode": mode & 0o777}


def archive_records(path: Path, spec: dict) -> dict:
    records = {}
    if spec["kind"] == "tar":
        with tarfile.open(path, "r:gz") as archive:
            for member in archive:
                name = relative_path(member.name)
                if not member.isfile() and not member.isdir():
                    raise ValueError("Links and special files are not allowed in tool archives: " + member.name)
                if member.isdir():
                    continue
                if name in records or name == ".":
                    raise ValueError("Duplicate or invalid archived file: " + member.name)
                with archive.extractfile(member) as stream:
                    records[name] = file_record(stream, member.mode)
    else:
        with zipfile.ZipFile(path) as archive:
            for member in archive.infolist():
                name = relative_path(member.filename)
                if stat.S_ISLNK(member.external_attr >> 16):
                    raise ValueError("Links are not allowed in tool archives: " + member.filename)
                if member.is_dir() or not name.startswith("lib/netstandard2.0/") or not name.endswith(".dll"):
                    continue
                if name in records:
                    raise ValueError("Duplicate archived file: " + name)
                with archive.open(member) as stream:
                    records[name] = file_record(stream, 0o644)
    if not records or any(name not in records for name in spec["required_files"]):
        raise ValueError("Verified archive is missing required tool files: " + spec["name"])
    return records


def installation_issues(destination: Path, records: dict) -> list[dict]:
    if destination.is_symlink() or not destination.is_dir():
        return [{"path": str(destination), "reason": "Installation is absent or is not a regular directory"}]
    issues = []
    for name, record in records.items():
        target = destination / name
        if target.is_symlink() or any(parent.is_symlink() for parent in target.parents if parent != destination and destination in parent.parents):
            reason = "Installed path contains a symlink"
        elif not target.is_file():
            reason = "Installed file is missing"
        elif target.stat().st_size != record["size"] or digest(target, "sha256") != record["sha256"]:
            reason = "Installed file differs from verified archive"
        elif record["mode"] & 0o111 and not target.stat().st_mode & 0o111:
            reason = "Installed executable permission is missing"
        else:
            continue
        issues.append({"path": name, "reason": reason})
    return issues


def extract_archive(archive_path: Path, spec: dict, records: dict, destination: Path) -> None:
    written = set()

    def write(name, stream):
        if name not in records:
            raise ValueError("Archive changed to include an unverified file: " + name)
        target = destination / name
        target.parent.mkdir(parents=True, exist_ok=True)
        value = hashlib.sha256()
        with target.open("xb") as output:
            for block in iter(lambda: stream.read(1024 * 1024), b""):
                output.write(block)
                value.update(block)
        if value.hexdigest() != records[name]["sha256"]:
            raise ValueError("Archive changed during extraction: " + name)
        target.chmod(records[name]["mode"])
        written.add(name)

    if spec["kind"] == "tar":
        with tarfile.open(archive_path, "r:gz") as archive:
            for member in archive:
                name = relative_path(member.name)
                if member.isfile():
                    with archive.extractfile(member) as stream:
                        write(name, stream)
                elif not member.isdir():
                    raise ValueError("Archive changed to include a special file")
    else:
        with zipfile.ZipFile(archive_path) as archive:
            for member in archive.infolist():
                name = relative_path(member.filename)
                if name in records:
                    with archive.open(member) as stream:
                        write(name, stream)
    if written != records.keys():
        raise ValueError("Archive changed to omit verified tool files")


def install(workspace: Path, destination: Path, archive_path: Path, spec: dict, records: dict) -> bool:
    # Existing SDKs may be in use by another build. Never replace their directory.
    if not destination.parent.resolve().is_relative_to(workspace):
        raise ValueError("Installation parent escapes the workspace")
    with exclusive_lock(destination.parent / (".setup-" + destination.name + ".lock")):
        if destination.exists() or destination.is_symlink():
            if not installation_issues(destination, records):
                return False
            raise ValueError("Installation appeared during setup; it was preserved")
        with tempfile.TemporaryDirectory(prefix=".setup-" + destination.name + "-", dir=destination.parent) as temporary:
            stage = Path(temporary) / "payload"
            stage.mkdir()
            extract_archive(archive_path, spec, records, stage)
            if spec["name"] == "dotnet_sdk":
                probe = Path(temporary) / "cli-home"
                environment = dict(os.environ, DOTNET_CLI_HOME=str(probe), DOTNET_NOLOGO="1",
                                   DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_CLI_TELEMETRY_OPTOUT="1")
                result = subprocess.run([str(stage / "dotnet"), "--version"], cwd=stage, env=environment,
                                        check=True, capture_output=True, text=True, timeout=30)
                if result.stdout.strip() != spec["version"]:
                    raise ValueError("Extracted SDK version differs from the pinned version")
            if destination.exists() or destination.is_symlink():
                if not installation_issues(destination, records):
                    return False
                raise ValueError("Installation appeared during setup; it was preserved")
            os.rename(stage, destination)
            return True


def setup(workspace: Path, cache_root: Path, metadata: dict, *, check: bool = False,
          offline: bool = False, rid: str | None = None, downloader=None) -> dict:
    workspace, cache_root = Path(workspace).resolve(), Path(cache_root).expanduser().resolve()
    rid = rid or current_rid()
    report = {"ok": False, "mode": "check" if check else "offline" if offline else "setup", "rid": rid,
              "workspace": str(workspace), "cache_root": str(cache_root), "dependencies": [], "missing": [], "errors": []}
    for spec in build_specs(metadata, rid):
        destination = workspace / spec["install_path"]
        dependency = {"name": spec["name"], "version": spec["version"],
                      "installation": {"path": str(destination), "status": "unverified", "issues": []}}
        report["dependencies"].append(dependency)
        try:
            if not destination.parent.resolve().is_relative_to(workspace):
                raise ValueError("Installation parent escapes the workspace")
            archive = select_archive(workspace, cache_root, spec, check=check, offline=offline,
                                     downloader=downloader or download_archive)
            dependency["archive"] = archive
            if archive["path"] is None:
                report["missing"].append(spec["name"] + ": verified archive unavailable")
                report["errors"].extend(spec["name"] + ": " + item["reason"] for item in archive["issues"])
                continue
            records = archive_records(Path(archive["path"]), spec)
            issues = installation_issues(destination, records)
            dependency["installation"]["issues"] = issues[:20]
            dependency["installation"]["issue_count"] = len(issues)
            if not issues:
                dependency["installation"].update(status="verified", verified_files=len(records))
            elif destination.exists() or destination.is_symlink():
                dependency["installation"]["status"] = "conflict"
                report["errors"].append(spec["name"] + ": existing installation differs; preserved without replacement")
            elif check:
                dependency["installation"]["status"] = "missing"
                report["missing"].append(spec["name"] + ": installation missing")
            else:
                published = install(workspace, destination, Path(archive["path"]), spec, records)
                dependency["installation"].update(status="installed" if published else "verified", verified_files=len(records), issues=[], issue_count=0)
        except (OSError, ValueError, EOFError, tarfile.TarError, zipfile.BadZipFile, subprocess.SubprocessError) as error:
            report["errors"].append(spec["name"] + ": " + str(error))
    report["ok"] = not report["missing"] and not report["errors"]
    return report


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache-root", type=Path, default=DEFAULT_CACHE,
                        help="Shared verified archive cache (default: %(default)s)")
    parser.add_argument("--check", action="store_true", help="Check all tool archives/installations without downloads or writes")
    parser.add_argument("--offline", action="store_true", help="Restore only from local verified archives; do not download")
    args = parser.parse_args()
    try:
        metadata = json.loads((ROOT / "tools/source-toolchain.json").read_text())
        report = setup(ROOT, args.cache_root, metadata, check=args.check, offline=args.offline)
    except (OSError, ValueError, KeyError) as error:
        report = {"ok": False, "errors": [str(error)]}
    print(json.dumps(report, ensure_ascii=False, indent=2))
    raise SystemExit(0 if report["ok"] else 1)


if __name__ == "__main__":
    main()
