#!/usr/bin/env python3
"""Exclude non-publisher files from a marked staged Client before verification."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[1]
MANIFEST_SHA256 = "28361cbef68e3d825e2cd675421057976c61cda1870fcdffe64797d9c1782a3b"
PUBLISHER_FILE_COUNT = 48347


def sha(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as source:
        for block in iter(lambda: source.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()


def prune(client: Path) -> dict:
    client = client.resolve(strict=True)
    if (ROOT / "vendor").resolve() in [client, *client.parents]:
        raise ValueError("Refusing to modify publisher files under vendor.")
    resources, contents, app = client.parent, client.parent.parent, client.parent.parent.parent
    if (client.name != "Client" or resources.name != "Resources" or contents.name != "Contents"
            or app.suffix != ".app" or not app.parent.name.startswith(".independent-build.")
            or not (resources / ".independent-build").is_file()):
        raise ValueError("Only a marked, staged application Client may be pruned.")
    manifest = ROOT / "vendor/MDPro3-mac-metadata/checksums.txt"
    data = manifest.read_bytes()
    if hashlib.sha256(data).hexdigest() != MANIFEST_SHA256:
        raise ValueError("Publisher manifest does not match the pinned release.")
    known_files, known_directories = set(), set()
    for line in data.decode("utf-8").splitlines():
        match = re.fullmatch(r"[0-9a-f]{64}  (.+)", line)
        name = match[1] if match else line[2:] if line.startswith("  ") else None
        if name is None:
            raise ValueError("Malformed publisher manifest entry.")
        path = PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or ":" in name:
            raise ValueError("Unsafe publisher manifest path.")
        if match:
            known_files.add(name)
        else:
            known_directories.add(name.rstrip("/"))
        known_directories.update(parent.as_posix() for parent in path.parents if parent != PurePosixPath("."))
    if len(known_files) != PUBLISHER_FILE_COUNT:
        raise ValueError("Publisher manifest inventory does not match the pinned release.")
    files, directories = [], []
    for base, names, filenames in os.walk(client, followlinks=False):
        for name in names + filenames:
            path = Path(base) / name
            if path.is_symlink():
                raise ValueError("Staged publisher files must not contain symlinks: " + str(path.relative_to(client)))
            if path.is_dir():
                directories.append(path)
            elif path.is_file():
                files.append(path)
            else:
                raise ValueError("Unsupported staged file type: " + str(path.relative_to(client)))
    removed = []
    for path in files:
        relative = path.relative_to(client).as_posix()
        if relative not in known_files:
            removed.append({"path": relative, "bytes": path.stat().st_size, "sha256": sha(path)})
    # Validate every entry before removing anything. SQLite WAL/SHM files from
    # local read audits and Finder metadata are excluded alongside any extras.
    for entry in removed:
        (client / entry["path"]).unlink()
    removed_directories = []
    for path in sorted(directories, key=lambda value: len(value.parts), reverse=True):
        relative = path.relative_to(client).as_posix()
        if relative not in known_directories:
            path.rmdir()
            removed_directories.append(relative)
    return {"ok": True, "schema": 1, "client": str(client),
            "publisher_manifest_sha256": MANIFEST_SHA256, "complete_publisher_inventory": PUBLISHER_FILE_COUNT,
            "present_publisher_files": len(files) - len(removed), "removed_files": removed,
            "removed_directories": removed_directories,
            "scope": "Staging inventory only; publisher file hashes and required completeness are verified afterward."}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if (ROOT / "vendor").resolve() in [args.output.resolve(), *args.output.resolve().parents]:
        raise SystemExit("Refusing to write a report under vendor.")
    try:
        report = prune(args.client)
    except (ValueError, OSError) as error:
        report = {"ok": False, "error": str(error)}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({key: value if key not in ("removed_files", "removed_directories") else len(value)
                      for key, value in report.items()}, ensure_ascii=False))
    raise SystemExit(0 if report["ok"] else 1)


if __name__ == "__main__":
    main()
