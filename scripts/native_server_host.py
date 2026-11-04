#!/usr/bin/env python3
"""Build and verify the signed universal host for the unchanged native server."""
from __future__ import annotations

import argparse
import fcntl
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

from native_core import LIBRARY_HASH

ROOT = Path(__file__).resolve().parents[1]
HOST_PATH = "MDPro3.app/Contents/Helpers/ygoserver-host"
LIBRARY_PATH = "MDPro3.app/Contents/PlugIns/libygoserver.dylib"
SOURCE_PATH = "native/ygoserver-host.c"
ARCHITECTURES = ["arm64", "x86_64"]
IDENTIFIER = "local.mdpro3.ygoserver-host"


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_binary(path: Path, metadata: dict) -> None:
    expected = {"schema": 1, "path": HOST_PATH, "source_path": SOURCE_PATH,
                "source_sha256": digest(ROOT / SOURCE_PATH), "library_path": LIBRARY_PATH,
                "library_sha256": LIBRARY_HASH, "architectures": ARCHITECTURES,
                "minimum_macos": "13.0", "signature_verified": True}
    if any(metadata.get(key) != value for key, value in expected.items()):
        raise ValueError("Native server host metadata differs from its current source or target")
    if not path.is_file() or path.is_symlink() or not path.stat().st_mode & 0o111 or digest(path) != metadata.get("sha256"):
        raise ValueError("Native server host bytes or executable mode differ from its bound hash")
    architectures = subprocess.run(["xcrun", "lipo", "-archs", str(path)], capture_output=True, text=True, check=True).stdout.split()
    if sorted(architectures) != ARCHITECTURES:
        raise ValueError("Native server host must contain arm64 and x86_64")
    signature = subprocess.run(["codesign", "--verify", "--strict", str(path)], capture_output=True)
    if signature.returncode:
        raise ValueError("Native server host signature verification failed")


def verify_host(client: Path, metadata: dict) -> dict:
    """Validate a packaged host; no native library is loaded or executed."""
    verify_binary(client / HOST_PATH, metadata)
    if digest(client / LIBRARY_PATH) != LIBRARY_HASH:
        raise ValueError("The publisher native rule library was modified")
    return metadata


def compile_host(output: Path) -> dict:
    """Cache one signed binary for both temporary proofs and final packaging."""
    output = output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    metadata_path = output.with_name(output.name + ".json")
    with output.with_name(output.name + ".lock").open("a+") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX)
        if output.is_file() and metadata_path.is_file():
            try:
                metadata = json.loads(metadata_path.read_text())
                verify_binary(output, metadata)
                return metadata
            except (ValueError, OSError, subprocess.CalledProcessError):
                pass
        source_sha = digest(ROOT / SOURCE_PATH)
        with tempfile.TemporaryDirectory(prefix=".ygoserver-host-", dir=output.parent) as directory:
            work = Path(directory)
            for architecture in ARCHITECTURES:
                subprocess.run(["xcrun", "clang", "-std=c11", "-O2", "-Wall", "-Wextra", "-Werror",
                                "-target", architecture + "-apple-macos13.0", str(ROOT / SOURCE_PATH),
                                "-o", str(work / architecture)], check=True)
            binary = work / "ygoserver-host"
            subprocess.run(["xcrun", "lipo", "-create", *(str(work / architecture) for architecture in ARCHITECTURES),
                            "-output", str(binary)], check=True)
            subprocess.run(["codesign", "--force", "--sign", "-", "--identifier", IDENTIFIER,
                            "--timestamp=none", str(binary)], check=True)
            metadata = {"schema": 1, "path": HOST_PATH, "source_path": SOURCE_PATH, "source_sha256": source_sha,
                        "sha256": digest(binary), "library_path": LIBRARY_PATH, "library_sha256": LIBRARY_HASH,
                        "architectures": ARCHITECTURES, "minimum_macos": "13.0", "signature_verified": True}
            verify_binary(binary, metadata)
            bound = work / "ygoserver-host.json"
            bound.write_text(json.dumps(metadata, indent=2) + "\n")
            os.replace(binary, output)
            os.replace(bound, metadata_path)
        return metadata


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "build/ygoserver-host")
    args = parser.parse_args()
    metadata = compile_host(args.output)
    print(json.dumps({"ok": True, "host": str(args.output.resolve()), "source_sha256": metadata["source_sha256"],
                      "host_sha256": metadata["sha256"], "architectures": metadata["architectures"]}))


if __name__ == "__main__":
    main()
