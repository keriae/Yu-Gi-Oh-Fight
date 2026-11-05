#!/usr/bin/env python3
"""Restore fixed build dependencies, optionally cloning an existing masterduel checkout."""
import argparse
import fcntl
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
    # Copy a donor dependency itself, rather than installing a link back into
    # that checkout where a later resource repair could write original files.
    source = source.resolve(strict=True)
    destination.parent.mkdir(parents=True, exist_ok=True)
    temporary = destination.with_name("." + destination.name + ".preparing")
    # Keep one lock inode across retries. cp inherits it so an orphaned copy
    # still protects its staging directory after the parent process exits.
    with temporary.with_suffix(temporary.suffix + ".lock").open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError("Dependency preparation is already running: " + str(destination))
        if destination.exists():
            return

        def discard_partial():
            if temporary.is_symlink() or temporary.is_file():
                temporary.unlink()
            elif temporary.exists():
                shutil.rmtree(temporary)

        discard_partial()
        result = subprocess.run(["cp", "-cR", str(source), str(temporary)],
                                capture_output=True, pass_fds=(lock.fileno(),))
        if result.returncode:
            discard_partial()
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
    if sys.version_info < (3, 12):
        parser.error("Dependency preparation requires Python 3.12 or later for safe publisher archive extraction")
    if args.source and not args.check:
        source = args.source.resolve(strict=True)
        if source == ROOT:
            parser.error("--from must point to another checkout")
        for relative in ["vendor/MDPro3-mac", "vendor/MDPro3-mac.manifest.json", "vendor/MDPro3-mac.metalink.xml",
                         ".tools/dotnet8", ".tools/mono-cecil-0.11.6",
                         "build/dotnet8-sdk.tar.gz", "build/mono.cecil.0.11.6.nupkg"]:
            clone_missing(source / relative, ROOT / relative)
    (ROOT / "artifacts").mkdir(exist_ok=True)
    subprocess.run([sys.executable, str(ROOT / "scripts/setup-source-toolchain.py"), *( ["--check"] if args.check else [] )], cwd=ROOT, check=True)
    runtime = ROOT / "vendor/MDPro3-mac"
    verification = [sys.executable, str(ROOT / "scripts/verify-native-client.py"),
                    "--output", str(ROOT / "artifacts/dependency-resources.json")]
    if args.check and not runtime.is_dir():
        raise SystemExit("Missing vendor/MDPro3-mac; run python3 prepare-dependencies.py")
    if runtime.is_dir():
        result = subprocess.run(verification, cwd=ROOT, check=args.check)
        if result.returncode == 0:
            return
    # A partial download or imported checkout already has a runtime directory.
    # Verify its contents before deciding whether the resumable repair is needed.
    subprocess.run([sys.executable, str(ROOT / "scripts/acquire-mdpro3-resources.py")], cwd=ROOT, check=True)
    subprocess.run(verification, cwd=ROOT, check=True)

if __name__ == "__main__":
    main()
