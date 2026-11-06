#!/usr/bin/env python3
"""Package a verified macOS client as a checked, atomically published ZIP."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import stat
import struct
import subprocess
import tarfile
import tempfile
import time
from typing import BinaryIO
import zipfile

from source_build_state import game_source_paths
from native_file_browser import LIBRARY_PATH as FILE_BROWSER_PATH, verify_file_browser


ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "dist/Yu-Gi-Oh! Fight.app"
OUTPUT = ROOT / "dist/Yu-Gi-Oh! Fight-macOS-final.zip"
BASE = ROOT / "artifacts/client-release"
ASSEMBLY = Path("Contents/Resources/Client/MDPro3.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll")
MANIFEST = Path("Contents/Resources/Source-Build.json")
DOCUMENT = Path("Contents/Resources/THIRD_PARTY_NOTICES.md")
RELEASE_DOCUMENTS = (DOCUMENT,
                  Path("Contents/Resources/COPYRIGHT.md"), Path("Contents/Resources/README.md"),
                  Path("Contents/Resources/docs/BUILDING.md"), Path("Contents/Resources/docs/ASSETS.md"),
                  Path("Contents/Resources/native/README.md"),
                  Path("Contents/Resources/docs/media/preview-en.jpg"),
                  Path("Contents/Resources/docs/media/preview-zh.jpg"),
                  Path("Contents/Resources/docs/media/launch-trailer-en.mp4"),
                  Path("Contents/Resources/docs/media/launch-trailer-en.srt"),
                  Path("Contents/Resources/docs/media/launch-trailer-zh.mp4"),
                  Path("Contents/Resources/docs/media/launch-trailer-zh.srt"),
                  Path("Contents/Resources/docs/media/full-duel.mp4"))
NATIVE_FILE_BROWSER = Path("Contents/Resources/Client") / FILE_BROWSER_PATH
FILE_BROWSER_LICENSE = Path("Contents/Resources/licenses/StandaloneFileBrowser-LICENSE.txt")
CRITICAL_FILES = (ASSEMBLY, MANIFEST, NATIVE_FILE_BROWSER, FILE_BROWSER_LICENSE, *RELEASE_DOCUMENTS)


def require(value: bool, message: str) -> None:
    if not value:
        raise ValueError(message)


def stream_digest(stream: BinaryIO) -> str:
    value = hashlib.sha256()
    for block in iter(lambda: stream.read(1024 * 1024), b""):
        value.update(block)
    return value.hexdigest()


def digest(path: Path) -> str:
    with path.open("rb") as stream:
        return stream_digest(stream)


def read(path: Path) -> dict:
    result = json.loads(path.read_text(encoding="utf-8"))
    require(isinstance(result, dict), "Expected an evidence object: " + path.name)
    return result


def verify_signature(app: Path) -> None:
    subprocess.run(["codesign", "--verify", "--deep", "--strict", str(app)], check=True)


def critical_hashes(app: Path) -> dict[str, str]:
    result = {}
    for relative in CRITICAL_FILES:
        path = app / relative
        require(path.is_file() and not path.is_symlink(), "Missing regular application file: " + str(relative))
        result[relative.as_posix()] = digest(path)
    return result


def create_archive(app: Path, archive: Path) -> dict[str, int | str]:
    """Stream macOS metadata from PAX into a standards-compliant ZIP64 archive."""
    command = ["/usr/bin/tar", "-c", "--format", "pax", "--mac-metadata", "-f", "-",
               "-C", str(app.parent), "--", app.name]
    members = 0
    metadata_members = 0
    names: set[str] = set()

    def member_name(member: tarfile.TarInfo, metadata: bool = False) -> str:
        path = PurePosixPath(member.name)
        require(not path.is_absolute() and ".." not in path.parts and bool(path.parts),
                "Invalid application archive path: " + member.name)
        root_metadata = len(path.parts) == 1 and path.name == "._" + app.name
        require(path.parts[0] == app.name or (metadata and root_metadata),
                "Archive member lies outside the application: " + member.name)
        return ("__MACOSX/" if metadata else "") + path.as_posix()

    def zip_info(member: tarfile.TarInfo, name: str, file_type: int) -> zipfile.ZipInfo:
        modified = time.localtime(member.mtime)[:6]
        if modified[0] < 1980:
            modified = (1980, 1, 1, 0, 0, 0)
        elif modified[0] > 2107:
            modified = (2107, 12, 31, 23, 59, 58)
        info = zipfile.ZipInfo(name, modified)
        info.create_system = 3
        info.external_attr = (file_type | (member.mode & 0o7777)) << 16
        if file_type == stat.S_IFDIR:
            info.external_attr |= 0x10
        timestamp = int(member.mtime)
        if 0 <= timestamp <= 0xFFFFFFFF:
            info.extra = struct.pack("<HHBI", 0x5455, 5, 1, timestamp)
        require(name not in names, "Duplicate application archive member: " + name)
        names.add(name)
        return info

    # PAX carries AppleDouble records; the ZIP writer in the system bsdtar drops
    # them. Python writes the ZIP64 offsets while retaining those records as
    # ordinary, CRC-checked __MACOSX members understood by Archive Utility.
    with tempfile.TemporaryFile() as errors:
        process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=errors)
        try:
            require(process.stdout is not None, "Missing macOS metadata archive stream")
            with tarfile.open(fileobj=process.stdout, mode="r|") as source, \
                    zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED,
                                    compresslevel=3, allowZip64=True) as destination:
                for member in source:
                    if member.isdir():
                        name = member_name(member) + "/"
                        destination.writestr(zip_info(member, name, stat.S_IFDIR), b"")
                    elif member.issym():
                        name = member_name(member)
                        destination.writestr(zip_info(member, name, stat.S_IFLNK),
                                             member.linkname.encode("utf-8"))
                    elif member.isfile() or member.islnk():
                        if member.islnk():
                            # ZIP represents hard links as complete regular files.
                            path = app.parent / member_name(member)
                            require(path.is_file() and not path.is_symlink()
                                    and path.resolve().is_relative_to(app.resolve()),
                                    "Invalid application hard link: " + member.name)
                            payload = path.open("rb")
                            size = path.stat().st_size
                        else:
                            payload = source.extractfile(member)
                            size = member.size
                        require(payload is not None, "Missing archive payload: " + member.name)
                        with payload:
                            prefix = payload.read(4)
                            metadata = (PurePosixPath(member.name).name.startswith("._")
                                        and prefix == b"\x00\x05\x16\x07")
                            name = member_name(member, metadata)
                            info = zip_info(member, name, stat.S_IFREG)
                            info.file_size = size
                            info.compress_type = zipfile.ZIP_DEFLATED
                            info._compresslevel = 3
                            with destination.open(info, "w", force_zip64=size >= zipfile.ZIP64_LIMIT) as stream:
                                stream.write(prefix)
                                shutil.copyfileobj(payload, stream, 1024 * 1024)
                            metadata_members += int(metadata)
                    else:
                        raise ValueError("Unsupported application archive member: " + member.name)
                    members += 1
            process.stdout.close()
            status = process.wait()
            if status:
                errors.seek(0)
                raise subprocess.CalledProcessError(status, command, stderr=errors.read().decode("utf-8", "replace"))
        except BaseException:
            if process.stdout is not None:
                process.stdout.close()
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
            raise
    return {"archive_generator": "bsdtar-pax-python-zip64", "archive_members": members,
            "mac_metadata_members": metadata_members, "compression_level": 3}


def verify_archive(archive: Path, app_name: str, expected: dict[str, str]) -> dict[str, str]:
    verified = {}
    with zipfile.ZipFile(archive) as package:
        broken = package.testzip()
        require(broken is None, "Archive CRC failed: " + str(broken))
        names = package.namelist()
        for relative, expected_sha in expected.items():
            name = app_name + "/" + relative
            require(names.count(name) == 1, "Archive must contain exactly one critical member: " + name)
            with package.open(name) as stream:
                actual_sha = stream_digest(stream)
            require(actual_sha == expected_sha, "Archived application file differs: " + name)
            verified[name] = actual_sha
    return verified


def atomic_write(path: Path, data: bytes) -> None:
    require(not path.is_symlink(), "Refusing to replace a linked release record: " + str(path))
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(prefix=".client-release-", dir=path.parent, delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, path)
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)


def verify_application(app: Path) -> dict[str, str]:
    """Verify the current shipping application before ZIP or DMG packaging."""
    expected = critical_hashes(app)
    manifest = read(app / MANIFEST)
    require(manifest.get("ok") is True and manifest.get("source_compiled") is True
            and manifest.get("compatibility_verified") is True and manifest.get("build_profile") == "release"
            and "FIGHT_UI_PROOF" not in manifest.get("compiler_defines", []),
            "The application must contain the shipping release build")
    assembly_sha = expected[ASSEMBLY.as_posix()]
    require(manifest.get("assembly_sha256") == assembly_sha,
            "Application assembly does not match its release manifest")
    sources = {path.relative_to(ROOT / "game").as_posix(): digest(path) for path in game_source_paths(ROOT)}
    require(sources == manifest.get("source_files"), "Compile and package the current game sources before archiving")
    for relative in RELEASE_DOCUMENTS:
        source = ROOT / relative.relative_to("Contents/Resources")
        require(source.is_file() and digest(source) == expected[relative.as_posix()],
                "Packaged release document is stale: " + source.name)
    client = app / "Contents/Resources/Client"
    changes = read(app / "Contents/Resources/Client-Changes.json")
    require(changes.get("native_file_browser") is not None, "Packaged native file selector metadata is missing")
    verify_file_browser(client, changes["native_file_browser"])
    require(expected[FILE_BROWSER_LICENSE.as_posix()] == digest(ROOT / "native/StandaloneFileBrowser/LICENSE.txt"),
            "Packaged native file selector license is stale")
    require(not any((app / "Contents/Resources/Client").rglob("credentials.json*")),
            "Application bundle contains private credentials")
    verify_signature(app)
    return expected


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--app", type=Path, default=APP, help="Verified application bundle to archive")
    parser.add_argument("--output", type=Path, default=OUTPUT, help="Final ZIP destination")
    parser.add_argument("--base", type=Path, default=BASE, help="Directory for the local archive verification record")
    args = parser.parse_args()
    requested_app = args.app.expanduser().absolute()
    require(not requested_app.is_symlink(), "Application bundle must not be a symbolic link")
    app = requested_app.resolve(strict=True)
    output = args.output.expanduser().absolute()
    base = args.base.expanduser().absolute()
    require(app.is_dir() and app.suffix == ".app", "Expected a macOS application bundle")
    require(output.suffix.lower() == ".zip", "Release archive destination must end in .zip")
    require(not output.is_symlink() and (not output.exists() or output.is_file()),
            "Archive destination must be a regular file")
    require(not output.parent.resolve().is_relative_to(app) and not base.resolve().is_relative_to(app),
            "Release output and evidence must remain outside the signed application")
    require(not any(character in output.name for character in ("\n", "\r", "\\")),
            "Archive filename cannot contain newline or backslash characters")
    checksums = output.parent / "SHA256SUMS.txt"
    archive_record = base / "archive.json"
    for path in (checksums, archive_record):
        require(not path.is_symlink() and (not path.exists() or path.is_file()),
                "Release record destination must be a regular file: " + str(path))

    expected = verify_application(app)
    assembly_sha = expected[ASSEMBLY.as_posix()]

    output.parent.mkdir(parents=True, exist_ok=True)
    # Stage beside the destination so os.replace publishes on one filesystem.
    with tempfile.TemporaryDirectory(prefix=".client-release-", dir=output.parent) as directory:
        staged = Path(directory) / output.name
        archive_metadata = create_archive(app, staged)
        members = verify_archive(staged, app.name, expected)
        require(critical_hashes(app) == expected,
                "Application changed while packaging")
        verify_signature(app)
        archive_sha = digest(staged)
        archive_bytes = staged.stat().st_size
        with staged.open("rb") as stream:
            os.fsync(stream.fileno())
        os.replace(staged, output)

    record = {"ok": True, "kind": "macos-client-release-archive", "application": str(app),
              "archive": str(output), "archive_sha256": archive_sha, "archive_bytes": archive_bytes,
              "release_assembly_sha256": assembly_sha,
              "signature_verified": True, "zip_crc_verified": True, "critical_members": members,
              "resource_forks_preserved": True, "checksums": str(checksums)}
    record.update(archive_metadata)
    atomic_write(checksums, (archive_sha + "  " + output.name + "\n").encode("utf-8"))
    atomic_write(archive_record, (json.dumps(record, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    print(json.dumps(record, ensure_ascii=False))


if __name__ == "__main__":
    main()
