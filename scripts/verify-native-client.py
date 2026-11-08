#!/usr/bin/env python3
"""Validate selected or complete native payload against publisher file digests."""
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re

ROOT = Path(__file__).resolve().parent.parent
PUBLISHER_MANIFEST_SHA256 = "28361cbef68e3d825e2cd675421057976c61cda1870fcdffe64797d9c1782a3b"
PUBLISHER_FILE_COUNT = 48347


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", type=Path, default=ROOT / "vendor/MDPro3-mac")
    parser.add_argument("--manifest", type=Path, default=ROOT / "vendor/MDPro3-mac-metadata/checksums.txt")
    scope = parser.add_mutually_exclusive_group()
    scope.add_argument("--preview", action="store_true", help="Only verify app/core subset; not a full game validation")
    scope.add_argument("--present-only", action="store_true", help="Verify every acquired publisher file that is present; missing resources are outside this scope")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    manifest_data = args.manifest.read_bytes()
    if hashlib.sha256(manifest_data).hexdigest() != PUBLISHER_MANIFEST_SHA256:
        raise SystemExit("Publisher manifest does not match the pinned 1.4.7-2 release")
    manifest_lines = manifest_data.decode("utf-8").splitlines()
    manifest_names = [match[2] for line in manifest_lines
                      if (match := re.fullmatch(r"([0-9a-f]{64})  (.+)", line))]
    if len(manifest_names) != PUBLISHER_FILE_COUNT or len(set(manifest_names)) != PUBLISHER_FILE_COUNT:
        raise SystemExit("Publisher manifest does not contain the complete pinned file inventory")
    baseline = ROOT / "vendor/MDPro3-mac-metadata/baseline-paths.json"
    selected = set(json.loads(baseline.read_text())) if args.preview and baseline.is_file() else None
    expected = []
    if selected is not None and not selected.issubset(set(manifest_names)):
        raise SystemExit("Preview inventory contains files outside the publisher manifest")
    for line in manifest_lines:
        match = re.match(r"^([0-9a-f]{64})  (.+)$", line)
        if not match:
            continue
        digest, name = match.groups()
        path = PurePosixPath(name)
        if path.is_absolute() or ".." in path.parts or ":" in name:
            raise SystemExit("Publisher manifest contains an unsafe path")
        if args.preview and selected is not None and name not in selected:
            continue
        if args.preview and selected is None and not name.startswith(("MDPro3.app/", "Data/", "Deck/", "Puzzle/", "StandaloneOSX/MasterDuel/Built-in/")):
            continue
        if args.present_only and not (args.client / name).is_file():
            continue
        expected.append((digest, name))
    missing, wrong = [], []
    verified = 0
    for digest, name in expected:
        path = args.client / name
        if not path.is_file():
            missing.append(name)
            continue
        value = hashlib.sha256()
        with path.open("rb") as source:
            for block in iter(lambda: source.read(1024 * 1024), b""):
                value.update(block)
        if value.hexdigest() != digest:
            wrong.append(name)
        else:
            verified += 1
    name = "acquired-publisher-files-only" if args.present_only else "preview-app-and-core" if args.preview else "complete-publisher-payload"
    result = {"ok": bool(expected) and not missing and not wrong, "scope": name,
              "expected": len(expected), "verified": verified, "missing": missing, "wrong_hash": wrong,
              "gameplay_verified": False, "publisher_version": "1.4.7-2",
              "publisher_manifest_sha256": PUBLISHER_MANIFEST_SHA256,
              "complete_publisher_inventory": PUBLISHER_FILE_COUNT}
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(result, indent=2, ensure_ascii=False) + "\n")
    print(json.dumps({**result, "missing": len(missing), "wrong_hash": len(wrong)}))
    raise SystemExit(0 if result["ok"] else 1)


if __name__ == "__main__":
    main()
