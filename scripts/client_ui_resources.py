"""Offline, content-addressed Unity resource rebinding for the owned client.

Public API: plan_client_resources(root) returns changed publisher-relative
paths mapped to verified cache files, plus a manifest record. Apply those files
to an independent staging client, then call verify_client_resources.
"""
from __future__ import annotations
import argparse
import concurrent.futures
import fcntl
import hashlib
import json
import os
from pathlib import Path
import shutil
import struct
import subprocess
import sys
import tempfile
import zlib

from client_ui_catalog import Catalog

BINDINGS_PATH = "content/client-ui/serialized-bindings.json"
CLEANUP_PATH = "tools/client-ui-resources/missing-components.json"
CATALOG_PATH = "MDPro3.app/Contents/Resources/Data/StreamingAssets/aa/catalog.bin"
CATALOG_HASH_PATH = "MDPro3.app/Contents/Resources/Data/StreamingAssets/aa/catalog.hash"
BOOTSTRAP = ["globalgamemanagers", "globalgamemanagers.assets", "resources.assets", "sharedassets0.assets", "level0"]
TYPE_FIELDS = {"m_TargetAssemblyTypeName", "m_ObjectArgumentAssemblyTypeName", "m_CustomPlayableFullTypename", "m_EditorClassIdentifier"}
POPUP_FILE = "StandaloneOSX/MDPro3/prefab_assets_ui.bundle"
POPUP_CAB = "CAB-82836c9ccdcbcc28937d64d5337d491c"
POPUP_COMPONENT = 8948823303644636069
POPUP_GO = -5305107297375231067
GUIDE_FILE = "StandaloneOSX/MasterDuel/Built-in/duel"
GUIDE_CAB = "CAB-768ef927cda5a66cf2ebfcaad62fbe60"
GUIDE_EVENT = -1637353008260163514

def digest(path: Path) -> str:
    value = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            value.update(block)
    return value.hexdigest()

def _unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate resource binding key: " + key)
        result[key] = value
    return result

def load_bindings(root: Path) -> dict[tuple[str, str], tuple[str, str]]:
    value = json.loads((root / BINDINGS_PATH).read_text(), object_pairs_hook=_unique)
    if value.get("version") != 1 or not isinstance(value.get("bindings"), dict):
        raise ValueError("Unsupported serialized identity binding table.")
    result = {}
    for source, target in value["bindings"].items():
        if not isinstance(target, str) or ":" not in source or ":" not in target:
            raise ValueError("Binding identities must be assembly:type strings.")
        before, after = tuple(source.split(":", 1)), tuple(target.split(":", 1))
        if not all(before + after):
            raise ValueError("Empty serialized type identity.")
        result[before] = after
    return result

def load_missing_components(root: Path) -> dict:
    value = json.loads((root / CLEANUP_PATH).read_text(), object_pairs_hook=_unique)
    if value.get("version") != 1 or not isinstance(value.get("files"), dict):
        raise ValueError("Unsupported fingerprinted missing-component cleanup table.")
    assembly = root / "vendor/MDPro3-mac/MDPro3.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll"
    if digest(assembly) != value.get("publisher_assembly_sha256"):
        raise ValueError("Missing-component evidence is for a different publisher assembly.")
    return value

def _pointers(data, path=()):
    if isinstance(data, dict):
        if {"m_FileID", "m_PathID"}.issubset(data):
            yield path, data
        else:
            for key, value in data.items():
                yield from _pointers(value, (*path, key))
    elif isinstance(data, (list, tuple)):
        for index, value in enumerate(data):
            yield from _pointers(value, (*path, index))

def _missing_incoming(objects, target_keys, raw_data=None):
    incoming = {key: [] for key in target_keys}
    for key, obj in objects.items():
        file = obj.assets_file
        raw = raw_data[key] if raw_data is not None else obj.get_raw_data()
        candidates = []
        for asset, path_id in target_keys:
            file_ids = ([0] if file.name == asset else []) + [index + 1 for index, external in enumerate(file.externals) if external.name == asset]
            for file_id in file_ids:
                token = struct.pack(file.header.endian + "iq", file_id, path_id)
                offsets, start = [], 0
                while True:
                    offset = raw.find(token, start)
                    if offset < 0:
                        break
                    offsets.append(offset)
                    start = offset + 1
                if offsets:
                    candidates.append(((asset, path_id), file_id, len(offsets)))
        if not candidates:
            continue
        # All recorded incoming references have native type trees. An unknown
        # pointer candidate fails closed instead of guessing a raw-byte edit.
        tree = obj.read_typetree()
        pointers = list(_pointers(tree))
        for target, file_id, count in candidates:
            matches = [list(path) for path, pointer in pointers if pointer["m_FileID"] == file_id and pointer["m_PathID"] == target[1]]
            if len(matches) != count:
                raise ValueError("Missing component has an unverified incoming pointer: " + str(key))
            incoming[target].append({"asset_file": key[0], "path_id": key[1], "type": obj.type.name,
                "target_file_id": file_id, "verified_typed_paths": matches})
    return incoming

def _remove_preload_entries(bundle, dropped):
    """Remove preload slots and adjust ranges using their original indices."""
    def ranges(value):
        if isinstance(value, dict):
            if {"preloadIndex", "preloadSize"}.issubset(value):
                start, length = value["preloadIndex"], value["preloadSize"]
                value["preloadIndex"] = start - sum(index < start for index in dropped)
                value["preloadSize"] = length - sum(start <= index < start + length for index in dropped)
            else:
                for item in value.values(): ranges(item)
        elif isinstance(value, (list, tuple)):
            for item in value: ranges(item)
    ranges(bundle)
    bundle["m_PreloadTable"] = [pointer for index, pointer in enumerate(bundle["m_PreloadTable"]) if index not in dropped]

def _cleanup_missing_components(source, objects, before, rule, edited, removed, changes):
    if digest(source) != rule["source_sha256"]:
        raise ValueError("Missing-component source fingerprint differs.")
    targets = {(item["asset_file"], item["path_id"]): item for item in rule["objects"]}
    actual = _missing_incoming(objects, targets, before)
    reference_edits = {}
    for key, item in targets.items():
        obj = objects.get(key)
        if obj is None or obj.type.name != "MonoBehaviour" or hashlib.sha256(before[key]).hexdigest() != item["body_sha256"]:
            raise ValueError("Missing-component body fingerprint differs: " + str(key))
        head = obj.parse_monobehaviour_head()
        script_file = obj.assets_file.name if head.m_Script.m_FileID == 0 else obj.assets_file.externals[head.m_Script.m_FileID - 1].name
        script = objects.get((script_file, head.m_Script.m_PathID))
        if script is None or script.type.name != "MonoScript":
            raise ValueError("Missing-component script identity is not locally verifiable.")
        script_data = script.read_typetree()
        identity = script_data["m_AssemblyName"] + ":" + (script_data["m_Namespace"] + "." if script_data["m_Namespace"] else "") + script_data["m_ClassName"]
        if identity != item["missing_identity"] or head.m_GameObject.m_FileID != 0 or head.m_GameObject.m_PathID != item["game_object_path_id"]:
            raise ValueError("Missing-component identity or owner differs: " + str(key))
        expected = [{name: value for name, value in entry.items() if name != "body_sha256"} for entry in item["incoming_references"]]
        canonical = lambda entry: json.dumps(entry, sort_keys=True)
        if sorted(actual[key], key=canonical) != sorted(expected, key=canonical):
            raise ValueError("Missing-component incoming graph differs: " + str(key))
        for reference in item["incoming_references"]:
            owner_key = (reference["asset_file"], reference["path_id"])
            if hashlib.sha256(before[owner_key]).hexdigest() != reference["body_sha256"]:
                raise ValueError("Missing-component incoming object fingerprint differs.")
            for path in reference["verified_typed_paths"]:
                reference_edits.setdefault(owner_key, []).append((path, key[1]))
    for key, paths in reference_edits.items():
        obj = objects[key]
        data = edited.get(key)
        if data is None:
            data = obj.read_typetree()
        if obj.type.name == "GameObject":
            indices = []
            for path, target in paths:
                if len(path) != 3 or path[0] != "m_Component" or path[2] != "component":
                    raise ValueError("Unexpected GameObject missing-component pointer field.")
                item = targets[(key[0], target)]
                if data["m_Name"] != item["game_object_name"] or item["game_object_component_indices"] != [path[1]]:
                    raise ValueError("Missing-component GameObject slot fingerprint differs.")
                indices.append(path[1])
            for index in sorted(set(indices), reverse=True):
                data["m_Component"].pop(index)
        elif obj.type.name == "AnimatorController":
            for path, _ in paths:
                if len(path) != 2 or path[0] != "m_StateMachineBehaviours":
                    raise ValueError("Unexpected AnimatorController missing-behaviour pointer field.")
                data["m_StateMachineBehaviours"][path[1]] = {"m_FileID": 0, "m_PathID": 0}
        elif obj.type.name == "AssetBundle":
            dropped = []
            for path, _ in paths:
                if len(path) != 2 or path[0] != "m_PreloadTable":
                    raise ValueError("Missing component is unexpectedly a bundle container root.")
                dropped.append(path[1])
            _remove_preload_entries(data, dropped)
        else:
            raise ValueError("Missing component has a live custom incoming reference: " + str(key))
        obj.save_typetree(data)
        edited[key] = data
        changes.append({"kind": "repair-known-unimplemented-component-reference", "asset_file": key[0], "path_id": key[1],
            "type": obj.type.name, "fields": [{"path": path, "removed_target": target} for path, target in paths],
            "animator_indices_and_ranges_preserved": obj.type.name == "AnimatorController"})
    for key, item in targets.items():
        del objects[key].assets_file.objects[key[1]]
        removed.add(key)
        changes.append({"kind": "remove-publisher-unimplemented-component", **item})
    return set(targets)

def _type_string(value: str, bindings) -> str:
    if "," in value:
        name, assembly = value.split(",", 1)
        short = assembly.strip().split(",", 1)[0]
        target = bindings.get((short, name))
        if target is None:
            return value
        suffix = assembly.strip()[len(short):]
        return target[1] + ", " + target[0] + suffix
    if "::" in value:
        assembly, name = value.split("::", 1)
        target = bindings.get((assembly, name))
        return target[0] + "::" + target[1] if target else value
    return value

def _rewrite_type_fields(data, bindings, path=()):
    changes = []
    if isinstance(data, dict):
        for key, value in data.items():
            if key in TYPE_FIELDS and isinstance(value, str):
                target = _type_string(value, bindings)
                if target != value:
                    changes.append({"path": [*path, key], "before": value, "after": target})
                    data[key] = target
            elif key == "type" and isinstance(value, dict) and {"class", "ns", "asm"}.issubset(value):
                name = (value["ns"] + "." if value["ns"] else "") + value["class"]
                target = bindings.get((value["asm"], name))
                if target and target != (value["asm"], name):
                    namespace, _, classname = target[1].rpartition(".")
                    changes.append({"path": [*path, key], "before": dict(value), "after": {"asm": target[0], "ns": namespace, "class": classname}})
                    value.update({"asm": target[0], "ns": namespace, "class": classname})
            else:
                changes.extend(_rewrite_type_fields(value, bindings, (*path, key)))
    elif isinstance(data, (list, tuple)):
        for index, value in enumerate(data):
            changes.extend(_rewrite_type_fields(value, bindings, (*path, index)))
    return changes

def _rewrite_stripped_strings(raw: bytes, bindings, endian: str):
    edits = []
    for offset in range(0, len(raw) - 4, 4):
        size = struct.unpack_from(endian + "I", raw, offset)[0]
        if not 8 <= size <= 4096 or offset + 4 + size > len(raw):
            continue
        value_bytes = raw[offset + 4:offset + 4 + size]
        try:
            value = value_bytes.decode("utf-8")
        except UnicodeDecodeError:
            continue
        if "," not in value and "::" not in value:
            continue
        target = _type_string(value, bindings)
        if target == value:
            continue
        end = (offset + 4 + size + 3) // 4 * 4
        encoded = target.encode("utf-8")
        replacement = struct.pack(endian + "I", len(encoded)) + encoded + b"\0" * (-len(encoded) % 4)
        edits.append((offset, end, replacement, value, target))
    for previous, following in zip(edits, edits[1:]):
        if previous[1] > following[0]:
            raise ValueError("Overlapping aligned CLR identity strings in a stripped object.")
    output = raw
    for start, end, replacement, _, _ in reversed(edits):
        output = output[:start] + replacement + output[end:]
    return output, [{"raw_offset": start, "before": before, "after": after, "kind": "aligned-full-clr-identity"} for start, _, _, before, after in edits]

def _bundle_crc(path: Path) -> int:
    import UnityPy
    data = path.read_bytes()
    env = UnityPy.load(str(path))
    bundle = next(iter(env.files.values()))
    if bundle.signature != "UnityFS" or bundle.dataflags & 0x400:
        raise ValueError("Only unencrypted UnityFS bundles can be rebound.")
    position = 0
    for _ in range(1):
        position = data.index(b"\0", position) + 1
    version = struct.unpack_from(">I", data, position)[0]
    position += 4
    for _ in range(2):
        position = data.index(b"\0", position) + 1
    size, compressed, uncompressed, flags = struct.unpack_from(">QIII", data, position)
    position += 20
    if size != len(data):
        raise ValueError("UnityFS header file size differs from actual bytes.")
    if bundle._uses_block_alignment:
        position = (position + 15) // 16 * 16
    metadata_offset = len(data) - compressed if flags & 0x80 else position
    block_data_offset = position if flags & 0x80 else position + compressed
    metadata = bundle.decompress_data(data[metadata_offset:metadata_offset + compressed], uncompressed, flags)
    if flags & 0x200:
        block_data_offset = (block_data_offset + 15) // 16 * 16
    count = struct.unpack_from(">I", metadata, 16)[0]
    crc = 0
    for index in range(count):
        length, packed, block_flags = struct.unpack_from(">IIH", metadata, 20 + index * 10)
        block = bundle.decompress_data(data[block_data_offset:block_data_offset + packed], length, block_flags, index)
        crc = zlib.crc32(block, crc)
        block_data_offset += packed
    return crc & 0xffffffff

def _object_map(env):
    return {(obj.assets_file.name, obj.path_id): obj for obj in env.objects}

def _cleanup_popup(objects, edited, removed, changes):
    key = (POPUP_CAB, POPUP_COMPONENT)
    obj = objects.get(key)
    go = objects.get((POPUP_CAB, POPUP_GO))
    if obj is None or go is None or hashlib.sha256(obj.get_raw_data()).hexdigest() != "144594eb82e449f33dcf9abeee8bd0b245e582ddf62126dda3580267a0a9c33a":
        raise ValueError("Publisher PopupDuelSelectionItem empty slot fingerprint differs.")
    head = obj.parse_monobehaviour_head()
    data = go.read_typetree()
    if head.m_Script.m_PathID != 0 or head.m_GameObject.m_PathID != POPUP_GO or data["m_Name"] != "PopupDuelSelectionItem":
        raise ValueError("Publisher popup slot is not the expected inert component.")
    indices = [index for index, component in enumerate(data["m_Component"]) if component["component"]["m_PathID"] == POPUP_COMPONENT]
    if indices != [3]:
        raise ValueError("Popup null component ownership differs.")
    data["m_Component"].pop(3)
    go.save_typetree(data)
    edited[(POPUP_CAB, POPUP_GO)] = data
    for candidate_key, candidate in objects.items():
        if candidate.assets_file.name != POPUP_CAB or candidate.type.name != "AssetBundle":
            continue
        bundle = candidate.read_typetree()
        dropped = [index for index, pointer in enumerate(bundle["m_PreloadTable"]) if pointer["m_FileID"] == 0 and pointer["m_PathID"] == POPUP_COMPONENT]
        if dropped != [1906]:
            raise ValueError("Popup empty component preload span differs.")
        _remove_preload_entries(bundle, dropped)
        candidate.save_typetree(bundle)
        edited[candidate_key] = bundle
    del obj.assets_file.objects[POPUP_COMPONENT]
    removed.add(key)
    changes.append({"kind": "remove-known-inert-missing-component", "asset_file": POPUP_CAB, "component": POPUP_COMPONENT,
        "game_object": POPUP_GO, "game_object_name": "PopupDuelSelectionItem", "component_index_removed": 3,
        "preload_index_removed": 1906, "body_sha256": hashlib.sha256(obj.get_raw_data()).hexdigest()})

def _cleanup_guide(objects, edited, changes):
    key = (GUIDE_CAB, GUIDE_EVENT)
    obj = objects.get(key)
    if obj is None:
        raise ValueError("Publisher guide event fingerprint differs.")
    data = edited.get(key, obj.read_typetree())
    calls = data["onFinished"]["m_PersistentCalls"]["m_Calls"]
    call = calls[1]
    if call["m_Target"] != {"m_FileID": 0, "m_PathID": 0} or call["m_MethodName"] != "PlayLabel" or call["m_Mode"] != 5 \
        or call["m_Arguments"]["m_StringArgument"] != "test" or call["m_CallState"] != 2:
        raise ValueError("Guide callback is not the known inert null test call.")
    calls.pop(1)
    obj.save_typetree(data)
    edited[key] = data
    changes.append({"kind": "remove-known-inert-guide-test-call", "asset_file": GUIDE_CAB, "path_id": GUIDE_EVENT,
        "game_object": 4001898385668503837, "game_object_name": "PlayableGuide_c001_near", "field": "onFinished", "call_index": 1, "removed_call": call})

def _rewrite_file(task):
    import UnityPy
    source, relative, destination, bindings, *cleanup = task
    source, destination = Path(source), Path(destination)
    env = UnityPy.load(str(source))
    objects = _object_map(env)
    before = {key: obj.get_raw_data() for key, obj in objects.items()}
    edited, raw_edits, removed, changes = {}, {}, set(), []
    interesting = {}
    missing_keys = set()
    for key, obj in objects.items():
        if obj.type.name == "MonoScript":
            data = obj.read_typetree()
            name = (data["m_Namespace"] + "." if data["m_Namespace"] else "") + data["m_ClassName"]
            identity = (data["m_AssemblyName"], name)
            target = bindings.get(identity, identity)
            if target == identity:
                continue
            data["m_Namespace"], _, data["m_ClassName"] = target[1].rpartition(".")
            data["m_AssemblyName"] = target[0]
            obj.save_typetree(data)
            edited[key] = data
            changes.append({"kind": "MonoScript", "asset_file": key[0], "path_id": key[1], "source": identity, "target": target})
        elif obj.type.name == "MonoBehaviour":
            if obj.serialized_type.node is None:
                output, updates = _rewrite_stripped_strings(before[key], bindings, obj.assets_file.header.endian)
                if updates:
                    obj.set_raw_data(output)
                    raw_edits[key] = output
                    changes.append({"kind": "stripped-clr-identities", "asset_file": key[0], "path_id": key[1], "fields": updates})
                continue
            type_key = (obj.assets_file.name, obj.type_id)
            if type_key not in interesting:
                interesting[type_key] = any(node.m_Name in TYPE_FIELDS or node.m_Type == "managedReferenceRegistry" for node in obj.serialized_type.node.traverse())
            if not interesting[type_key]:
                continue
            data = obj.read_typetree()
            updates = _rewrite_type_fields(data, bindings)
            if updates:
                obj.save_typetree(data)
                edited[key] = data
                changes.append({"kind": "serialized-clr-identities", "asset_file": key[0], "path_id": key[1], "fields": updates})
    for file in {obj.assets_file.name: obj.assets_file for obj in objects.values()}.values():
        for item in file.ref_types or []:
            if item.m_ClassName is None:
                continue
            name = (item.m_NameSpace + "." if item.m_NameSpace else "") + item.m_ClassName
            identity = (item.m_AssemblyName, name)
            target = bindings.get(identity, identity)
            if target != identity:
                item.m_NameSpace, _, item.m_ClassName = target[1].rpartition(".")
                item.m_AssemblyName = target[0]
                file.mark_changed()
                changes.append({"kind": "serialized-managed-reference-type", "asset_file": file.name, "source": identity, "target": target})
    if relative == POPUP_FILE:
        _cleanup_popup(objects, edited, removed, changes)
    if relative == GUIDE_FILE:
        _cleanup_guide(objects, edited, changes)
    if cleanup and cleanup[0] is not None:
        missing_keys = _cleanup_missing_components(source, objects, before, cleanup[0], edited, removed, changes)
    if not changes:
        return None
    root_file = next(iter(env.files.values()))
    output = root_file.save(packer="original") if root_file.__class__.__name__ == "BundleFile" else root_file.save()
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(output)
    reopened = UnityPy.load(str(destination))
    after_objects = _object_map(reopened)
    if set(after_objects) != set(before) - removed:
        raise ValueError("Resource object IDs changed unexpectedly: " + relative)
    for key, obj in after_objects.items():
        if key in edited:
            if obj.read_typetree() != edited[key]:
                raise ValueError("Rewritten object has unexpected semantic fields: " + relative + " " + str(key))
        elif key in raw_edits:
            if obj.get_raw_data() != raw_edits[key]:
                raise ValueError("Rewritten stripped object has unexpected bytes: " + relative)
        elif obj.get_raw_data() != before[key]:
            raise ValueError("Unmodified resource object payload changed: " + relative + " " + str(key))
    if missing_keys and any(_missing_incoming(after_objects, missing_keys).values()):
        raise ValueError("Removed missing component still has an incoming reference: " + relative)
    result = {"path": relative, "input_sha256": digest(source), "output_sha256": digest(destination),
        "objects_verified": len(before), "objects_rewritten": len(edited) + len(raw_edits), "objects_removed": len(removed), "changes": changes}
    if missing_keys:
        result["missing_component_integrity"] = {"publisher_assembly_sha256": "57d0517f1e4c7bd6211d35fdc1806c19ed5ed39b3d48c4a52662cf052b038db6",
            "removed_objects": len(missing_keys), "remaining_incoming_references": 0,
            "all_other_object_payloads_verified": True, "animator_behaviour_vector_indices_preserved": True}
    if relative.startswith("StandaloneOSX/MDPro3/"):
        result["bundle_integrity"] = {"source_crc": _bundle_crc(source), "source_bytes": source.stat().st_size,
            "crc": _bundle_crc(destination), "bytes": destination.stat().st_size,
            "cache_hash_bytes": hashlib.md5(output).hexdigest()}
    return result

def _ensure_toolchain(root: Path) -> tuple[Path, dict]:
    env = root / ".tools/client-ui-resources"
    python = env / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")
    setup = root / "scripts/setup-client-ui-resources.py"
    if not (env / "manifest.json").is_file():
        subprocess.run([sys.executable, str(setup)], cwd=root, check=True, stdout=sys.stderr)
    subprocess.run([str(python), str(setup), "--verify"], cwd=root, check=True, stdout=sys.stderr)
    return python, json.loads((env / "manifest.json").read_text())

def _inputs(root: Path) -> dict[str, str]:
    vendor = root / "vendor/MDPro3-mac"
    data = vendor / "MDPro3.app/Contents/Resources/Data"
    paths = [data / name for name in BOOTSTRAP] + sorted(path for path in (vendor / "StandaloneOSX").rglob("*") if path.is_file())
    paths += [vendor / CATALOG_PATH, vendor / CATALOG_HASH_PATH]
    expected = dict((name, value) for value, name in (line.split("  ", 1) for line in (root / "vendor/MDPro3-mac-metadata/checksums.txt").read_text().splitlines() if line))
    result = {}
    for path in paths:
        relative = path.relative_to(vendor).as_posix()
        value = digest(path)
        if value != expected.get(relative):
            raise ValueError("Publisher resource differs from its immutable manifest: " + relative)
        result[relative] = value
    return result

def _cache_identity(root: Path, toolchain: dict) -> dict:
    return {"schema": 1, "bindings_sha256": digest(root / BINDINGS_PATH),
        "missing_components_sha256": digest(root / CLEANUP_PATH),
        "mapper_sha256": digest(Path(__file__)), "catalog_mapper_sha256": digest(root / "scripts/client_ui_catalog.py"),
        "publisher_manifest_sha256": digest(root / "vendor/MDPro3-mac-metadata/checksums.txt"),
        "toolchain": toolchain}

def _build_cache(root: Path, cache: Path, identity: dict, inputs: dict, cache_key: str) -> None:
    vendor = root / "vendor/MDPro3-mac"
    bindings = load_bindings(root)
    cleanup = load_missing_components(root)
    cache.mkdir(parents=True, exist_ok=True)
    if not set(cleanup["files"]).issubset(inputs):
        raise ValueError("Missing-component cleanup contains a file outside the publisher input inventory.")
    tasks = [(str(vendor / path), path, str(cache / "files" / path), bindings, cleanup["files"].get(path)) for path in inputs if path not in {CATALOG_PATH, CATALOG_HASH_PATH}]
    results = []
    with concurrent.futures.ProcessPoolExecutor(max_workers=4) as pool:
        for result in pool.map(_rewrite_file, tasks, chunksize=24):
            if result is not None:
                results.append(result)
    catalog = Catalog((vendor / CATALOG_PATH).read_bytes())
    bundle_integrity = {result["path"]: result["bundle_integrity"] for result in results if "bundle_integrity" in result}
    derived, catalog_changes = catalog.rewrite(bindings, bundle_integrity)
    if derived != (vendor / CATALOG_PATH).read_bytes():
        path = cache / "files" / CATALOG_PATH
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(derived)
        hash_path = cache / "files" / CATALOG_HASH_PATH
        hash_path.write_text(hashlib.md5(derived).hexdigest())
        results.extend([{"path": CATALOG_PATH, "input_sha256": inputs[CATALOG_PATH], "output_sha256": digest(path), "changes": catalog_changes},
            {"path": CATALOG_HASH_PATH, "input_sha256": inputs[CATALOG_HASH_PATH], "output_sha256": digest(hash_path), "changes": [{"kind": "catalog-cache-content-version", "algorithm": "md5-128", "value": hashlib.md5(derived).hexdigest()}]}])
    if _cache_identity(root, identity["toolchain"]) != identity:
        raise ValueError("Resource mapper or bindings changed during cache construction.")
    record = {**identity, "cache_key": cache_key, "inputs": inputs,
        "outputs": {result["path"]: result["output_sha256"] for result in results}, "changes": results,
        "policy": "Publisher files are immutable. Serialized script/managed/event type identities are rebound; no component implementation bridge is used. Unmodified object payloads are byte-identical. Only fingerprinted inert popup/test-callback records and publisher components with proven missing CLR implementations are removed. Valid GO=0 script objects are retained; missing AnimatorController behaviour pointers become null while native vector indices/ranges remain unchanged. Changed bundle CRCs cover the complete uncompressed UnityFS data stream; appended MD5-128 content-version tokens prevent reuse of publisher cache entries."}
    temporary_manifest = cache / "manifest.json.tmp"
    with temporary_manifest.open("w") as stream:
        stream.write(json.dumps(record, indent=2, sort_keys=True) + "\n")
        stream.flush()
        os.fsync(stream.fileno())
    os.replace(temporary_manifest, cache / "manifest.json")

def plan_client_resources(root: Path) -> tuple[dict[str, Path], dict]:
    root = Path(root).resolve()
    cache_root = root / "build/.client-ui-resources"
    cache_root.mkdir(parents=True, exist_ok=True)
    with (cache_root / ".toolchain.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        python, toolchain = _ensure_toolchain(root)
    identity = _cache_identity(root, toolchain)
    key = hashlib.sha256(json.dumps(identity, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    cache = cache_root / key
    manifest = cache / "manifest.json"
    with (cache_root / (key + ".lock")).open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        inputs = _inputs(root)
        if not manifest.is_file():
            pending = Path(tempfile.mkdtemp(prefix=".pending-" + key[:12] + "-", dir=cache_root))
            try:
                subprocess.run([str(python), str(Path(__file__).resolve()), "--build-cache", "--root", str(root), "--cache", str(pending), "--cache-key", key], cwd=root, check=True)
                if cache.exists():
                    shutil.rmtree(cache)
                os.replace(pending, cache)
            finally:
                if pending.exists():
                    shutil.rmtree(pending)
        record = json.loads(manifest.read_text())
    if any(record.get(name) != value for name, value in identity.items()) or record.get("inputs") != inputs:
        raise ValueError("Resource cache manifest differs from current source inputs.")
    files = {relative: cache / "files" / relative for relative in record["outputs"]}
    for relative, path in files.items():
        if path.is_symlink() or not path.is_file() or digest(path) != record["outputs"][relative]:
            raise ValueError("Derived resource cache file differs: " + relative)
    return files, record

def verify_client_resources(root: Path, client: Path, expected: dict | None = None) -> dict:
    files, record = plan_client_resources(root)
    if expected is not None and expected != record:
        raise ValueError("Packaged resource mapping evidence differs from its source generation.")
    for relative in files:
        path = Path(client) / relative
        if path.is_symlink() or not path.is_file() or digest(path) != record["outputs"][relative]:
            raise ValueError("Packaged client resource differs from its bound derived output: " + relative)
    return record

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--build-cache", action="store_true")
    parser.add_argument("--cache", type=Path)
    parser.add_argument("--cache-key")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    if args.build_cache:
        toolchain = json.loads((args.root / ".tools/client-ui-resources/manifest.json").read_text())
        identity = _cache_identity(args.root, toolchain)
        key = hashlib.sha256(json.dumps(identity, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
        if key != args.cache_key:
            raise ValueError("Resource cache inputs changed before worker startup.")
        _build_cache(args.root, args.cache, identity, _inputs(args.root), key)
    else:
        files, record = plan_client_resources(args.root)
        if args.report:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            args.report.write_text(json.dumps(record, indent=2, sort_keys=True) + "\n")
        print(json.dumps({"ok": True, "files_rebound": len(files), "cache_key": record["cache_key"]}))
