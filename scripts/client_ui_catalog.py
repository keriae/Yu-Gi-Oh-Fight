"""Pinned Addressables v2 binary storage reader and precise cache-record edits.

The field layout mirrors the publisher's ContentCatalogData and
BinaryStorageBuffer serializers. Existing offsets never move: replacements
append data and update only the referring typed records.
"""
from __future__ import annotations
import hashlib
import struct

NULL = 0xffffffff

class Catalog:
    def __init__(self, data: bytes):
        self.data = data
        self.header = self.values(0, 8)
        if self.header[:2] != (0x0de38942, 2):
            raise ValueError("Unsupported publisher binary Addressables catalog.")
        self.locations = {}
        self.keys = {}
        self.type_records = {}
        self.type_assembly_strings = {}
        for key_id, locations_id in self.array(self.header[2], 2):
            key = self.object_value(key_id)
            ids = [row[0] for row in self.array(locations_id)]
            if not isinstance(key, str) or key in self.keys:
                raise ValueError("Catalog has a non-string or duplicate key.")
            self.keys[key] = ids
            for id in ids:
                self.location(id)

    def values(self, offset: int, count: int = 1) -> tuple[int, ...]:
        if offset < 0 or offset + count * 4 > len(self.data):
            raise ValueError("Catalog offset is outside its buffer.")
        return struct.unpack_from("<" + "I" * count, self.data, offset)

    def array(self, id: int, width: int = 1) -> list[tuple[int, ...]]:
        if id == NULL:
            return []
        size = self.values(id - 4)[0]
        if size % (width * 4) or id + size > len(self.data):
            raise ValueError("Invalid catalog value-array size.")
        return [self.values(offset, width) for offset in range(id, id + size, width * 4)]

    def string(self, id: int, separator: str = "\0") -> str | None:
        if id == NULL:
            return None
        if separator != "\0" and id & 0x40000000:
            parts, seen = [], set()
            while id != NULL:
                if id in seen:
                    raise ValueError("Catalog dynamic string has a cycle.")
                seen.add(id)
                string_id, id = self.values(id & 0x3fffffff, 2)
                parts.append(self.string(string_id))
            return separator.join(reversed(parts))
        unicode = bool(id & 0x80000000)
        offset = id & 0x3fffffff if unicode else id
        size = self.values(offset - 4)[0]
        if offset + size > len(self.data):
            raise ValueError("Catalog string is outside its buffer.")
        return self.data[offset:offset + size].decode("utf-16-le" if unicode else "ascii")

    def type(self, id: int) -> tuple[str, str]:
        assembly_id, class_id = self.values(id, 2)
        assembly = self.string(assembly_id, ".")
        result = assembly.split(",", 1)[0], self.string(class_id, ".")
        if not all(isinstance(value, str) and value for value in result):
            raise ValueError("Catalog type has no assembly or class identity.")
        self.type_records[id] = result
        self.type_assembly_strings[id] = assembly
        return result

    def object_value(self, id: int):
        if id == NULL:
            return None
        type_id, value_id = self.values(id, 2)
        assembly, name = self.type(type_id)
        if name == "System.String":
            string_id = self.values(value_id)[0]
            separator = chr(struct.unpack_from("<H", self.data, value_id + 4)[0])
            return self.string(string_id, separator)
        if name == "System.Int32":
            return struct.unpack_from("<i", self.data, value_id)[0]
        if name == "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions":
            hash_id, name_id, crc, size, common_id = self.values(value_id, 5)
            self.values(hash_id, 4)
            self.values(common_id, 2)
            return {"offset": value_id, "hash_offset": hash_id, "hash_bytes": self.data[hash_id:hash_id + 16].hex(),
                "bundle_name": self.string(name_id, "_"), "crc": crc, "bundle_size": size,
                "common_offset": common_id, "common_bytes": self.data[common_id:common_id + 8].hex()}
        raise ValueError("Unsupported catalog object type: " + assembly + ":" + name)

    def location(self, id: int) -> dict:
        if id in self.locations:
            return self.locations[id]
        primary, internal, provider, deps, dep_hash, extra, type_id = self.values(id, 7)
        result = {"primary_key": self.string(primary, "/"), "internal_id": self.string(internal, "/"),
            "provider": self.string(provider, "."), "resource_type": self.type(type_id),
            "dependency_hash": dep_hash, "dependencies": [row[0] for row in self.array(deps)], "data": self.object_value(extra)}
        self.locations[id] = result
        for dependency in result["dependencies"]:
            self.location(dependency)
        return result

    def graph(self) -> dict:
        def identity(id):
            item = self.locations[id]
            return item["provider"] + "|" + item["internal_id"]
        locations = {}
        for id, row in self.locations.items():
            locations.setdefault(identity(id), {"primary_key": row["primary_key"], "internal_id": row["internal_id"],
                "provider": row["provider"], "resource_type": row["resource_type"][1],
                "dependencies": [identity(dependency) for dependency in row["dependencies"]]})
        return {"keys": {key: [identity(id) for id in ids] for key, ids in self.keys.items()}, "locations": locations}

    def rewrite(self, bindings: dict[tuple[str, str], tuple[str, str]], bundles: dict[str, dict]) -> tuple[bytes, list[dict]]:
        data = bytearray(self.data)
        changes = []
        def append(value: bytes) -> int:
            data.extend(b"\0" * (-len(data) % 4))
            offset = len(data)
            data.extend(value)
            return offset
        def string(value: str) -> int:
            unicode = not value.isascii()
            encoded = value.encode("utf-16-le" if unicode else "ascii")
            offset = append(struct.pack("<I", len(encoded)) + encoded) + 4
            return offset | (0x80000000 if unicode else 0)
        for offset, source in self.type_records.items():
            target = bindings.get(source, source)
            if target != source:
                assembly = target[0] + self.type_assembly_strings[offset][len(source[0]):]
                struct.pack_into("<II", data, offset, string(assembly), string(target[1]))
                changes.append({"kind": "catalog-resource-type", "offset": offset, "source": source, "target": target})
        modified = set()
        for row in self.locations.values():
            options = row["data"]
            if not isinstance(options, dict) or options["offset"] in modified:
                continue
            relative = row["internal_id"].replace("{UnityEngine.AddressableAssets.Addressables.RuntimePath}/", "MDPro3.app/Contents/Resources/Data/StreamingAssets/")
            relative = relative.replace("{MDPro3.Addressables.InternalID.Path}/", "")
            bundle = bundles.get(relative)
            if bundle is None:
                # Publisher internal IDs use runtime substitutions. Match the
                # fixed filename only when its input identity is unique.
                candidates = [item for path, item in bundles.items() if path.rsplit("/", 1)[-1] == relative.rsplit("/", 1)[-1]]
                if len(candidates) > 1:
                    raise ValueError("Ambiguous catalog bundle filename: " + relative)
                bundle = candidates[0] if candidates else None
            if bundle is None:
                continue
            # A zero publisher CRC deliberately disables CRC validation. The
            # caller already checks every source against its pinned SHA-256.
            if (options["crc"] != 0 and options["crc"] != bundle["source_crc"]) or options["bundle_size"] != bundle["source_bytes"]:
                raise ValueError("Publisher catalog CRC/size does not match its bundle: " + relative)
            hash_offset = append(bytes.fromhex(bundle["cache_hash_bytes"]))
            struct.pack_into("<I", data, options["offset"], hash_offset)
            struct.pack_into("<II", data, options["offset"] + 8, bundle["crc"], bundle["bytes"])
            modified.add(options["offset"])
            changes.append({"kind": "catalog-bundle-integrity", "internal_id": row["internal_id"], "options_offset": options["offset"],
                "before": options, "after": {"hash_bytes": bundle["cache_hash_bytes"], "crc": bundle["crc"], "bundle_size": bundle["bytes"]}})
        if len(modified) != len(bundles):
            raise ValueError("Not every changed Addressables bundle has exactly one catalog options record.")
        rewritten = bytes(data)
        after = Catalog(rewritten)
        expected_graph = self.graph()
        type_names = {source[1]: target[1] for source, target in bindings.items()}
        for row in expected_graph["locations"].values():
            row["resource_type"] = type_names.get(row["resource_type"], row["resource_type"])
        if after.graph() != expected_graph:
            raise ValueError("Catalog keys/provider IDs/dependencies changed during resource rebinding.")
        return rewritten, changes
