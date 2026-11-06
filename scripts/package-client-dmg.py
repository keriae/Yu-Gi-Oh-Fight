#!/usr/bin/env python3
"""Create and verify a macOS disk image with an Applications drag-and-drop link."""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "dist/Yu-Gi-Oh! Fight.app"
OUTPUT = ROOT / "dist/Yu-Gi-Oh! Fight-macOS.dmg"
BASE = ROOT / "artifacts/client-release"
GITHUB_ASSET_LIMIT = 2 * 1024**3


def archive_helpers():
    spec = importlib.util.spec_from_file_location(
        "client_release_archive", Path(__file__).with_name("package-client-release.py"))
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def run(*arguments: str | Path) -> None:
    subprocess.run([str(argument) for argument in arguments], check=True)


def verify_disk_image(image: Path, app_name: str, expected: dict[str, str], helpers, mount: Path) -> None:
    run("hdiutil", "verify", image)
    mount.mkdir()
    try:
        run("hdiutil", "attach", image, "-readonly", "-nobrowse", "-noautoopen", "-mountpoint", mount)
        helpers.require(mount.is_mount(), "The disk image did not mount at its requested location")
        packaged = mount / app_name
        helpers.require(packaged.is_dir() and not packaged.is_symlink(),
                        "The mounted disk image is missing its application")
        helpers.require(helpers.critical_hashes(packaged) == expected,
                        "The disk image changed a critical application file")
        helpers.verify_signature(packaged)
        applications = mount / "Applications"
        helpers.require(applications.is_symlink() and os.readlink(applications) == "/Applications",
                        "The disk image is missing its Applications link")
    finally:
        if mount.is_mount():
            run("hdiutil", "detach", mount)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--app", type=Path, default=APP, help="Verified shipping application bundle")
    parser.add_argument("--output", type=Path, default=OUTPUT, help="Local DMG destination")
    parser.add_argument("--base", type=Path, default=BASE, help="Directory for the local verification record")
    parser.add_argument("--format", choices=("UDZO", "ULMO"), default="UDZO",
                        help="UDZO is zlib compression; ULMO uses slower LZMA compression")
    args = parser.parse_args()
    helpers = archive_helpers()
    requested_app = args.app.expanduser().absolute()
    helpers.require(not requested_app.is_symlink(), "Application bundle must not be a symbolic link")
    app = requested_app.resolve(strict=True)
    output = args.output.expanduser().absolute()
    base = args.base.expanduser().absolute()
    helpers.require(app.is_dir() and app.suffix == ".app", "Expected a macOS application bundle")
    helpers.require(output.suffix.lower() == ".dmg", "Disk image destination must end in .dmg")
    helpers.require(not output.is_symlink() and (not output.exists() or output.is_file()),
                    "Disk image destination must be a regular file")
    helpers.require(not output.parent.resolve().is_relative_to(app) and not base.resolve().is_relative_to(app),
                    "Disk image and evidence must remain outside the signed application")
    helpers.require(not any(character in output.name for character in ("\n", "\r", "\\")),
                    "Disk image filename cannot contain newline or backslash characters")
    checksums = output.parent / "SHA256SUMS.dmg.txt"
    record_path = base / "dmg.json"
    for path in (checksums, record_path):
        helpers.require(not path.is_symlink() and (not path.exists() or path.is_file()),
                        "Release record destination must be a regular file: " + str(path))
    expected = helpers.verify_application(app)
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix=".client-dmg-", dir=output.parent) as directory:
        stage = Path(directory)
        folder = stage / "payload"
        folder.mkdir()
        copied_app = folder / app.name
        cloned = subprocess.run(["cp", "-cR", str(app), str(copied_app)],
                                stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
        if cloned.returncode:
            if copied_app.exists():
                shutil.rmtree(copied_app)
            run("ditto", app, copied_app)
        helpers.require(helpers.critical_hashes(copied_app) == expected,
                        "The application changed while staging the disk image")
        helpers.verify_signature(copied_app)
        (folder / "Applications").symlink_to("/Applications")
        staged_image = stage / output.name
        # HFS+ rewrites Unicode filenames to NFD, invalidating the resource seal
        # for the shipped Japanese deck filenames. APFS preserves their bytes
        # and is available on every supported macOS version.
        command = ["hdiutil", "create", "-srcfolder", str(folder), "-volname", "Yu-Gi-Oh! Fight",
                   "-fs", "APFS", "-format", args.format, "-nospotlight", str(staged_image)]
        if args.format == "UDZO":
            command[2:2] = ["-imagekey", "zlib-level=9"]
        run(*command)
        verify_disk_image(staged_image, app.name, expected, helpers, stage / "mounted")
        helpers.require(helpers.critical_hashes(app) == expected, "The source application changed during packaging")
        helpers.verify_signature(app)
        image_sha = helpers.digest(staged_image)
        image_bytes = staged_image.stat().st_size
        with staged_image.open("rb") as stream:
            os.fsync(stream.fileno())
        os.replace(staged_image, output)
    record = {"ok": True, "kind": "macos-client-disk-image", "application": str(app),
              "disk_image": str(output), "disk_image_sha256": image_sha, "disk_image_bytes": image_bytes,
              "format": args.format, "filesystem": "APFS", "image_checksum_verified": True,
              "mounted_signature_verified": True, "applications_link_verified": True,
              "critical_members": expected, "checksums": str(checksums),
              "github_single_asset_limit_bytes": GITHUB_ASSET_LIMIT,
              "github_single_asset_uploadable": image_bytes < GITHUB_ASSET_LIMIT}
    helpers.atomic_write(checksums, (image_sha + "  " + output.name + "\n").encode("utf-8"))
    helpers.atomic_write(record_path, (json.dumps(record, ensure_ascii=False, indent=2) + "\n").encode("utf-8"))
    print(json.dumps(record, ensure_ascii=False))


if __name__ == "__main__":
    main()
