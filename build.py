#!/usr/bin/env python3
"""Build the new client, or compile only its C# sources."""
import argparse
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parent

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--code-only", action="store_true", help="Compile without packaging the macOS app")
    parser.add_argument("--open", action="store_true", help="Open the successfully packaged app")
    args = parser.parse_args()
    if args.code_only and args.open:
        parser.error("--open requires a packaged app; omit --code-only")
    script = "build-mdpro3-source.py" if args.code_only else "build-mdpro3-offline.py"
    subprocess.run([sys.executable, str(ROOT / "scripts" / script)], cwd=ROOT, check=True)
    if args.open:
        subprocess.run(["open", str(ROOT / "dist/Yu-Gi-Oh! Fight.app")], check=True)

if __name__ == "__main__":
    main()
