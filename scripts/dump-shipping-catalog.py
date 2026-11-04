#!/usr/bin/env python3
"""Read the fixed publisher binary catalog with its actual managed reader.

Only a diagnostic copy of Addressables is modified. The resulting JSON is an
ID/dependency projection, not a runnable game catalog or a request-options dump.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess


ROOT = Path(__file__).resolve().parents[1]
CLIENT = ROOT / "vendor/MDPro3-mac"
CONTENTS = CLIENT / "MDPro3.app/Contents"
MANAGED = CONTENTS / "Resources/Data/Managed"
CATALOG = CONTENTS / "Resources/Data/StreamingAssets/aa/catalog.bin"
DOTNET = ROOT / ".tools/dotnet8/dotnet"
CSC = ROOT / ".tools/dotnet8/sdk/8.0.427/Roslyn/bincore/csc.dll"
BUILD = ROOT / "build"


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def run(arguments):
    environment = os.environ.copy()
    environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
    subprocess.run([str(value) for value in arguments], cwd=ROOT, env=environment, check=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "artifacts/shipping-addressables-catalog.json")
    args = parser.parse_args()
    output = args.output.resolve()
    if output.is_relative_to(CLIENT.resolve()):
        raise ValueError("The publisher tree is read only for diagnostics")
    if not DOTNET.is_file() or not CSC.is_file():
        raise RuntimeError("Verified local .NET 8.0.427 authoring toolchain is required; see tools/source-toolchain.json")
    checksums = {}
    for line in (ROOT / "vendor/MDPro3-mac-metadata/checksums.txt").read_text().splitlines():
        if line and not line.startswith("  "):
            digest, path = line.split("  ", 1)
            checksums[path] = digest
    sources = [CATALOG, *sorted(MANAGED.glob("*.dll")),
               CONTENTS / "Frameworks/libmonobdwgc-2.0.dylib", CONTENTS / "Frameworks/libmono-native.dylib"]
    verified = {}
    for source in sources:
        relative = source.relative_to(CLIENT).as_posix()
        digest = sha256(source)
        if checksums.get(relative) != digest:
            raise RuntimeError("Unverified publisher input: " + relative)
        verified[relative] = digest
    if verified[CATALOG.relative_to(CLIENT).as_posix()] != "5107a480cdc9e6370d4cd574e81ee105d1663e026651ba0280cca0fa61655c74":
        raise RuntimeError("Unsupported publisher catalog version")
    BUILD.mkdir(exist_ok=True)
    diagnostic = BUILD / "catalog-dump-managed"
    diagnostic.mkdir(exist_ok=True)
    for source in sorted(MANAGED.glob("*.dll")):
        shutil.copy2(source, diagnostic / source.name)
    run([DOTNET, "build", ROOT / "tools/CatalogReaderPrep/CatalogReaderPrep.csproj", "--configuration", "Release", "--nologo"])
    run([DOTNET, ROOT / "tools/CatalogReaderPrep/bin/Release/net8.0/CatalogReaderPrep.dll",
         MANAGED / "Unity.Addressables.dll", diagnostic / "Unity.Addressables.dll"])
    references = ["mscorlib.dll", "System.dll", "System.Core.dll", "Unity.Addressables.dll",
                  "Unity.ResourceManager.dll", "UnityEngine.CoreModule.dll", "Newtonsoft.Json.dll"]
    executable = BUILD / "CatalogDump.exe"
    response = BUILD / "catalog-dump-csc.rsp"
    response.write_text("\n".join(["/nostdlib", "/target:exe", '/out:"' + str(executable) + '"',
                                 *['/reference:"' + str(MANAGED / name) + '"' for name in references],
                                 '"' + str(ROOT / "tools/CatalogDump/CatalogDump.cs") + '"']) + "\n")
    run([DOTNET, CSC, "/noconfig", "@" + str(response)])
    runner = BUILD / "managed-tool-runner"
    run(["/usr/bin/clang", "-Wall", "-Wextra", "-O2", ROOT / "native/managed-tool-runner.c", "-o", runner])
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_suffix(output.suffix + ".tmp")
    run([runner, CONTENTS / "Frameworks", diagnostic, executable, CATALOG, temporary])
    graph = json.loads(temporary.read_text())
    if len(graph["keys"]) != 16671 or len(graph["locations"]) != 16683:
        raise RuntimeError("Unexpected catalog counts; diagnostic output was not accepted")
    temporary.replace(output)
    report = {"catalog": str(CATALOG.relative_to(ROOT)), "catalog_sha256": sha256(CATALOG),
              "output": str(output), "output_sha256": sha256(output),
              "keys": len(graph["keys"]), "locations": len(graph["locations"]),
              "publisher_inputs_verified": verified,
              "diagnostic_addressables_sha256": sha256(diagnostic / "Unity.Addressables.dll"),
              "projection": ["raw internal IDs", "provider IDs", "resource types", "primary keys", "dependency edges"],
              "excluded": ["runtime internal ID resolution", "ResourceManager scene initialization", "AssetBundleRequestOptions hash/CRC/cache data"],
              "vendor_files_modified": False, "game_runtime_modified": False}
    report_path = output.with_suffix(".provenance.json")
    report_path.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"output": str(output), "keys": report["keys"], "locations": report["locations"], "provenance": str(report_path)}))


if __name__ == "__main__":
    main()
