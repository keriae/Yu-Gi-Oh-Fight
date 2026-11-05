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
    parser.add_argument("--ui-proof", action="store_true", help="Include native UI acceptance code in a code-only validation build")
    parser.add_argument("--open", action="store_true", help="Open the successfully packaged app")
    args = parser.parse_args()
    if args.code_only and args.open:
        parser.error("--open requires a packaged app; omit --code-only")
    if args.ui_proof and not args.code_only:
        parser.error("--ui-proof is only available with --code-only; packaged apps use the release profile")
    script = "build-mdpro3-source.py" if args.code_only else "build-mdpro3-offline.py"
    command = [sys.executable, str(ROOT / "scripts" / script)]
    if args.ui_proof:
        command.append("--ui-proof")
    subprocess.run(command, cwd=ROOT, check=True)
    if args.open:
        subprocess.run(["open", str(ROOT / "dist/Yu-Gi-Oh! Fight.app")], check=True)

if __name__ == "__main__":
    main()
