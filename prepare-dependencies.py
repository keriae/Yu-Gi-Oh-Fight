#!/usr/bin/env python3
"""Restore fixed build dependencies, optionally cloning an existing masterduel checkout."""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parent

def clone_missing(source: Path, destination: Path):
    if destination.exists():
        return
    if not source.exists():
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_name("." + destination.name + ".preparing")
    if temporary.exists():
        raise ValueError("Dependency preparation is already present: " + str(temporary))
    result = subprocess.run(["cp", "-cR", str(source), str(temporary)], capture_output=True)
    if result.returncode:
        if temporary.is_dir():
            shutil.rmtree(temporary)
        elif temporary.exists():
            temporary.unlink()
        if source.is_dir():
            shutil.copytree(source, temporary)
        else:
            shutil.copy2(source, temporary)
    temporary.rename(destination)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--from", dest="source", type=Path, help="Read-only source checkout, e.g. ../masterduel")
    parser.add_argument("--check", action="store_true", help="Check toolchain and complete publisher resources without downloading")
    args = parser.parse_args()
    if args.source and not args.check:
        source = args.source.resolve(strict=True)
        if source == ROOT:
            parser.error("--from must point to another checkout")
        for relative in ["vendor/MDPro3-mac", "vendor/MDPro3-mac.manifest.json", "vendor/MDPro3-mac.metalink.xml",
                         "vendor/Mono-LICENSE.txt", "vendor/YgoMaster-LICENSE.txt", ".tools/dotnet8", ".tools/mono-cecil-0.11.6",
                         "build/dotnet8-sdk.tar.gz", "build/mono.cecil.0.11.6.nupkg"]:
            clone_missing(source / relative, ROOT / relative)
    (ROOT / "artifacts").mkdir(exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / "scripts/setup-source-toolchain.py"), *( ["--check"] if args.check else [] )], cwd=ROOT, check=True)
    runtime = ROOT / "vendor/MDPro3-mac"
    if not runtime.exists():
        if args.check:
            raise SystemExit("Missing vendor/MDPro3-mac; run prepare-dependencies.py --from ../masterduel")
        subprocess.run([sys.executable, str(ROOT / "scripts/acquire-mdpro3-resources.py")], cwd=ROOT, check=True)
    subprocess.run([sys.executable, str(ROOT / "scripts/verify-native-client.py"), "--output", str(ROOT / "artifacts/dependency-resources.json")], cwd=ROOT, check=True)

if __name__ == "__main__":
    main()
