#!/usr/bin/env python3
"""Install or verify the portable resource-rebinding Python environment."""
from __future__ import annotations
import argparse
import base64
import csv
import hashlib
import importlib.metadata
import io
import json
from pathlib import Path
import platform
import subprocess
import sys
import venv

ROOT = Path(__file__).resolve().parents[1]
ENV = ROOT / ".tools/client-ui-resources"
REQUIREMENTS = ROOT / "tools/client-ui-resources/requirements.txt"

def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()

def canonical(name: str) -> str:
    return name.lower().replace("_", "-").replace(".", "-")

def inspect() -> dict:
    pinned = dict(line.strip().split("==") for line in REQUIREMENTS.read_text().splitlines() if line.strip())
    installed = {canonical(distribution.metadata["Name"]): distribution for distribution in importlib.metadata.distributions()}
    packages = {}
    for name, version in pinned.items():
        distribution = installed.get(canonical(name))
        if distribution is None or distribution.version != version:
            raise ValueError("Resource dependency version differs: " + name)
        record = distribution.read_text("RECORD")
        if record is None:
            raise ValueError("Dependency wheel RECORD is missing: " + name)
        files = 0
        for relative, encoded, size in csv.reader(io.StringIO(record)):
            if not encoded:
                continue
            algorithm, value = encoded.split("=", 1)
            if algorithm != "sha256":
                raise ValueError("Unsupported dependency file hash: " + algorithm)
            path = Path(distribution.locate_file(relative))
            actual = base64.urlsafe_b64encode(hashlib.sha256(path.read_bytes()).digest()).decode().rstrip("=")
            if actual != value or size and path.stat().st_size != int(size):
                raise ValueError("Resource dependency file differs: " + name + "/" + relative)
            files += 1
        packages[name] = {"version": version, "record_sha256": hashlib.sha256(record.encode()).hexdigest(), "files_verified": files}
    return {"schema": 1, "requirements_sha256": sha(REQUIREMENTS), "setup_sha256": sha(Path(__file__)),
        "python": {"version": platform.python_version(), "implementation": platform.python_implementation(),
            "platform": sys.platform, "architecture": platform.machine()}, "packages": packages}

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--verify", action="store_true")
    parser.add_argument("--wheelhouse", type=Path)
    parser.add_argument("--inside", action="store_true", help=argparse.SUPPRESS)
    args = parser.parse_args()
    if sys.version_info < (3, 12):
        raise ValueError("Client resource setup requires Python 3.12 or newer.")
    python = ENV / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")
    manifest = ENV / "manifest.json"
    if not args.inside:
        if not python.is_file():
            if args.verify:
                raise ValueError("Run python3 scripts/setup-client-ui-resources.py first.")
            venv.EnvBuilder(with_pip=True).create(ENV)
        if not args.verify:
            command = [str(python), "-m", "pip", "install", "--disable-pip-version-check", "--prefer-binary", "-r", str(REQUIREMENTS)]
            if args.wheelhouse:
                command.extend(["--no-index", "--find-links", str(args.wheelhouse.resolve())])
            subprocess.run(command, cwd=ROOT, check=True)
        command = [str(python), str(Path(__file__).resolve()), "--inside"]
        if args.verify:
            command.append("--verify")
        subprocess.run(command, cwd=ROOT, check=True)
        return
    record = inspect()
    if args.verify:
        if not manifest.is_file() or json.loads(manifest.read_text()) != record:
            raise ValueError("Resource toolchain manifest differs from installed dependencies.")
    else:
        manifest.write_text(json.dumps(record, indent=2, sort_keys=True) + "\n")
    print(json.dumps({"ok": True, "manifest": str(manifest), "requirements_sha256": record["requirements_sha256"], "packages": len(record["packages"])}))

if __name__ == "__main__":
    main()
