#!/usr/bin/env python3
"""Preview or copy personal MDPro3 data into the separate Fight save directory."""
from __future__ import annotations
import argparse
from contextlib import ExitStack
import fcntl
import hashlib
import json
import os
from pathlib import Path
import tempfile

PERSONAL_TREES = ("Deck", "Replay", "Puzzle", "Expansions", "Picture/DIY", "Picture/Art2", "Video/Art")
PERSONAL_FILES = ("Data/config.conf", "Data/Rarity.json", "Data/FileGroups.json", "Data/LLMAI/settings.json")

def candidates(source: Path, include_credentials: bool):
    selected = [source / name for name in PERSONAL_FILES]
    if include_credentials:
        selected.append(source / "Data/LLMAI/credentials.json")
    for name in PERSONAL_TREES:
        folder = source / name
        if folder.is_dir() and not folder.is_symlink():
            selected.extend(folder.rglob("*"))
    for path in sorted(set(selected)):
        if path.is_file() and not path.is_symlink() and path.name != ".DS_Store":
            # Import real personal files, never publisher resources reached through links.
            if any(parent.is_symlink() for parent in path.parents if parent != source and source in parent.parents):
                continue
            yield path

def plan_import(source: Path, destination: Path, include_credentials=False):
    if source == destination or source in destination.parents or destination in source.parents:
        raise ValueError("Source and destination must be separate save directories.")
    plan = []
    for path in candidates(source, include_credentials):
        relative = path.relative_to(source)
        target = destination / relative
        if any(parent.is_symlink() for parent in [target, *target.parents] if parent == destination or destination in parent.parents):
            raise ValueError("Import cannot traverse a destination link: " + relative.as_posix())
        plan.append({"path": relative.as_posix(), "action": "preserve_existing" if target.exists() else "copy"})
    return plan

def apply_import(source: Path, destination: Path, plan):
    destination.mkdir(parents=True, exist_ok=True)
    copied = []
    with ExitStack() as stack:
        for folder in (source, destination):
            lock_path = folder / ".native-game-lock"
            if folder == source and not lock_path.exists():
                continue
            stream = stack.enter_context(lock_path.open("rb" if folder == source else "a+"))
            try:
                fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
            except BlockingIOError:
                raise ValueError("Close the game using " + str(folder) + " before importing.")
        for item in plan:
            if item["action"] != "copy":
                continue
            original, target = source / item["path"], destination / item["path"]
            target.parent.mkdir(parents=True, exist_ok=True)
            data = original.read_bytes()
            with tempfile.NamedTemporaryFile(dir=target.parent, prefix=".legacy-copy-", delete=False) as output:
                output.write(data)
                temporary = Path(output.name)
            try:
                # link publishes the completed copy without replacing a file
                # that appeared after preview; originals are never written.
                os.link(temporary, target)
            except FileExistsError:
                continue
            finally:
                temporary.unlink()
            copied.append({"path": item["path"], "sha256": hashlib.sha256(data).hexdigest()})
        report = {"source": str(source), "copied": copied, "preserved_existing": [item["path"] for item in plan if item["action"] == "preserve_existing"]}
        with tempfile.NamedTemporaryFile(mode="w", dir=destination, prefix=".legacy-import-", delete=False) as output:
            json.dump(report, output, ensure_ascii=False, indent=2)
            temporary = output.name
        os.replace(temporary, destination / "Legacy-Import.json")
    return report

def main():
    support = Path.home() / "Library/Application Support"
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, default=support / "MDPro3 Offline")
    parser.add_argument("--destination", type=Path, default=support / "Yu-Gi-Oh! Fight")
    parser.add_argument("--include-credentials", action="store_true", help="Also copy the remembered AI key; never printed")
    parser.add_argument("--apply", action="store_true", help="Apply the preview; existing files are always retained")
    args = parser.parse_args()
    source = args.source.resolve(strict=True)
    if not source.is_dir():
        parser.error("--source must be a save directory")
    destination = args.destination.resolve()
    plan = plan_import(source, destination, args.include_credentials)
    if args.apply:
        report = apply_import(source, destination, plan)
        print(json.dumps({"ok": True, "copied": len(report["copied"]), "preserved_existing": len(report["preserved_existing"]), "destination": str(destination)}, ensure_ascii=False))
    else:
        print(json.dumps({"preview": True, "source": str(source), "destination": str(destination), "files": plan}, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    main()
