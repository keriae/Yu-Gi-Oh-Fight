#!/usr/bin/env python3
"""Install validated deck lessons into a marked staging Client, never user saves."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "content/deck-academy"


def load_script(name: str, filename: str):
    spec = importlib.util.spec_from_file_location(name, ROOT / "scripts" / filename)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def content_hashes(content: Path) -> dict[str, str]:
    if not (content / "manifest.json").is_file():
        raise ValueError("The deck academy content manifest is missing.")
    result = {}
    for path in sorted(content.rglob("*")):
        if path.is_symlink():
            raise ValueError("Deck academy content must not contain symbolic links.")
        if path.is_file():
            result[path.relative_to(content).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result


def safe_relative(value: str) -> str:
    if not isinstance(value, str) or not value or "\x00" in value:
        raise ValueError("Missing academy content path.")
    path = PurePosixPath(value)
    if (path.is_absolute() or ".." in path.parts or "\\" in value or ":" in value
            or value == "." or path.as_posix() != value):
        raise ValueError("Unsafe academy content path: " + value)
    return path.as_posix()


def distribution_paths(catalog: dict, hashes: dict[str, str]) -> dict[str, str]:
    """Return exact runtime destination -> content source, shared by seeding/audit."""
    if catalog.get("schema") != 1 or not isinstance(catalog.get("decks"), list) or not catalog["decks"]:
        raise ValueError("Invalid deck academy catalog.")
    if not isinstance(hashes, dict) or "manifest.json" not in hashes:
        raise ValueError("Missing bound academy content inventory.")
    planned = {}
    for relative, digest in hashes.items():
        safe_relative(relative)
        if not isinstance(digest, str) or not re.fullmatch(r"[0-9a-f]{64}", digest):
            raise ValueError("Invalid academy content hash: " + relative)
        planned["Data/DeckAcademy/" + relative] = relative
    ids = set()
    for entry in catalog["decks"]:
        identifier = entry.get("id", "")
        if not isinstance(identifier, str) or not re.fullmatch(r"[a-z][a-z0-9-]*", identifier) or identifier in ids:
            raise ValueError("Unsafe or duplicate academy deck ID.")
        ids.add(identifier)
        deck_source = safe_relative(entry["deck_file"])
        if not re.fullmatch(r"Decks/[^/]+\.ydk", deck_source):
            raise ValueError("Unsupported academy deck path.")
        if deck_source not in hashes:
            raise ValueError("Missing academy deck template: " + deck_source)
        copies = []
        for kind, extension in (("puzzle", ".lua"), ("replay", ".yrp3d")):
            source = entry.get(kind + "_source")
            runtime = entry.get(kind + "_file")
            if kind == "replay" and not source and not runtime:
                continue
            expected_source = kind.title() + "/" + identifier + extension
            expected_runtime = kind.title() + "/Academy/" + identifier + extension
            if source != expected_source or runtime != expected_runtime:
                raise ValueError("Academy source/runtime path mismatch: " + identifier + " " + kind)
            copies.append((runtime, source))
        for target, source in copies:
            if source not in hashes or target in planned:
                raise ValueError("Missing content or duplicate academy destination: " + target)
            planned[target] = source
    return planned


def seed(client_path: Path, source_manifest: Path, content: Path = CONTENT) -> dict:
    defaults = load_script("academy_defaults", "seed-offline-defaults.py")
    client = defaults.staged_client(client_path)
    build = json.loads(source_manifest.read_text())
    if not all(build.get(key) is True for key in ("ok", "source_compiled", "compatibility_verified")):
        raise ValueError("A successful compiled-source build is required.")
    assembly = defaults.checked_file(client, "MDPro3.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll")
    if hashlib.sha256(assembly.read_bytes()).hexdigest() != build.get("assembly_sha256"):
        raise ValueError("Staged assembly does not match the source build.")
    hashes = content_hashes(content)
    if hashes != build.get("deck_academy_content"):
        raise ValueError("Deck academy content changed after source compilation; rebuild first.")
    validator = load_script("academy_validator", "validate-deck-academy.py")
    validation = validator.validate_library(content, client)
    if validation.get("ok") is not True:
        raise ValueError("Deck academy validation failed.")
    catalog = json.loads((content / "manifest.json").read_text())
    paths = distribution_paths(catalog, hashes)
    data = {relative: (content / relative).read_bytes() for relative in hashes}
    if any(hashlib.sha256(value).hexdigest() != hashes[relative] for relative, value in data.items()):
        raise ValueError("Deck academy content changed during seeding.")
    planned = {target: data[source] for target, source in paths.items()}
    updates, changes = [], []
    for relative, data in sorted(planned.items()):
        safe_relative(relative)
        target = client / relative
        for parent in [target, *target.parents]:
            if parent == client:
                break
            if parent.is_symlink():
                raise ValueError("Staged academy data must not traverse links: " + relative)
        if target.exists() and not target.is_file():
            raise ValueError("Unexpected academy destination type: " + relative)
        before = target.read_bytes() if target.exists() else None
        if before is not None and before != data:
            raise ValueError("Refusing to replace unrelated staged academy data: " + relative)
        if before is None:
            updates.append((target, data))
        changes.append({"path": relative, "before_sha256": None if before is None else hashlib.sha256(before).hexdigest(),
                        "after_sha256": hashlib.sha256(data).hexdigest(), "action": "created" if before is None else "unchanged"})
    # Validate the entire plan before any write. Application publication is atomic.
    for target, data in updates:
        target.parent.mkdir(parents=True, exist_ok=True)
        defaults.atomic_write(target, data)
    return {"ok": True, "decks": len(catalog["decks"]), "changed_files": len(updates),
            "content_manifest_sha256": hashes["manifest.json"], "validation": validation, "changes": changes}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    if args.output.resolve().is_relative_to((ROOT / "vendor").resolve()):
        raise ValueError("Reports must not be written into publisher resources.")
    report = seed(args.client, args.manifest)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n")
    print(json.dumps({"ok": True, "decks": report["decks"], "changed_files": report["changed_files"]}))


if __name__ == "__main__":
    main()
