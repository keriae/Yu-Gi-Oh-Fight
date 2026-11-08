#!/usr/bin/env python3
"""Check native rule-prompt literals against the pinned MDPro3 source."""
import argparse
import hashlib
import json
import re
import tarfile
from datetime import datetime, timedelta, timezone
from pathlib import Path


METHODS = [
    "GameMessage_SelectEffectYn", "GameMessage_SelectChain", "GameMessage_SelectYesNo",
    "GameMessage_SelectCard", "GameMessage_SelectUnselect", "GameMessage_SelectTribute",
    "GameMessage_SelectSum", "GameMessage_SelectOption", "GameMessage_SortChain",
    "GameMessage_AnnounceRace", "GameMessage_AnnounceAttrib", "GameMessage_AnnounceNumber",
    "GameMessage_AnnounceCard",
]
POPUPS = [
    "UI/Popup/Old/PopupDuelSelection.cs", "UI/Popup/Old/PopupDuelYesOrNo.cs",
    "UI/Popup/Old/PopupDuelSelectCard.cs", "UI/Popup/Old/PopupDuelInput.cs",
    "UI/Popup/PopupSelection.cs", "UI/Popup/PopupYesOrNo.cs", "UI/Popup/PopupInput.cs",
    "UI/Popup/PopupConfirm.cs", "UI/Popup/PopupText.cs",
]
PRESENTERS = [
    "UI/Client/FightDuelPopupSkin.cs", "UI/Client/FightDuelPopupLayout.cs",
    "UI/Client/FightDuelActionComposition.cs",
]
TOKEN = re.compile(r'//[^\n]*|/\*.*?\*/|(?:\$?@|@\$)"(?:""|[^\"])*"|\$?"(?:\\.|[^"\\])*"', re.S)


def sha(data):
    return hashlib.sha256(data).hexdigest()


def literals(source):
    return [match.group() for match in TOKEN.finditer(source)
            if not match.group().startswith(("//", "/*"))]


def method_literals(source, name):
    match = re.search(r"async UniTask " + re.escape(name) + r"\(", source)
    if match is None:
        raise ValueError("Required method missing: " + name)
    end = source.find("protected override async UniTask", match.start() + 10)
    return literals(source[match.start():end if end >= 0 else len(source)])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    root = args.root.resolve()
    output = args.output or root / "artifacts/mdpro3-rule-copy.json"
    vendor = root / "vendor/MDPro3-release-source"
    manifest = json.loads((vendor / "manifest.json").read_text())
    archive_info = next(item for item in manifest["archives"] if item["path"] == "Assets-Scripts.tar.gz")
    archive = vendor / archive_info["path"]
    archive_sha = sha(archive.read_bytes())
    if archive_sha != archive_info["sha256"]:
        raise ValueError("Pinned source archive SHA-256 differs")
    source_hashes = {}
    checks = []
    with tarfile.open(archive) as bundle:
        def sources(suffix):
            path = "Assets/Scripts/MDPro3/" + suffix
            member = next(name for name in bundle.getnames() if name.endswith("/" + path))
            original = bundle.extractfile(member).read()
            expected = archive_info["files"][path]
            if sha(original) != expected:
                raise ValueError("Pinned source member SHA-256 differs: " + path)
            current_path = "game/Assets/Scripts/YuGiOhFight/Client/" + suffix
            current = (root / current_path).read_bytes()
            source_hashes[suffix] = {"current_path": current_path, "current_sha256": sha(current),
                                     "upstream_sha256": expected}
            return original.decode("utf-8-sig"), current.decode("utf-8-sig")

        original, current = sources("Duel/Message/DuelMessage.cs")
        for name in METHODS:
            before, after = method_literals(original, name), method_literals(current, name)
            check = {"kind": "rule_method", "name": name, "literal_count": len(before), "identical": before == after}
            if before != after:
                check.update(upstream_literals=before, current_literals=after)
            checks.append(check)
        for path in POPUPS:
            original, current = sources(path)
            before, after = literals(original), literals(current)
            check = {"kind": "popup", "path": path, "literal_count": len(before), "identical": before == after}
            if before != after:
                check.update(upstream_literals=before, current_literals=after)
            checks.append(check)

    presentation = []
    for path in PRESENTERS:
        current_path = "game/Assets/Scripts/YuGiOhFight/Client/" + path
        data = (root / current_path).read_bytes()
        text = data.decode("utf-8-sig")
        presentation.append({"path": current_path, "sha256": sha(data),
                             "writes_text": bool(re.search(r"\.text\s*=", text)),
                             "adds_raw_caption": "FightUI.RawText(" in text,
                             "client_copy_translation": "FightText.Get(" in text})
    ok = all(item["identical"] for item in checks) and all(
        not item["writes_text"] and not item["adds_raw_caption"] and not item["client_copy_translation"]
        for item in presentation)
    report = {
        "ok": ok,
        "created_at": datetime.now(timezone(timedelta(hours=8))).isoformat(timespec="seconds"),
        "upstream": {"commit": manifest["commit"], "tag": manifest["tag"],
                     "archive_sha256": archive_sha, "manifest_sha256": sha((vendor / "manifest.json").read_bytes())},
        "scope": {"rule_method_count": len(METHODS), "popup_count": len(POPUPS)},
        "source_hashes": source_hashes,
        "checks": checks,
        "presentation": presentation,
        "limits": "Checks exact C# string literals after excluding comments, not an exhaustive proof of dynamic card data or translations. Native hint/selections/args forwarding and publisher phase captions were additionally reviewed; UI geometry and font styling may differ.",
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({"ok": ok, "rule_methods": len(METHODS), "popups": len(POPUPS), "report": str(output)}))
    return 0 if ok else 1


if __name__ == "__main__":
    raise SystemExit(main())
