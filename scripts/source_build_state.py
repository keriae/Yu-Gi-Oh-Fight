"""Serialize source builds and publish complete generations through one pointer."""
from __future__ import annotations

from contextlib import contextmanager
import ctypes
import fcntl
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile
import uuid


@contextmanager
def source_build_lock(root: Path):
    build = root / "build"
    build.mkdir(parents=True, exist_ok=True)
    with (build / ".mdpro3-source.lock").open("a+") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            print("Waiting for the current MDPro3 source build to finish.", file=sys.stderr)
            fcntl.flock(lock, fcntl.LOCK_EX)
        try:
            yield
        finally:
            fcntl.flock(lock, fcntl.LOCK_UN)


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def game_source_paths(root: Path, validation: bool = False) -> list[Path]:
    """Keep local acceptance code out of production assemblies and inventories."""
    game = root / "game"
    sources = []
    for path in game.rglob("*.cs"):
        relative = path.relative_to(game)
        if "Editor" in relative.parts:
            continue
        local_validation = ("UI/Client/Validation/" in relative.as_posix()
                            or (path.name.endswith("Proof.cs") and path.parent.name in {"AI", "Utility"}))
        if validation or not local_validation:
            sources.append(path)
    return sorted(sources)


def validate_source_inputs(root: Path, expected: dict, validation: bool = False) -> None:
    """Reject edits, additions and removals after compiler inputs were copied."""
    try:
        actual = {path.relative_to(root / "game").as_posix(): digest(path)
                  for path in game_source_paths(root, validation)}
    except OSError as error:
        raise ValueError("C# sources changed or became unreadable during the build; rerun the build.") from error
    if not isinstance(expected, dict) or actual != expected:
        raise ValueError("C# sources changed during the build; rerun the build.")


def validate_generation(generation: Path) -> dict:
    manifest = json.loads((generation / "manifest.json").read_text())
    if not all(manifest.get(key) for key in ("ok", "source_compiled", "compatibility_verified")):
        raise ValueError("A successful compiled-source compatibility manifest is required.")
    for extension in ("dll", "pdb"):
        key = "assembly_sha256" if extension == "dll" else "pdb_sha256"
        if digest(generation / ("Assembly-CSharp." + extension)) != manifest[key]:
            raise ValueError("Source generation digest mismatch: " + extension)
    if manifest.get("compatibility_report"):
        if manifest["compatibility_report"] != "compatibility.json":
            raise ValueError("Unsupported generation compatibility report path.")
        if digest(generation / "compatibility.json") != manifest["compatibility_report_sha256"]:
            raise ValueError("Source generation compatibility report digest mismatch.")
    return manifest


def _sync_directory(path: Path) -> None:
    descriptor = os.open(path, os.O_RDONLY)
    try:
        os.fsync(descriptor)
    finally:
        os.close(descriptor)


def atomic_exchange(first: Path, second: Path) -> None:
    """Swap existing paths on one filesystem without exposing a missing path."""
    library = ctypes.CDLL(None, use_errno=True)
    if sys.platform == "darwin":
        rename = library.renameatx_np
        cwd = -2  # Darwin AT_FDCWD
    elif sys.platform.startswith("linux") and hasattr(library, "renameat2"):
        rename = library.renameat2
        cwd = -100  # Linux AT_FDCWD
    else:
        raise OSError("Atomic migration requires macOS renameatx_np or Linux renameat2.")
    rename.argtypes = [ctypes.c_int, ctypes.c_char_p, ctypes.c_int, ctypes.c_char_p, ctypes.c_uint]
    rename.restype = ctypes.c_int
    if rename(cwd, os.fsencode(first), cwd, os.fsencode(second), 2):
        error = ctypes.get_errno()
        raise OSError(error, os.strerror(error))


def _publish(root: Path, generation: Path) -> None:
    manifest = json.loads((generation / "manifest.json").read_text())
    validate_source_inputs(root, manifest.get("source_files"), manifest.get("build_profile") == "ui-proof")
    build = root / "build"
    output = build / "mdpro3-source"
    temporary = build / (".mdpro3-source-pointer-" + uuid.uuid4().hex)
    os.symlink(os.path.relpath(generation, build), temporary)
    try:
        if output.exists() and not output.is_symlink():
            if not output.is_dir():
                raise ValueError("Refusing to replace an unexpected source output file.")
            atomic_exchange(temporary, output)
            # Publication already succeeded. Keep the old directory recoverable
            # even if its optional relocation cannot complete.
            try:
                os.rename(temporary, generation.parent / ("legacy-" + uuid.uuid4().hex))
            except OSError:
                pass
        else:
            os.replace(temporary, output)
        _sync_directory(build)
    finally:
        if temporary.is_symlink():
            temporary.unlink()


def build_transaction(root: Path, compile_generation) -> Path:
    """Run the compiler under the shared lock; failures retain the last build."""
    with source_build_lock(root):
        generations = root / "build/.mdpro3-source-generations"
        generations.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(prefix=".pending-", dir=generations) as work:
            staged = Path(work)
            compile_generation(staged)
            manifest = validate_generation(staged)
            validate_source_inputs(root, manifest.get("source_files"), manifest.get("build_profile") == "ui-proof")
            for name in ("Assembly-CSharp.dll", "Assembly-CSharp.pdb", "manifest.json", "compatibility.json"):
                path = staged / name
                if path.is_file():
                    with path.open("rb") as stream:
                        os.fsync(stream.fileno())
            _sync_directory(staged)
            validate_source_inputs(root, manifest.get("source_files"), manifest.get("build_profile") == "ui-proof")
            final = generations / ("generation-" + uuid.uuid4().hex)
            os.rename(staged, final)
            _sync_directory(generations)
            try:
                _publish(root, final)
            except BaseException:
                output = root / "build/mdpro3-source"
                # A failed input check never published this owned generation.
                # Preserve it if a later error occurred after the atomic swap.
                if not output.is_symlink() or output.resolve() != final:
                    shutil.rmtree(final)
                raise
            return final.resolve(strict=True)


def source_snapshot(root: Path) -> Path:
    """Resolve once; retained generations are never overwritten by a build."""
    with source_build_lock(root):
        output = root / "build/mdpro3-source"
        if not output.is_symlink():
            raise ValueError("Compile the source output before requesting a retained snapshot.")
        snapshot = output.resolve(strict=True)
        validate_generation(snapshot)
        return snapshot
