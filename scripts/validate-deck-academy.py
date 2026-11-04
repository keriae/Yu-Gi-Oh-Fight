#!/usr/bin/env python3
"""Validate the curated academy against the exact bundled cards, scripts and list."""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sqlite3
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
CONTENT = ROOT / "content/deck-academy"
CLIENT = ROOT / "vendor/MDPro3-mac"
EXTRA_TYPES = 0x40 | 0x2000 | 0x800000 | 0x4000000
TOKEN_TYPE = 0x4000
FULL_REPLAY_PROOF = "native-full-replay-proof.json"
POLICIES = {"branded": "branded_policy.py", "ryzeal": "ryzeal_policy.py", "maliss": "maliss_policy.py",
            "mitsurugi": "mitsurugi_policy.py", "dracotail": "dracotail_replay.py", "k9-vs": "k9_vs_replay.py",
            "yubel": "policies/yubel.py", "tenpai": "policies/tenpai.py"}
POLICIES.update(dict.fromkeys(("hero", "blue-eyes", "dark-magician", "red-eyes", "cyber-dragon", "stardust",
                              "blackwing", "utopia", "odd-eyes", "galaxy-eyes", "dragunity", "ddd"), "classic_anime.py"))
POLICIES.update(dict.fromkeys(("tearlaments", "sky-striker", "eldlich", "shaddoll", "invoked", "swordsoul",
                              "tri-brigade", "zoodiac", "salamangreat", "dragonmaid", "traptrix", "labrynth",
                              "dark-world", "lightsworn", "thunder-dragon", "orcust", "phantom-knights",
                              "burning-abyss", "mathmech", "spright"), "classic_meta.py"))
POLICIES.update(dict.fromkeys(("slifer", "obelisk", "ra", "sacred-beasts", "runick", "dinosaurs", "ancient-gear",
                              "six-samurai", "magnet-warrior", "numeron", "gravekeeper", "gadget"), "classic_gods.py"))
PUBLISHER_ASSEMBLY_SHA = "57d0517f1e4c7bd6211d35fdc1806c19ed5ed39b3d48c4a52662cf052b038db6"
RECORDER_SOURCES = (("tools/DeckAcademyProof/NativeReplayHost.cs", "host_source", "host_sha256"),
                    ("tools/DeckAcademyProof/native_session.py", "native_session_source", "native_session_sha256"),
                    ("tools/DeckAcademyProof/proof-runner.c", "native_runner_source", "native_runner_sha256"))


def replay_frames(data: bytes) -> list[dict]:
    frames, cursor = [], 0
    while cursor < len(data):
        if len(data) - cursor < 5:
            raise ValueError("Truncated replay frame header")
        length = struct.unpack_from("<I", data, cursor + 1)[0]
        end = cursor + 5 + length
        if end > len(data):
            raise ValueError("Truncated replay frame payload")
        frames.append({"function": data[cursor], "payload": data[cursor + 5:end]})
        cursor = end
    if not frames or frames[0]["function"] != 162 or frames[-1]["function"] not in (11, 5):
        raise ValueError("Replay must begin at native ReloadField and finish at a legal idle or win")
    if any(frame["function"] == 1 for frame in frames):
        raise ValueError("Replay contains a native rejected-response retry")
    return frames


def query_rows(data: bytes) -> list:
    rows, cursor = [], 0
    scalar = {1: "code", 2: "gps", 4: "alias", 8: "type", 16: "level", 32: "rank", 64: "attribute",
              128: "race", 256: "atk", 512: "def", 1024: "base_atk", 2048: "base_def", 4096: "reason",
              8192: "reason_card", 16384: "equip", 262144: "owner", 524288: "status", 1048576: "public",
              2097152: "lscale", 4194304: "rscale"}
    while cursor < len(data):
        if len(data) - cursor < 4:
            raise ValueError("Truncated native query block")
        length = struct.unpack_from("<I", data, cursor)[0]
        end = cursor + length
        if length < 4 or 4 < length < 8 or end > len(data):
            raise ValueError("Invalid native query block length")
        if length == 4:
            rows.append(None); cursor = end; continue
        flags = struct.unpack_from("<I", data, cursor + 4)[0]
        if flags & ~0xffffff:
            raise ValueError("Unsupported native query flag")
        row, at = {"flags": flags}, cursor + 8
        def number():
            nonlocal at
            if at + 4 > end:
                raise ValueError("Truncated native query field")
            value = struct.unpack_from("<I", data, at)[0]; at += 4
            return value
        for bit in (1 << i for i in range(24)):
            if not flags & bit:
                continue
            # The publisher wrapper treats IsPublic as a flag-only marker.
            if bit == 1048576:
                continue
            if bit in (32768, 65536, 131072):
                count = number()
                if count > (end - at) // 4:
                    raise ValueError("Truncated native query array")
                row[{32768: "targets", 65536: "overlay", 131072: "counters"}[bit]] = [number() for _ in range(count)]
            elif bit == 8388608:
                row["link"], row["link_marker"] = number(), number()
            elif bit in scalar:
                row[scalar[bit]] = number()
            else:
                raise ValueError("Unsupported native query flag")
        if at != end or "code" not in row:
            raise ValueError("Native query identity or field length missing")
        rows.append(row); cursor = end
    return rows


def recording_facts(data: bytes, entry: dict, deck: dict, snapshot: dict) -> dict:
    frames = replay_frames(data)
    initial, latest, first_idle = {}, {}, False
    for frame in frames:
        if frame["function"] == 7:
            raise ValueError("Unsupported per-card native UpdateCard; academy recordings require complete zone updates")
        if frame["function"] == 11:
            first_idle = True
        if frame["function"] == 6:
            payload = frame["payload"]
            if len(payload) < 2:
                raise ValueError("Truncated native UpdateData")
            key = str(payload[0]) + ":" + str(payload[1])
            latest[key] = query_rows(payload[2:])
            if not first_idle:
                initial[key] = latest[key]
    codes = lambda rows: [row["code"] for row in rows if row]
    remaining = Counter(deck["main"]) - Counter(entry["starter_hand"])
    if (codes(initial.get("0:2", [])) != entry["starter_hand"]
            or Counter(codes(initial.get("0:1", []))) != remaining
            or Counter(codes(initial.get("0:64", []))) != Counter(deck["extra"])
            or codes(initial.get("1:2", [])) != [89631139] * 5
            or codes(initial.get("1:1", [])) != [89631139] * 40):
        raise ValueError("Replay initial full deck or quiet opponent differs from the catalog")
    expected_keys = {str(player) + ":" + str(location) for player in (0, 1) for location in (1, 2, 4, 8, 16, 32, 64)}
    if not isinstance(snapshot, dict) or set(snapshot) != expected_keys:
        raise ValueError("Complete fresh native snapshot is missing")
    snapshot_fields = {"flags", "code", "gps", "alias", "type", "level", "rank", "attribute", "race",
                       "atk", "def", "base_atk", "base_def", "reason", "overlay", "status"}
    for key in expected_keys:
        recorded = latest.get(key, [])
        if not isinstance(snapshot[key], list) or len(snapshot[key]) != len(recorded):
            raise ValueError("Fresh snapshot differs from recorded native queries: " + key)
        for row, cached in zip(snapshot[key], recorded):
            if row is None or cached is None:
                if row != cached:
                    raise ValueError("Fresh snapshot differs from recorded native queries: " + key)
                continue
            if (not isinstance(row, dict) or set(row) != snapshot_fields or row["flags"] != 0x91fff
                    or any(type(row[field]) is not int for field in snapshot_fields - {"overlay"})
                    or not isinstance(row["overlay"], list) or any(type(code) is not int for code in row["overlay"])):
                raise ValueError("Complete fresh native query fields are missing or invalid: " + key)
            # Cached updates may omit unchanged fields or carry scales excluded
            # by the fresh query's mask. Every common field must match at the
            # same zone slot, including card position and attack values.
            if any(row[field] != cached[field] for field in row.keys() & cached.keys() - {"flags"}):
                raise ValueError("Fresh snapshot differs from recorded native queries: " + key)
            if key.endswith(":4") and row["overlay"] != cached.get("overlay", []):
                raise ValueError("Fresh materials differ from recorded native queries")
            if not key.endswith(":4") and row["overlay"]:
                raise ValueError("Fresh native query has overlay materials outside a monster zone")
    resources = Counter(code for key in expected_keys if key.startswith("0:") for code in codes(snapshot[key]))
    resources.update(code for key in expected_keys if key.startswith("0:")
                     for row in snapshot[key] if row for code in row["overlay"])
    if resources != Counter(deck["main"] + deck["extra"]):
        raise ValueError("Fresh native snapshot does not conserve the complete curated deck")
    board = codes(snapshot["0:4"]) + codes(snapshot["0:8"])
    if Counter(entry["end_board"]["codes"]) - Counter(board):
        raise ValueError("Fresh native snapshot does not contain the prescribed end board")
    return {"frames": len(frames), "bytes": len(data), "first_function": frames[0]["function"],
            "last_function": frames[-1]["function"], "initial_full_deck_verified": True,
            "quiet_opponent_verified": True, "fresh_snapshot_matches_recording": True,
            "complete_card_resources_conserved": True, "rejected_response_frames": 0, "update_card_frames": 0}


def step_frames(values: dict, count: int, frames: int) -> list[int]:
    if not isinstance(values, dict) or set(values) != {str(i) for i in range(1, count + 1)}:
        raise ValueError("Replay steps must cover every tutorial step")
    result = [values[str(i)] for i in range(1, count + 1)]
    if any(type(value) is not int or not 0 <= value < frames for value in result):
        raise ValueError("Replay step frame is outside the real recording")
    if any(a >= b for a, b in zip(result, result[1:])):
        raise ValueError("Replay step frames must strictly increase")
    return result


def validate_replay_binding(content: Path, client: Path, entry: dict, proof: dict, environment: dict) -> dict:
    identifier = entry["id"]
    if proof.get("id") != identifier or proof.get("ok") is not True or proof.get("complete_combo_verified") is not True:
        raise ValueError("Complete combo proof is missing or unsuccessful")
    for field in ("deck_file", "puzzle_source", "replay_source", "replay_file"):
        if proof.get(field) != entry.get(field):
            raise ValueError("Replay proof path binding mismatch: " + field)
    for source, field in ((entry["deck_file"], "deck_sha256"), (entry["puzzle_source"], "puzzle_sha256"), (entry["replay_source"], "replay_sha256")):
        if sha(checked_path(content, source)) != proof.get(field):
            raise ValueError("Replay proof source hash mismatch: " + field)
    if entry.get("replay_sha256") != proof.get("replay_sha256"):
        raise ValueError("Catalog replay hash differs from complete proof")
    if proof.get("starter_hand") != entry["starter_hand"]:
        raise ValueError("Replay proof fixed hand differs from catalog")
    expected_policy = "tools/DeckAcademyProof/" + POLICIES[identifier]
    if proof.get("policy_source") != expected_policy or sha(checked_path(ROOT, expected_policy)) != proof.get("policy_sha256"):
        raise ValueError("Replay policy source hash mismatch")
    for source, path_field, hash_field in RECORDER_SOURCES:
        if environment.get(path_field) != source or sha(checked_path(ROOT, source)) != environment.get(hash_field):
            raise ValueError("Replay recorder source hash mismatch: " + hash_field)
    for source, field in (("Data/script.zip", "script_zip_sha256"), ("Data/locales/zh-CN/cards.cdb", "card_database_sha256"),
                          ("MDPro3.app/Contents/PlugIns/libocgcore.dylib", "native_library_sha256")):
        if sha(checked_path(client, source)) != environment.get(field):
            raise ValueError("Replay publisher input hash mismatch: " + field)
    if environment.get("wrapper_assembly_sha256") != PUBLISHER_ASSEMBLY_SHA:
        raise ValueError("Replay wrapper is not the pinned original publisher assembly")
    data = checked_path(content, entry["replay_source"]).read_bytes()
    facts = recording_facts(data, entry, ydk_sections(content / entry["deck_file"]), proof.get("native_final_queries"))
    if proof.get("frames") != facts["frames"] or proof.get("bytes") != facts["bytes"]:
        raise ValueError("Complete proof frame or byte count differs from the real recording")
    if proof.get("checks") != facts:
        raise ValueError("Complete proof checks differ from verified recording facts")
    values = step_frames(proof.get("step_frames"), len(entry["steps"]), facts["frames"])
    if [step.get("replay_frame") for step in entry["steps"]] != values:
        raise ValueError("Catalog replay steps differ from complete proof")
    parser = proof.get("shipping_parser", {})
    if (parser.get("ok") is not True or parser.get("byte_exact_frame_round_trip") is not True
            or parser.get("assembly_sha256") != PUBLISHER_ASSEMBLY_SHA
            or parser.get("package_count") != facts["frames"] or parser.get("reconstructed_bytes") != facts["bytes"]
            or parser.get("reconstructed_sha256") != proof.get("replay_sha256")):
        raise ValueError("Original shipping parser round-trip proof is not bound to the recording")
    return facts


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def checked_path(root: Path, relative: str) -> Path:
    if not isinstance(relative, str) or not relative or "\\" in relative or ":" in relative or "\x00" in relative:
        raise ValueError("Invalid relative content path")
    pure = PurePosixPath(relative)
    if pure.is_absolute() or ".." in pure.parts or pure.as_posix() != relative or relative == ".":
        raise ValueError("Unsafe relative content path: " + relative)
    path = root / relative
    for parent in [path, *path.parents]:
        if parent == root:
            break
        if parent.is_symlink():
            raise ValueError("Content path traverses a symbolic link: " + relative)
    if not path.is_file():
        raise ValueError("Missing content file: " + relative)
    return path


def ydk_sections(path: Path) -> dict[str, list[int]]:
    """Honor MDPro3 metadata sections, including commented pickup card IDs."""
    sections = {name: [] for name in ("main", "extra", "side", "pickup")}
    section = None
    for raw in path.read_text(encoding="utf-8-sig").splitlines():
        line = raw.strip()
        if line in ("#main", "#extra", "!side", "#pickup"):
            section = line[1:]
        elif line.startswith("#") and not line[1:].isdigit():
            if line.startswith(("#protector", "#case", "#field", "#grave", "#stand", "#mate")):
                section = None
        elif section and (line.isdigit() or section == "pickup" and line.startswith("#") and line[1:].isdigit()):
            sections[section].append(int(line.lstrip("#")))
    return sections


def regulation(path: Path, name: str) -> dict[int, int]:
    quantities, active, found = {}, False, False
    for raw in path.read_text(encoding="utf-8-sig").splitlines():
        line = raw.strip()
        if line.startswith("!"):
            active = line[1:] == name
            found |= active
        elif active and re.match(r"^\d+\s+[0-3](?:\s|$)", line):
            code, quantity = line.split()[:2]
            quantities[int(code)] = int(quantity)
    if not found:
        raise ValueError("Bundled regulation is missing: " + name)
    return quantities


def validate_recommendations(catalog: dict, content: Path, client: Path, cards: dict) -> dict:
    library = catalog.get("library")
    if library is None:
        return {"legacy_catalog": True}
    if library.get("schema") != 1 or not isinstance(library.get("decks"), list) or not library["decks"]:
        raise ValueError("Invalid recommendation library")
    academy = {entry["id"]: entry for entry in catalog["decks"]}
    ids, names, sources, lessons = set(), set(), set(), set()
    bound_lessons = set()
    pages = {"competitive": 0, "classic": 0}
    limits = regulation(checked_path(client, "Data/lflist.conf"), catalog["environment"]["regulation"])
    with zipfile.ZipFile(checked_path(client, "Data/script.zip")) as archive:
        scripts = set(archive.namelist())
    curated, originals, themes = 0, 0, set()
    tutorial_titles = {entry.get("academy_id"): entry.get("name") for entry in library["decks"]
                       if str(entry.get("source_file", "")).startswith("Decks/") and entry.get("academy_id")}
    for entry in library["decks"]:
        identifier, name = entry.get("id"), entry.get("name")
        source, legacy = entry.get("source_file"), entry.get("legacy_name")
        if (not isinstance(identifier, str) or not re.fullmatch(r"[a-z0-9-]{1,64}", identifier)
                or identifier in ids or not isinstance(name, str) or not name or name != name.strip()
                or name in names or re.search(r'[A-Za-zぁ-ゖァ-ヺ/\\:*?"<>|\x00-\x1f]', name)
                or entry.get("page") not in pages or not isinstance(source, str)
                or not re.fullmatch(r"(?:Decks|BundledDecks|CuratedDecks)/[^/\\]+\.ydk", source)
                or source in sources or legacy != Path(source).stem):
            raise ValueError("Invalid or nonstandard recommendation metadata")
        ids.add(identifier); names.add(name); sources.add(source)
        pages[entry["page"]] += 1
        path = checked_path(content, source)
        if source not in catalog["files"] or sha(path) != catalog["files"][source]:
            raise ValueError("Recommendation source is not bound to content inventory")
        sections = ydk_sections(path)
        playable = sections["main"] + sections["extra"] + sections["side"]
        if (not 40 <= len(sections["main"]) <= 60 or len(sections["extra"]) > 15 or len(sections["side"]) > 15
                or not isinstance(entry.get("cover_card"), int) or entry["cover_card"] not in playable
                or entry["cover_card"] not in cards):
            raise ValueError("Recommendation deck size or cover card is invalid")
        lesson = entry.get("academy_id")
        if source.startswith("CuratedDecks/"):
            theme = entry.get("theme_id")
            if (entry["page"] != "classic" or not isinstance(theme, str)
                    or not re.fullmatch(r"[a-z0-9-]{1,64}", theme)
                    or any(not isinstance(entry.get(key), str) or not entry[key].strip()
                           for key in ("variant", "description", "win_condition"))
                    or not isinstance(entry.get("tags"), list) or not entry["tags"]
                    or any(not isinstance(tag, str) or not tag.strip() for tag in entry["tags"])
                    or entry.get("regulation") != catalog["environment"]["regulation"]
                    or len(sections["main"]) != 40 or sections["side"]
                    or entry.get("publisher_source") is not None):
                raise ValueError("Curated recommendation lacks its theme, strategy or fixed regulation")
            if lesson is not None:
                if lesson not in academy or theme != lesson:
                    raise ValueError("Curated recommendation links an unrelated theme lesson")
                title = entry.get("tutorial_name")
                if (not isinstance(title, str) or not title.strip() or title != title.strip()
                        or re.search(r'[A-Za-zぁ-ゖァ-ヺ/\\:*?"<>|\x00-\x1f]', title)
                        or tutorial_titles.setdefault(lesson, title) != title):
                    raise ValueError("Curated recommendation has an inconsistent base tutorial name")
                lessons.add(lesson)
            starters = entry.get("starter_cards")
            if (not isinstance(starters, list) or not starters or len(set(starters)) != len(starters)
                    or any(code not in sections["main"] for code in starters)):
                raise ValueError("Curated recommendation has invalid opening cards")
            for section in ("main", "extra"):
                for code in sections[section]:
                    card = cards.get(code)
                    if card is None or card["type"] & TOKEN_TYPE:
                        raise ValueError("Curated recommendation has a missing card or token: " + str(code))
                    if bool(card["type"] & EXTRA_TYPES) != (section == "extra"):
                        raise ValueError("Curated recommendation has a card in the wrong section: " + str(code))
                    script_code = card["alias"] or code
                    if ("script/c" + str(script_code) + ".lua" not in scripts
                            and "script/c" + str(code) + ".lua" not in scripts
                            and not (card["type"] & 0x1 and card["type"] & 0x10)):
                        raise ValueError("Curated recommendation is missing a card script: " + str(code))
            aliases = Counter(cards[code]["alias"] or code for code in playable)
            if any(count > limits.get(code, 3) for code, count in aliases.items()):
                raise ValueError("Curated recommendation exceeds alias-aware regulation counts")
            curated += 1
            themes.add(theme)
        elif source.startswith("Decks/"):
            if lesson not in academy or academy[lesson]["deck_file"] != source or lesson in bound_lessons:
                raise ValueError("Recommendation lesson differs from its template")
            bound_lessons.add(lesson)
            lessons.add(lesson)
        elif lesson is not None or entry.get("publisher_source") not in (
                "Deck/" + Path(source).name, "Data/Windbot/Decks/" + Path(source).name):
            raise ValueError("Original recommendation source metadata is invalid")
        elif sha(checked_path(client, entry["publisher_source"])) != sha(path):
            raise ValueError("Original recommendation differs from the publisher deck")
        else:
            originals += 1
    legacy_names = set()
    for entry in library.get("legacy_presets", []):
        source, name = entry.get("source_file"), entry.get("name")
        if (not isinstance(source, str) or not re.fullmatch(r"(?:Decks|BundledDecks)/[^/\\]+\.ydk", source)
                or name != Path(source).stem or name in legacy_names
                or source not in catalog["files"] or sha(checked_path(content, source)) != catalog["files"][source]):
            raise ValueError("Invalid preserved legacy preset index")
        legacy_names.add(name)
    if lessons != set(academy):
        raise ValueError("Recommendation library does not cover all academy lessons")
    return {"decks": len(ids), "pages": pages, "lessons": len(lessons),
            "originals": originals, "curated": curated, "classic_themes": len(themes),
            "legacy_presets": len(legacy_names), "standard_chinese_names": True, "cover_cards_verified": True,
            "curated_cards_scripts_sections_and_regulation_verified": True}


def validate_library(content: Path = CONTENT, client: Path = CLIENT) -> dict:
    content, client = Path(content).absolute(), Path(client).absolute()
    errors, decks = [], []
    report = {"ok": False, "errors": errors, "decks": decks,
              "scope": "Bound file hashes, bundled card/script availability, alias-aware regulation, complete fixed-hand native recordings, fresh final queries and frame synchronization. Visual playback is verified separately in the game."}
    try:
        manifest_path = checked_path(content, "manifest.json")
        catalog = json.loads(manifest_path.read_text())
        if catalog.get("schema") != 1 or not isinstance(catalog.get("decks"), list) or not catalog["decks"]:
            raise ValueError("Invalid academy catalog schema")
        inventory = catalog.get("files")
        if not isinstance(inventory, dict) or not inventory:
            raise ValueError("Missing academy hash inventory")
        actual = set()
        for path in content.rglob("*"):
            if path.is_symlink():
                raise ValueError("Academy content contains a symbolic link")
            if path.is_file() and path != manifest_path:
                actual.add(path.relative_to(content).as_posix())
        if actual != set(inventory):
            raise ValueError("Academy hash inventory differs from files: " + str(sorted(actual ^ set(inventory))))
        for relative, digest in inventory.items():
            if not isinstance(digest, str) or not re.fullmatch(r"[0-9a-f]{64}", digest):
                raise ValueError("Invalid academy SHA-256: " + relative)
            if sha(checked_path(content, relative)) != digest:
                raise ValueError("Academy file hash mismatch: " + relative)
        env = catalog.get("environment", {})
        if not isinstance(env.get("regulation"), str) or not env.get("scope") or not env.get("label"):
            raise ValueError("Missing fixed-environment context")
        card_path = checked_path(client, "Data/locales/zh-CN/cards.cdb")
        script_path = checked_path(client, "Data/script.zip")
        list_path = checked_path(client, "Data/lflist.conf")
        with sqlite3.connect(card_path.as_uri() + "?mode=ro", uri=True) as database:
            cards = {row[0]: {"alias": row[1], "type": row[2], "name": row[3]}
                     for row in database.execute("SELECT datas.id,alias,type,name FROM datas JOIN texts ON datas.id=texts.id")}
        report["recommendations"] = validate_recommendations(catalog, content, client, cards)
        with zipfile.ZipFile(script_path) as archive:
            scripts = set(archive.namelist())
        limits = regulation(list_path, env["regulation"])
        full_proof = None
        replay_ids = {item.get("id") for item in catalog["decks"] if item.get("replay_source") or item.get("replay_file")}
        if replay_ids:
            if catalog.get("replay_proof") != FULL_REPLAY_PROOF or FULL_REPLAY_PROOF not in inventory:
                raise ValueError("Automatic replays require a bound complete replay proof")
            full_proof = json.loads(checked_path(content, FULL_REPLAY_PROOF).read_text())
            if (full_proof.get("schema") != 1 or full_proof.get("kind") != "shipping-wrapper-complete-academy-replays"
                    or full_proof.get("ok") is not True or not isinstance(full_proof.get("decks"), list)
                    or len(full_proof["decks"]) != len(replay_ids)
                    or {item.get("id") for item in full_proof["decks"]} != replay_ids):
                raise ValueError("Complete replay proof deck inventory differs from catalog")
        source_hashes, identifiers, names = {}, set(), set()
        for entry in catalog["decks"]:
            identifier = entry.get("id", "")
            item_errors = []
            item = {"id": identifier, "name": entry.get("name"), "errors": item_errors, "ok": False}
            decks.append(item)
            try:
                if not isinstance(identifier, str) or not re.fullmatch(r"[a-z][a-z0-9-]*", identifier) or identifier in identifiers:
                    raise ValueError("Invalid or duplicate deck ID")
                identifiers.add(identifier)
                if not isinstance(entry.get("name"), str) or not entry["name"] or entry["name"] in names:
                    raise ValueError("Missing or duplicate deck name")
                names.add(entry["name"])
                expected_deck = "Decks/" + entry["name"] + ".ydk"
                if entry.get("deck_file") != expected_deck:
                    raise ValueError("Deck filename does not match its catalog name")
                deck_path = checked_path(content, expected_deck)
                if sha(deck_path) != entry.get("deck_sha256"):
                    raise ValueError("Deck catalog hash mismatch")
                if entry.get("regulation") != env["regulation"]:
                    raise ValueError("Deck regulation differs from its fixed environment")
                sources = entry.get("source")
                if not isinstance(sources, list) or not sources:
                    raise ValueError("Missing deck sources")
                for source in sources:
                    source_name = source.get("path")
                    digest = source.get("sha256")
                    if source_name not in source_hashes:
                        source_hashes[source_name] = sha(checked_path(client, source_name))
                    if source_hashes[source_name] != digest:
                        raise ValueError("Bundled reference hash mismatch: " + source_name)
                sections = ydk_sections(deck_path)
                counts = {key: len(sections[key]) for key in ("main", "extra", "side")}
                item["counts"] = counts
                if counts["main"] != 40 or not 0 <= counts["extra"] <= 15 or counts["side"] != 0:
                    raise ValueError("Academy decks must contain 40 main, 0 to 15 extra, 0 side cards")
                if entry.get("category") not in (None, "当前精选", "动画经典", "经典竞技", "神与经典"):
                    raise ValueError("Invalid deck category")
                if entry.get("difficulty") not in (None, "入门", "进阶", "熟练"):
                    raise ValueError("Invalid tutorial difficulty")
                if "tags" in entry and (not isinstance(entry["tags"], list) or len(entry["tags"]) > 16
                        or any(not isinstance(tag, str) or not tag or len(tag) > 64 for tag in entry["tags"])):
                    raise ValueError("Invalid deck search tags")
                playable = sections["main"] + sections["extra"] + sections["side"]
                for key in ("main", "extra"):
                    for code in sections[key]:
                        if code not in cards:
                            raise ValueError("Card is missing from bundled database: " + str(code))
                        card = cards[code]
                        if card["type"] & TOKEN_TYPE:
                            raise ValueError("A playable deck contains a token: " + str(code))
                        if bool(card["type"] & EXTRA_TYPES) != (key == "extra"):
                            raise ValueError("Card is placed in the wrong deck section: " + str(code))
                        script_code = card["alias"] or code
                        if "script/c" + str(script_code) + ".lua" not in scripts and "script/c" + str(code) + ".lua" not in scripts:
                            # Normal monsters without a built-in effect need no Lua script.
                            if card["type"] & 0x1 and card["type"] & 0x10:
                                continue
                            raise ValueError("Card script is missing: " + str(code))
                aliases = Counter(cards[code]["alias"] or code for code in playable)
                for code, count in aliases.items():
                    if count > limits.get(code, 3):
                        raise ValueError("Regulation count exceeded: " + str(code) + " (" + str(count) + ")")
                hand = entry.get("starter_hand", [])
                if len(hand) != 5 or Counter(hand) - Counter(sections["main"]):
                    raise ValueError("Fixed starter hand is not five cards from the main deck")
                starters = entry.get("starter_cards", [])
                if not starters or Counter(starters) - Counter(hand):
                    raise ValueError("Core starters are absent from the fixed hand")
                if not entry.get("description") or not entry.get("cautions") or not entry.get("end_board", {}).get("text"):
                    raise ValueError("Incomplete tutorial overview or cautions")
                referenced = list(starters) + entry["end_board"].get("codes", []) + sections["pickup"]
                step_references = []
                if not isinstance(entry.get("steps"), list) or not entry["steps"]:
                    raise ValueError("Missing tutorial steps")
                for step in entry["steps"]:
                    if not step.get("title") or not step.get("text") or not step.get("card_codes"):
                        raise ValueError("Incomplete tutorial step")
                    step_references += step["card_codes"]
                if any(code not in playable for code in referenced):
                    raise ValueError("Tutorial or pickup references a card outside its deck")
                generated_tokens = set(step_references) - set(playable)
                if any(code not in cards or not cards[code]["type"] & TOKEN_TYPE for code in generated_tokens):
                    raise ValueError("Tutorial step references a card outside its deck")
                for kind, extension in (("puzzle", ".lua"), ("replay", ".yrp3d")):
                    source, runtime = entry.get(kind + "_source"), entry.get(kind + "_file")
                    if kind == "replay" and source is None and runtime is None:
                        item["automatic_replay"] = False
                        continue
                    if source != kind.title() + "/" + identifier + extension or runtime != kind.title() + "/Academy/" + identifier + extension:
                        raise ValueError("Tutorial source/runtime path mismatch: " + kind)
                    checked_path(content, source)
                    if source not in inventory:
                        raise ValueError("Unbound tutorial asset: " + source)
                    if kind == "puzzle":
                        puzzle = (content / source).read_text()
                        supplied = [int(code) for code in re.findall(r"Debug\.AddCard\((\d+),0,0,LOCATION_HAND,\d+,POS_FACEDOWN\)", puzzle)]
                        if supplied != hand:
                            raise ValueError("Practice Lua hand differs from the catalog")
                        for location, expected in (("DECK", Counter(sections["main"]) - Counter(hand)), ("EXTRA", Counter(sections["extra"]))):
                            supplied = Counter(int(code) for code in re.findall(r"Debug\.AddCard\((\d+),0,0,LOCATION_" + location + r",\d+,POS_FACEDOWN\)", puzzle))
                            if supplied != expected:
                                raise ValueError("Practice Lua " + location + " differs from the catalog")
                        if "Debug.ReloadFieldEnd()" not in puzzle:
                            raise ValueError("Practice Lua did not finish setup")
                    else:
                        proof = next(item for item in full_proof["decks"] if item["id"] == identifier)
                        item["replay_validation"] = validate_replay_binding(content, client, entry, proof, full_proof.get("environment", {}))
                        item["automatic_replay"] = True
                        if generated_tokens:
                            observed = set()
                            for frame in replay_frames((content / source).read_bytes()):
                                if frame["function"] == 6 and frame["payload"][:2] == bytes((0, 4)):
                                    observed.update(row["code"] for row in query_rows(frame["payload"][2:]) if row)
                            if generated_tokens - observed:
                                raise ValueError("Tutorial token was not generated in the native recording")
                            item["generated_tutorial_tokens"] = sorted(generated_tokens)
                if generated_tokens and not item.get("automatic_replay"):
                    raise ValueError("Generated tutorial tokens require a complete native recording")
                item["starter_hand"] = hand
                item["unique_cards"] = len(set(playable))
                item["alias_aware_regulation"] = True
                item["ok"] = True
            except (KeyError, TypeError, ValueError, OSError) as exception:
                item_errors.append(str(exception))
                errors.append(identifier + ": " + str(exception))
        report.update({"manifest_sha256": sha(manifest_path), "regulation": env["regulation"],
                       "card_database_sha256": sha(card_path), "script_zip_sha256": sha(script_path),
                       "regulation_sha256": sha(list_path), "bound_files": len(inventory)})
    except (KeyError, TypeError, ValueError, OSError, sqlite3.Error, zipfile.BadZipFile) as exception:
        errors.append(str(exception))
    report["ok"] = not errors and bool(decks)
    return report


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--content", type=Path, default=CONTENT)
    parser.add_argument("--client", type=Path, default=CLIENT)
    parser.add_argument("--output", "--report", dest="output", type=Path, default=ROOT / "artifacts/mdpro3-deck-academy-validation.json")
    args = parser.parse_args()
    report = validate_library(args.content, args.client)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({"ok": report["ok"], "decks": len(report["decks"]), "errors": report["errors"]}, ensure_ascii=False))
    if not report["ok"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
