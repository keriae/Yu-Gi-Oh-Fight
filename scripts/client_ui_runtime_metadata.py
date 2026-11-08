"""Derive fixed-player initialization metadata for the project-owned UI types.

The publisher input remains immutable. Callers write the returned bytes only to
their independent staging client and bind the returned record to its manifest.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path


RUNTIME_INITIALIZE_PATH = "MDPro3.app/Contents/Resources/Data/RuntimeInitializeOnLoads.json"
RUNTIME_METADATA_PATH = RUNTIME_INITIALIZE_PATH
BINDINGS_PATH = "content/client-ui/serialized-bindings.json"
PUBLISHER_SHA256 = "20df609cd242ab8eb1228a2948958caa00443048d2dcf8c7eb7760dbeb806d62"


def _digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def _encoded(value: object, *, sort_keys: bool = False) -> bytes:
    return (json.dumps(value, ensure_ascii=False, separators=(",", ":"),
                       sort_keys=sort_keys) + "\n").encode("utf-8")


def _unique_object(pairs: list[tuple[str, object]]) -> dict:
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate runtime metadata key: " + key)
        result[key] = value
    return result


def _validate_bindings(contract: dict) -> dict[str, str]:
    if not isinstance(contract, dict) or contract.get("version") != 1:
        raise ValueError("Unsupported client resource bindings.")
    bindings = contract.get("bindings")
    if not isinstance(bindings, dict) or not bindings:
        raise ValueError("Missing client resource bindings.")
    for source, target in bindings.items():
        if not isinstance(source, str) or not isinstance(target, str) or ":" not in source or ":" not in target:
            raise ValueError("Invalid client resource binding: " + str(source))
    return bindings


def map_runtime_initialize_bytes(publisher_bytes: bytes, contract: dict) -> tuple[bytes, dict]:
    """Return deterministic derived bytes and evidence without reading or writing files."""
    publisher_sha = _digest(publisher_bytes)
    if publisher_sha != PUBLISHER_SHA256:
        raise ValueError("Unsupported publisher RuntimeInitializeOnLoads.json bytes.")
    bindings = _validate_bindings(contract)
    metadata = json.loads(publisher_bytes, object_pairs_hook=_unique_object)
    entries = metadata.get("root") if isinstance(metadata, dict) else None
    if not isinstance(entries, list):
        raise ValueError("Invalid publisher runtime initialization table.")
    mapping = []
    assembly_entry_count = 0
    for index, entry in enumerate(entries):
        if not isinstance(entry, dict) or not all(isinstance(entry.get(key), str)
                                                for key in ("assemblyName", "nameSpace", "className", "methodName")):
            raise ValueError("Invalid runtime initialization entry: " + str(index))
        if entry["assemblyName"] != "Assembly-CSharp":
            continue
        assembly_entry_count += 1
        before = (entry["nameSpace"] + "." if entry["nameSpace"] else "") + entry["className"]
        target = bindings.get("Assembly-CSharp:" + before, "Assembly-CSharp:" + before)
        assembly, after = target.split(":", 1)
        if assembly != "Assembly-CSharp":
            raise ValueError("Startup callback must belong to the client assembly: " + target)
        if before == after:
            continue
        namespace, separator, class_name = after.rpartition(".")
        if not separator:
            namespace, class_name = "", after
        if not class_name:
            raise ValueError("Invalid mapped runtime initialization type: " + after)
        mapping.append({"index": index, "assembly_name": entry["assemblyName"],
                        "method_name": entry["methodName"], "original_type": before,
                        "implementation_type": after, "load_type": entry["loadTypes"],
                        "is_unity_class": entry["isUnityClass"]})
        entry["nameSpace"] = namespace
        entry["className"] = class_name
    derived = _encoded(metadata)
    record = {"schema": 1, "bindings_canonical_sha256": _digest(_encoded(contract, sort_keys=True)),
              "entry_count": len(entries), "assembly_entry_count": assembly_entry_count,
              "mapping": mapping,
              "changes": [{"path": RUNTIME_METADATA_PATH, "publisher_sha256": publisher_sha,
                           "sha256": _digest(derived)}]}
    return derived, record


def plan_runtime_metadata(root: Path) -> tuple[dict[str, bytes], dict]:
    """Validate immutable inputs and plan staging updates, without writing any files."""
    bindings_bytes = (root / BINDINGS_PATH).read_bytes()
    contract = json.loads(bindings_bytes, object_pairs_hook=_unique_object)
    publisher = (root / "vendor/MDPro3-mac" / RUNTIME_METADATA_PATH).read_bytes()
    derived, record = map_runtime_initialize_bytes(publisher, contract)
    record.update({"bindings_path": BINDINGS_PATH, "bindings_sha256": _digest(bindings_bytes),
                   "mapper_sha256": _digest(Path(__file__).read_bytes())})
    return {RUNTIME_METADATA_PATH: derived}, record


def verify_runtime_metadata(root: Path, client: Path, expected: dict | None = None) -> dict:
    """Require current inputs, bound evidence, and exact packaged metadata to agree."""
    files, record = plan_runtime_metadata(root)
    if expected is not None and expected != record:
        raise ValueError("Client UI runtime metadata evidence differs from its source inputs.")
    for relative, data in files.items():
        path = client / relative
        if not path.is_file() or path.is_symlink() or path.read_bytes() != data:
            raise ValueError("Packaged client UI runtime metadata differs from its mapped bytes: " + relative)
    return record
