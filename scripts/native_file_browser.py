#!/usr/bin/env python3
"""Build and verify the universal macOS file selector used by the SFB wrapper."""
from __future__ import annotations

import argparse
import fcntl
import hashlib
import json
import os
from pathlib import Path
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = "native/StandaloneFileBrowser"
LIBRARY_PATH = "MDPro3.app/Contents/PlugIns/libStandaloneFileBrowser.dylib"
MANAGED_LINK = "MDPro3.app/Contents/Resources/Data/Managed/libStandaloneFileBrowser.dylib"
LINK_TARGET = "../../../PlugIns/libStandaloneFileBrowser.dylib"
ARCHITECTURES = ["arm64", "x86_64"]
EXPORTS = ["DialogOpenFilePanel", "DialogOpenFilePanelAsync", "DialogOpenFolderPanel",
           "DialogOpenFolderPanelAsync", "DialogSaveFilePanel", "DialogSaveFilePanelAsync"]


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_record() -> dict:
    return {"schema": 1, "path": LIBRARY_PATH, "managed_link": MANAGED_LINK,
            "managed_link_target": LINK_TARGET, "architectures": ARCHITECTURES,
            "minimum_macos": "13.0", "signature_verified": True,
            "source_files": {name: digest(ROOT / SOURCE / name)
                             for name in ("Plugin.mm", "Plugin.pch", "LICENSE.txt", "provenance.json")},
            "builder_sha256": digest(Path(__file__).resolve())}


def verify_binary(path: Path, metadata: dict) -> None:
    if any(metadata.get(key) != value for key, value in source_record().items()):
        raise ValueError("Native file selector metadata differs from its source or target")
    if not path.is_file() or path.is_symlink() or digest(path) != metadata.get("sha256"):
        raise ValueError("Native file selector bytes differ from their bound hash")
    architectures = subprocess.run(["xcrun", "lipo", "-archs", str(path)],
                                   capture_output=True, text=True, check=True).stdout.split()
    if sorted(architectures) != ARCHITECTURES:
        raise ValueError("Native file selector must contain arm64 and x86_64")
    for architecture in ARCHITECTURES:
        symbols = subprocess.run(["xcrun", "nm", "-arch", architecture, "-gU", str(path)],
                                 capture_output=True, text=True, check=True).stdout
        if not all(" _" + name + "\n" in symbols for name in EXPORTS):
            raise ValueError("Native file selector is missing its SFB entry points")
    subprocess.run(["codesign", "--verify", "--strict", str(path)], check=True,
                   stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)


def verify_file_browser(client: Path, metadata: dict) -> dict:
    verify_binary(client / LIBRARY_PATH, metadata)
    link = client / MANAGED_LINK
    if not link.is_symlink() or os.readlink(link) != LINK_TARGET \
            or link.resolve(strict=True) != (client / LIBRARY_PATH).resolve(strict=True):
        raise ValueError("Managed Mono file selector lookup link is missing or changed")
    return metadata


def compile_file_browser(output: Path) -> dict:
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
        record = source_record()
        with tempfile.TemporaryDirectory(prefix=".file-selector-", dir=output.parent) as directory:
            work = Path(directory)
            for architecture in ARCHITECTURES:
                subprocess.run(["xcrun", "clang++", "-dynamiclib", "-O2", "-std=c++11", "-fobjc-arc",
                                "-Wno-deprecated-declarations", "-target", architecture + "-apple-macos13.0",
                                "-framework", "AppKit", "-framework", "Foundation",
                                "-Wl,-install_name,@rpath/libStandaloneFileBrowser.dylib",
                                str(ROOT / SOURCE / "Plugin.mm"), "-o", str(work / architecture)], check=True)
            binary = work / "libStandaloneFileBrowser.dylib"
            subprocess.run(["xcrun", "lipo", "-create", *(str(work / architecture) for architecture in ARCHITECTURES),
                            "-output", str(binary)], check=True)
            subprocess.run(["codesign", "--force", "--sign", "-", "--timestamp=none", "--identifier",
                            "io.github.keriae.yugiohfight.file-selector", str(binary)], check=True)
            if source_record() != record:
                raise ValueError("Native file selector sources changed during compilation")
            metadata = {**record, "sha256": digest(binary)}
            verify_binary(binary, metadata)
            bound = work / "metadata.json"
            bound.write_text(json.dumps(metadata, indent=2) + "\n")
            os.replace(binary, output)
            os.replace(bound, metadata_path)
        return metadata


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "build/libStandaloneFileBrowser.dylib")
    args = parser.parse_args()
    metadata = compile_file_browser(args.output)
    print(json.dumps({"ok": True, "library": str(args.output.resolve()), "sha256": metadata["sha256"],
                      "architectures": metadata["architectures"]}))


if __name__ == "__main__":
    main()
