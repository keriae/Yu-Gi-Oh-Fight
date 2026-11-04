#!/usr/bin/env python3
"""Acquire missing published game files in small, resumable update phases."""

import argparse
import fcntl
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.request
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
METADATA = ROOT / "vendor/MDPro3-mac-metadata"
DESTINATION = ROOT / "vendor/MDPro3-mac"
CHECKSUMS = METADATA / "checksums.txt"
STATE = METADATA / "resource-phases.json"
URL = "https://sapi.moecube.com:444/release/update/update/mdpro3/1.4.7-2?platform=darwin&locale=zh-CN&arch=arm64"
NS = {"m": "urn:ietf:params:xml:ns:metalink"}
GROUPS = [
    ("art", "Picture/Art/"),
    ("art2", "Picture/Art2/"),
    ("closeup", "Picture/Closeup/"),
    ("masterduel", "StandaloneOSX/MasterDuel/"),
    ("mdpro", "StandaloneOSX/MDPro3/"),
    ("cutin", "StandaloneOSX/MonsterCutin/"),
    ("cutin2", "StandaloneOSX/MonsterCutin2/"),
    ("video", "Video/"),
    ("misc", ""),
]
PATTERN = re.compile(r"^(art|art2|closeup|masterduel|mdpro|cutin|cutin2|video|misc)-(\d{3})\.request\.json$")
CACHE = {}


def emit(message):
    print(message, flush=True)


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for data in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(data)
    return digest.hexdigest()


def save(state):
    temporary = STATE.with_suffix(".json.tmp")
    temporary.write_text(json.dumps(state, indent=2) + "\n")
    temporary.replace(STATE)


def file_matches(name, expected):
    path = DESTINATION / name
    if not path.is_file():
        return False
    stat = path.stat()
    signature = (stat.st_ino, stat.st_size, stat.st_mtime_ns, expected)
    if CACHE.get(name) == signature:
        return True
    if sha256(path) == expected:
        CACHE[name] = signature
        return True
    return False


def prepare_phase(name, paths):
    request_path = METADATA / f"{name}.request.json"
    metalink_path = METADATA / f"{name}.metalink.xml"
    if request_path.exists() and json.loads(request_path.read_text()) != paths:
        raise RuntimeError("An existing phase has a different file selection")
    request_path.write_text(json.dumps(paths, indent=2) + "\n")
    if not metalink_path.exists():
        last_error = None
        for attempt in range(3):
            try:
                request = urllib.request.Request(URL, data=json.dumps(paths).encode(),
                                                 headers={"Content-Type": "application/json"})
                with urllib.request.urlopen(request, timeout=55) as response:
                    data = response.read(16 * 1024 * 1024 + 1)
                if len(data) > 16 * 1024 * 1024:
                    raise RuntimeError("Update metadata exceeds expected size")
                ET.fromstring(data)
                metalink_path.write_bytes(data)
                break
            except Exception as error:
                last_error = error
                if attempt == 2:
                    raise RuntimeError(f"Could not obtain public phase metadata: {last_error}")
                time.sleep((attempt + 1) * 2)
    packages = ET.fromstring(metalink_path.read_bytes()).findall("m:file", NS)
    size = sum(int(item.find("m:size", NS).text) for item in packages)
    emit(f"Phase {name}: {len(paths)} requested files, {len(packages)} packages, {size:,} bytes")
    return size


def run_phase(name, state):
    start = time.monotonic()
    paths = json.loads((METADATA / f"{name}.request.json").read_text())
    size = prepare_phase(name, paths)
    state["current_phase"] = name
    save(state)
    environment = os.environ.copy()
    environment.pop("MDPRO3_APP_FIRST", None)
    environment["MDPRO3_SELECTION"] = name
    environment["MDPRO3_DOWNLOAD_WORKERS"] = str(state.get("download_workers", 8))
    environment["MDPRO3_RANGE_WORKERS"] = "8"
    # A resumed phase repairs removed files from the already-verified archives.
    environment["MDPRO3_REEXTRACT"] = "1"
    result = subprocess.run([sys.executable, str(ROOT / "scripts/acquire-mdpro3-mac.py")],
                            cwd=ROOT, env=environment)
    if result.returncode:
        raise RuntimeError(f"Phase {name} did not complete; its metadata and cache are retained")
    report = json.loads((METADATA / f"{name}.file-verification.json").read_text())
    if not report["passed"] or report["checked_files"] != len(paths):
        raise RuntimeError("The completed phase did not verify all requested files")
    state["completed_phases"].append({"name": name, "files": len(paths), "package_bytes": size,
                                      "elapsed_seconds": round(time.monotonic() - start, 2),
                                      "verified_utc": report["verified_utc"]})
    state["current_phase"] = None
    save(state)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch-size", type=int, default=500)
    parser.add_argument("--download-workers", type=int, default=8)
    parser.add_argument("--build-complete", action="store_true")
    args = parser.parse_args()
    if not 100 <= args.batch_size <= 2000:
        raise RuntimeError("Batch size must be between 100 and 2000 files")
    if not 1 <= args.download_workers <= 8:
        raise RuntimeError("Download worker count must be between 1 and 8")
    METADATA.mkdir(parents=True, exist_ok=True)
    lock = (METADATA / "resource-phases.lock").open("a")
    try:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    except BlockingIOError:
        raise RuntimeError("A resource phase downloader is already running")
    spec = importlib.util.spec_from_file_location("mac_acquire", ROOT / "scripts/acquire-mdpro3-mac.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.validate_checksum_manifest(CHECKSUMS.read_bytes())
    expected = {}
    for line in CHECKSUMS.read_text().splitlines():
        if not line:
            continue
        if line.startswith("  "):
            name = line[2:]
            directory = DESTINATION / name
            if not directory.resolve().is_relative_to(DESTINATION.resolve()):
                raise RuntimeError("A publisher directory resolves outside the destination")
            directory.mkdir(parents=True, exist_ok=True)
        else:
            digest, name = line.split("  ", 1)
            expected[name] = digest
    state = json.loads(STATE.read_text()) if STATE.exists() else {"version": "1.4.7-2", "completed_phases": [], "current_phase": None}
    state["download_workers"] = args.download_workers
    state["batch_size"] = args.batch_size
    state["build_complete_after_verification"] = args.build_complete
    completed = {phase["name"] for phase in state["completed_phases"]}
    counters = {}
    pending = []
    for path in sorted(METADATA.glob("*.request.json")):
        match = PATTERN.match(path.name)
        if not match:
            continue
        prefix, index = match.groups()
        counters[prefix] = max(counters.get(prefix, 0), int(index))
        name = path.name.removesuffix(".request.json")
        report_path = METADATA / f"{name}.file-verification.json"
        if name in completed:
            continue
        if report_path.exists() and json.loads(report_path.read_text()).get("passed"):
            report = json.loads(report_path.read_text())
            state["completed_phases"].append({"name": name, "files": report["checked_files"],
                                              "verified_utc": report["verified_utc"], "imported": True})
            completed.add(name)
        else:
            pending.append(name)
    if state["current_phase"] and state["current_phase"] not in completed:
        name = state["current_phase"]
        if name in pending:
            pending.remove(name)
        pending.insert(0, name)
    for name in pending:
        run_phase(name, state)
    save(state)
    for prefix, folder in GROUPS:
        while True:
            paths = [name for name, digest in expected.items() if name.startswith(folder) and not file_matches(name, digest)][:args.batch_size]
            if not paths:
                break
            counters[prefix] = counters.get(prefix, 0) + 1
            name = f"{prefix}-{counters[prefix]:03d}"
            (METADATA / f"{name}.request.json").write_text(json.dumps(paths, indent=2) + "\n")
            state["current_phase"] = name
            save(state)
            run_phase(name, state)
    module.APP_FIRST = False
    report = module.verify_payload(CHECKSUMS)
    state["full_file_verification"] = report
    state["current_phase"] = None
    save(state)
    emit(f"All {len(expected):,} publisher-hashed native game files verified")
    if args.build_complete:
        stamp = time.strftime("%Y%m%dT%H%M%SZ", time.gmtime())
        log_path = METADATA / f"complete-app-build-{stamp}.log"
        command = [sys.executable, str(ROOT / "scripts/build-mdpro3-offline.py")]
        record = {"command": command, "log": str(log_path), "started_utc": stamp,
                  "application": str(ROOT / "dist/Yu-Gi-Oh! Fight.app")}
        state["complete_build"] = record
        save(state)
        emit(f"Full verified payload ready; building separate app, log: {log_path}")
        with log_path.open("wb") as log:
            result = subprocess.run(command, cwd=ROOT, stdout=log, stderr=subprocess.STDOUT)
            log.flush()
            os.fsync(log.fileno())
        record["exit_code"] = result.returncode
        record["finished_utc"] = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        save(state)
        if result.returncode:
            raise RuntimeError(f"Complete-app build failed with exit {result.returncode}; retained log: {log_path}")
        emit(f"Complete native app built: {record['application']}")


if __name__ == "__main__":
    main()
