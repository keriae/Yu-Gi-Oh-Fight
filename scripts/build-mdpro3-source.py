#!/usr/bin/env python3
"""Compile the project C# sources against the pinned Unity runtime APIs."""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess
import zipfile
import importlib.util
import argparse

from source_build_state import build_transaction, game_source_paths, validate_generation, validate_source_inputs
from client_ui_assets import content_hashes as client_ui_hashes
from client_ui_resources import plan_client_resources
from client_ui_runtime_metadata import plan_runtime_metadata

ROOT = Path(__file__).resolve().parents[1]
DOTNET = ROOT / ".tools/dotnet8/dotnet"
MANAGED = ROOT / "vendor/MDPro3-mac/MDPro3.app/Contents/Resources/Data/Managed"
NET48_SHA = "8a7e348538e7eb91351696911689f49e3d4f63f8bab517432bbe159b8b1104a2"
DEFINES = ["MDPRO3_OFFLINE", "UNITY_STANDALONE_OSX", "UNITY_STANDALONE", "UNITY_64", "UNITY_6000_0",
           "UNITY_6000_0_OR_NEWER", "UNITY_5_3_OR_NEWER", "UNITY_5_4_OR_NEWER", "UNITY_2017_1_OR_NEWER",
           "UNITY_2018_1_OR_NEWER", "UNITY_2019_1_OR_NEWER", "UNITY_2020_1_OR_NEWER", "UNITY_2021_1_OR_NEWER",
           "UNITY_2022_1_OR_NEWER", "NET_STANDARD_2_0", "ENABLE_INPUT_SYSTEM", "ENABLE_LEGACY_INPUT_MANAGER"]


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def run(*args: str | Path) -> None:
    subprocess.run([str(arg) for arg in args], cwd=ROOT, check=True)


def preflight() -> None:
    required = [DOTNET, ROOT / ".tools/dotnet8/sdk/8.0.427/Roslyn/bincore/csc.dll",
                ROOT / ".tools/dotnet8/packs/NETStandard.Library.Ref/2.1.0/ref/netstandard2.1/netstandard.dll",
                ROOT / ".tools/mono-cecil-0.11.6/lib/netstandard2.0/Mono.Cecil.dll",
                ROOT / "vendor/MDPro3-mac-metadata/checksums.txt"]
    required += [ROOT / "vendor/MDPro3-release-source" / name for name in
                 ("manifest.json", "net48-reference-1.0.3.nupkg")]
    missing = [str(path.relative_to(ROOT)) for path in required if not path.is_file()]
    if not MANAGED.is_dir():
        missing.append(str(MANAGED.relative_to(ROOT)))
    if missing:
        raise ValueError("Missing source build inputs:\n  " + "\n  ".join(missing)
                         + "\nRestore tools with python3 scripts/setup-source-toolchain.py."
                         + "\nRestore publisher files with python3 scripts/acquire-mdpro3-resources.py.")


def compile_generation(output: Path) -> None:
    manifest_path = output / "manifest.json"
    preflight()
    _, ui_resources = plan_client_resources(ROOT)
    runtime_files, ui_runtime_metadata = plan_runtime_metadata(ROOT)
    ui_hashes = client_ui_hashes(ROOT / "content/client-ui")
    academy_spec = importlib.util.spec_from_file_location("source_academy_content", ROOT / "scripts/seed-deck-academy.py")
    academy = importlib.util.module_from_spec(academy_spec)
    academy_spec.loader.exec_module(academy)
    academy_hashes = academy.content_hashes(ROOT / "content/deck-academy")
    validator = academy.load_script("source_academy_validator", "validate-deck-academy.py")
    academy_validation = validator.validate_library(ROOT / "content/deck-academy", ROOT / "vendor/MDPro3-mac")
    if academy_validation.get("ok") is not True:
        raise ValueError("Deck academy validation failed: " + "; ".join(academy_validation.get("errors", [])))
    source_manifest = ROOT / "vendor/MDPro3-release-source/manifest.json"
    provenance = json.loads(source_manifest.read_text())
    publisher_path = ROOT / "vendor/MDPro3-mac-metadata/checksums.txt"
    publisher_sha = digest(publisher_path)
    if publisher_sha != "28361cbef68e3d825e2cd675421057976c61cda1870fcdffe64797d9c1782a3b":
        raise ValueError("Unsupported publisher manifest.")
    publisher = {name: value for value, name in (line.split("  ", 1) for line in publisher_path.read_text().splitlines())}
    prefix = MANAGED.relative_to(ROOT / "vendor/MDPro3-mac").as_posix() + "/"
    missing = [name for name in publisher if name.startswith(prefix) and name.endswith(".dll")
               and not (ROOT / "vendor/MDPro3-mac" / name).is_file()]
    if missing:
        raise ValueError("Missing publisher managed dependencies:\n  " + "\n  ".join(missing)
                         + "\nRun python3 scripts/acquire-mdpro3-resources.py.")
    dependencies = {}
    for path in sorted(MANAGED.glob("*.dll")):
        relative = path.relative_to(ROOT / "vendor/MDPro3-mac").as_posix()
        value = digest(path)
        if value != publisher.get(relative):
            raise ValueError("Publisher managed dependency changed: " + relative)
        dependencies[path.name] = value
    package = source_manifest.parent / "net48-reference-1.0.3.nupkg"
    if digest(package) != NET48_SHA:
        raise ValueError("Pinned Microsoft reference package identity mismatch.")
    net48 = ROOT / ".tools/net48-reference"
    net48.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(package) as archive:
        for name in archive.namelist():
            if name.startswith("build/.NETFramework/v4.8/") and name.endswith(".dll") and "/Facades/" not in name:
                (net48 / Path(name).name).write_bytes(archive.read(name))
    run(DOTNET, "build", ROOT / "tools/SourceReferences/SourceReferences.csproj", "-c", "Release", "--nologo")
    run(DOTNET, "build", ROOT / "tools/SourceValidation/SourceValidation.csproj", "-c", "Release", "--nologo")
    run(DOTNET, "build", ROOT / "tools/SourceRegistrations/SourceRegistrations.csproj", "-c", "Release", "--nologo")
    references = output / "references"
    netstandard = ROOT / ".tools/dotnet8/packs/NETStandard.Library.Ref/2.1.0/ref/netstandard2.1/netstandard.dll"
    run(DOTNET, ROOT / "tools/SourceReferences/bin/Release/net8.0/SourceReferences.dll", MANAGED, net48, references, netstandard)
    source_files = game_source_paths(ROOT)
    if not source_files or not (ROOT / "game/Generated/JobReflectionRegistration.g.cs").is_file():
        raise ValueError("Game sources and the Unity Job initializer are required.")
    inputs, hashes = [], {}
    for source in source_files:
        relative = source.relative_to(ROOT / "game").as_posix()
        before = source.read_bytes()
        target = output / "compiler-input" / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(before)
        inputs.append(target)
        hashes[relative] = hashlib.sha256(before).hexdigest()
    validate_source_inputs(ROOT, hashes)
    registrations = output / "compiler-input/Generated"
    run(DOTNET, ROOT / "tools/SourceRegistrations/bin/Release/net8.0/SourceRegistrations.dll",
        output / "compiler-input/Assets", registrations, ";".join(DEFINES))
    inputs.append(registrations / "MonoScriptRegistration.g.cs")
    registration_record = json.loads((registrations / "registration.json").read_text())
    response = ["-nostdlib+", "-target:library", "-unsafe+", "-langversion:9.0", "-nullable:disable",
                "-optimize+", "-deterministic+", "-debug:portable", '-out:"' + str(output / "Assembly-CSharp.dll") + '"',
                '-pathmap:"' + str(output) + '=/mdpro3/build/mdpro3-source,' + str(ROOT) + '=/mdpro3"', "-define:" + ";".join(DEFINES), "-warn:0"]
    response += ['-reference:"' + str(path) + '"' for path in sorted(references.glob("*.dll"))
                 if path.name != "Assembly-CSharp.dll"]
    response += ['"' + str(path) + '"' for path in inputs]
    tokenizer = ROOT / "content/ai/tokenizers/o200k_base.tiktoken"
    tokenizer_sha = digest(tokenizer)
    if tokenizer_sha != "446a9538cb6c348e3516120d7c08b09f57c36495e2acfffe59a5bf8b0cfb1a2d":
        raise ValueError("Prompt tokenizer rank table identity mismatch.")
    response.append('-resource:"' + str(tokenizer) + '",MDPro3.AI.o200k_base.tiktoken')
    rsp = output / "compile.rsp"
    rsp.write_text("\n".join(response) + "\n")
    run(DOTNET, ROOT / ".tools/dotnet8/sdk/8.0.427/Roslyn/bincore/csc.dll", "-noconfig", "@" + str(rsp))
    validation_path = output / "validation.json"
    runtime_initialize = output / "runtime-initialize.json"
    runtime_initialize.write_bytes(next(iter(runtime_files.values())))
    run(DOTNET, ROOT / "tools/SourceValidation/bin/Release/net8.0/SourceValidation.dll",
        MANAGED, output / "Assembly-CSharp.dll", ROOT / "content/client-ui/serialized-bindings.json",
        runtime_initialize, validation_path)
    validation = json.loads(validation_path.read_text())
    if digest(tokenizer) != tokenizer_sha:
        raise ValueError("Prompt tokenizer changed during compilation; rebuild first.")
    if academy.content_hashes(ROOT / "content/deck-academy") != academy_hashes:
        raise ValueError("Deck academy content changed during source compilation; rebuild first.")
    if client_ui_hashes(ROOT / "content/client-ui") != ui_hashes:
        raise ValueError("Client UI content changed during source compilation; rebuild first.")
    validate_source_inputs(ROOT, hashes)
    if plan_runtime_metadata(ROOT)[1] != ui_runtime_metadata:
        raise ValueError("Client runtime metadata changed during source compilation; rebuild first.")
    if plan_client_resources(ROOT)[1] != ui_resources:
        raise ValueError("Client serialized resources changed during source compilation; rebuild first.")
    manifest = {"ok": True, "source_compiled": True, "validation_verified": validation["ok"],
                "prompt_tokenizer_sha256": tokenizer_sha, "prompt_tokenizer_encoding": "o200k_base",
                "assembly_sha256": digest(output / "Assembly-CSharp.dll"), "pdb_sha256": digest(output / "Assembly-CSharp.pdb"),
                "publisher_manifest_sha256": publisher_sha, "source_commit": provenance["commit"],
                "publisher_version": provenance["tag"], "source_files": hashes, "source_file_count": len(hashes),
                "source_archive_manifest_sha256": digest(source_manifest), "shipping_dependencies": dependencies,
                "compiler_defines": DEFINES,
                "reference_supplements_sha256": digest(references / "reference-prep.json"),
                "validation_report_sha256": digest(validation_path), "validation_report": "validation.json",
                "deck_academy_content": academy_hashes, "client_ui_content": ui_hashes,
                "client_ui_resources": ui_resources,
                "unity_source_registration": {"source_files": registration_record["source_files"],
                    "source_types": registration_record["source_types"],
                    "generated_sha256": registration_record["generated_sha256"]},
                "client_ui_runtime_metadata": ui_runtime_metadata,
                "limits": "C# gameplay/UI/animation code is compiled from project sources. Unity layouts bind directly to project components; Unity runtime, card data and native rule libraries are reused."}
    manifest_path.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n")


def build_source() -> Path:
    return build_transaction(ROOT, compile_generation)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    snapshot = build_source()
    manifest = validate_generation(snapshot)
    print(json.dumps({"ok": True, "source_file_count": manifest["source_file_count"],
                      "assembly_sha256": manifest["assembly_sha256"], "generation": str(snapshot)}))


if __name__ == "__main__":
    main()
