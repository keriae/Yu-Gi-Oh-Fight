#!/usr/bin/env python3
"""Seed a verified, staged native Client with an offline deck and locale defaults."""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import stat
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DECK_KEY = "Blue-Eyes"
DECKS = {
    "Deck/Blue Eyes.ydk": "416ab3ea0be3ff98227e60d60b5918eb5cd5564697d9c751e4bf897c0c9ea326",
    "Data/Windbot/Decks/AI_BlueEyes.ydk": "592c765a10758ddfcae20fec40ccea7eea42bb1e12fdf2a827165597fa059a71",
}
DEFAULT_CONFIG = (
    "DeckInUse->Blue Eyes\r\n"
).encode("utf-8")
DIALOGS = {
    "zh-CN": "Universal.zh-CN", "zh-TW": "Universal_6.zh-TW",
    "en-US": "Universal.EN", "ja-JP": "Universal.JP", "ko-KR": "Universal.KR",
    "fr-FR": "Universal.FR", "de-DE": "Universal.DE", "es-ES": "Universal.ES",
    "it-IT": "Universal.IT", "pt-PT": "Universal.PT",
}
DECK_ARGUMENT = re.compile(r"(?:^|\s)Deck=(?:'([^']*)'|\"([^\"]*)\"|([^\s]+))(?:\s|$)")


def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def checked_file(client: Path, relative: str, *, absent: bool = False) -> Path:
    target = client / relative
    for path in [target, *target.parents]:
        if path == client:
            break
        if path.is_symlink():
            raise ValueError("Staged data must not contain symlinked paths: " + relative)
    if client not in target.resolve().parents:
        raise ValueError("Data path escaped the staged Client: " + relative)
    if not (absent and not target.exists()) and not target.is_file():
        raise ValueError("Missing packaged regular file: " + relative)
    return target


def staged_client(path: Path) -> Path:
    client = path.resolve(strict=True)
    if (ROOT / "vendor").resolve() in [client, *client.parents]:
        raise ValueError("Refusing to modify publisher files under vendor.")
    resources, contents, app = client.parent, client.parent.parent, client.parent.parent.parent
    if (client.name != "Client" or resources.name != "Resources" or contents.name != "Contents"
            or app.suffix != ".app" or not app.parent.name.startswith(".independent-build.")
            or not (resources / ".independent-build").is_file()):
        raise ValueError("Defaults may only be seeded inside a marked, staged application Client.")
    return client


def contains_deck(data: bytes) -> bool:
    lines = data.decode("utf-8-sig").splitlines()
    for index, line in enumerate(lines[:-1]):
        if line.strip().startswith("!"):
            match = DECK_ARGUMENT.search(lines[index + 1].strip())
            if match and next(value for value in match.groups() if value is not None) == DECK_KEY:
                return True
    return False


def atomic_write(target: Path, data: bytes) -> None:
    mode = stat.S_IMODE(target.stat().st_mode) if target.exists() else 0o644
    handle, temporary = tempfile.mkstemp(prefix=".offline-default-", dir=target.parent)
    try:
        with os.fdopen(handle, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, mode)
        os.replace(temporary, target)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def seed(client_path: Path, source_manifest: Path | None = None) -> dict:
    client = staged_client(client_path)
    if source_manifest is None:
        raise ValueError("A verified compiled-source manifest is required to seed defaults.")
    compiled = json.loads(source_manifest.read_text())
    if not compiled.get("ok") or not compiled.get("source_compiled") or not compiled.get("validation_verified"):
        raise ValueError("Source defaults require a successful compiled-source validation manifest.")
    assembly_sha = sha(checked_file(client, "MDPro3.app/Contents/Resources/Data/Managed/Assembly-CSharp.dll").read_bytes())
    if assembly_sha != compiled["assembly_sha256"]:
        raise ValueError("Staged assembly does not match the compiled-source manifest.")
    prerequisites = []
    for relative, expected in DECKS.items():
        actual = sha(checked_file(client, relative).read_bytes())
        if actual != expected:
            raise ValueError("Unsupported bundled deck SHA-256: " + relative)
        prerequisites.append({"path": relative, "sha256": actual})
    checked_file(client, "Data/locales/zh-CN/cards.cdb")
    bots = sorted((client / "Data/locales").glob("*/bot.conf"))
    if not bots:
        raise ValueError("No packaged locale bot lists were found.")
    updates = []
    changes = []
    for bot in bots:
        relative = bot.relative_to(client).as_posix()
        bot = checked_file(client, relative)
        before = bot.read_bytes()
        after = before
        present = contains_deck(before)
        if not present:
            locale = bot.parent.name
            dialog = DIALOGS.get(locale, "default")
            checked_file(client, "Data/Windbot/Dialogs/" + dialog + ".json")
            newline = "\r\n" if b"\r\n" in before else "\n"
            chinese = locale in ("zh-CN", "zh-TW")
            title = "青眼白龙（单机）" if locale == "zh-CN" else "青眼白龍（單機）" if chinese else "Blue-Eyes (Offline)"
            description = "使用内置 AI_BlueEyes 牌组的本地电脑对手。" if locale == "zh-CN" else "使用內置 AI_BlueEyes 牌組的本地電腦對手。" if chinese else "Local computer opponent using the bundled AI_BlueEyes deck."
            entry = newline.join([
                "!" + title, "Name=Blue-Eyes Deck=" + DECK_KEY + " Dialog=" + dialog,
                description, "AI_LV3 SUPPORT_MASTER_RULE_3 SUPPORT_NEW_MASTER_RULE SUPPORT_MASTER_RULE_2020", "",
            ]).encode("utf-8")
            separator = newline.encode("ascii") if before.endswith((b"\n", b"\r")) else (newline * 2).encode("ascii")
            after = before + separator + entry
            updates.append((bot, after))
        changes.append({"path": relative, "action": "preserved_existing_deck" if present else "appended_offline_deck",
                        "before_sha256": sha(before), "after_sha256": sha(after)})
    config = checked_file(client, "Data/config.conf", absent=True)
    before_config = config.read_bytes() if config.exists() else None
    if before_config is None:
        updates.append((config, DEFAULT_CONFIG))
    changes.append({"path": "Data/config.conf", "action": "created" if before_config is None else "preserved_existing_config",
                    "before_sha256": None if before_config is None else sha(before_config),
                    "after_sha256": sha(DEFAULT_CONFIG if before_config is None else before_config)})
    # All paths and prerequisites are checked before the first data mutation.
    for target, data in updates:
        atomic_write(target, data)
    return {"ok": True, "schema": 1, "client": str(client), "changed_files": len(updates),
            "assembly_sha256": assembly_sha, "prerequisites": prerequisites,
            "executor": {"type": "WindBot.Game.AI.Decks.BlueEyesExecutor", "registered_name": DECK_KEY,
                         "deck_file": "AI_BlueEyes", "level": "Normal",
                         "publisher_custom_attribute_blob": "010009426c75652d457965730b41495f426c756545796573064e6f726d616c0000",
                         "registry": "DecksManager.Init uses DeckAttribute.Name as the exact dictionary key"},
            "changes": changes}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--client", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    args = parser.parse_args()
    if (ROOT / "vendor").resolve() in [args.output.resolve(), *args.output.resolve().parents]:
        print(json.dumps({"ok": False, "error": "Refusing to write a report under vendor."}))
        raise SystemExit(1)
    try:
        report = seed(args.client, args.manifest)
    except (ValueError, OSError, UnicodeError) as error:
        report = {"ok": False, "error": str(error)}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps(report, ensure_ascii=False))
    raise SystemExit(0 if report["ok"] else 1)


if __name__ == "__main__":
    main()
