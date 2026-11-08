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


def game_source_paths(root: Path) -> list[Path]:
    """Collect runtime C# sources, excluding Unity Editor code."""
    game = root / "game"
    return sorted(path for directory in (game / "Assets/Scripts", game / "Assets/Tools", game / "Generated")
                  for path in directory.rglob("*.cs") if "Editor" not in path.relative_to(game).parts)


def validate_source_inputs(root: Path, expected: dict) -> None:
    """Reject edits, additions and removals after compiler inputs were copied."""
    try:
        actual = {path.relative_to(root / "game").as_posix(): digest(path)
                  for path in game_source_paths(root)}
    except OSError as error:
        raise ValueError("C# sources changed or became unreadable during the build; rerun the build.") from error
    if not isinstance(expected, dict) or actual != expected:
        raise ValueError("C# sources changed during the build; rerun the build.")


def validate_generation(generation: Path) -> dict:
    manifest = json.loads((generation / "manifest.json").read_text())
    if not all(manifest.get(key) for key in ("ok", "source_compiled", "validation_verified")):
        raise ValueError("A successful compiled-source validation manifest is required.")
    for extension in ("dll", "pdb"):
        key = "assembly_sha256" if extension == "dll" else "pdb_sha256"
        if digest(generation / ("Assembly-CSharp." + extension)) != manifest[key]:
            raise ValueError("Source generation digest mismatch: " + extension)
    if manifest.get("validation_report") != "validation.json":
        raise ValueError("The source generation must contain its validation report.")
    report_path = generation / "validation.json"
    if digest(report_path) != manifest["validation_report_sha256"]:
        raise ValueError("Source generation validation report digest mismatch.")
    report = json.loads(report_path.read_text())
    if report.get("ok") is not True or report.get("compiled_sha256") != manifest["assembly_sha256"]:
        raise ValueError("Source generation validation does not match its assembly.")
    registration = manifest.get("unity_source_registration")
    if not isinstance(registration, dict) or digest(generation / "compiler-input/Generated/MonoScriptRegistration.g.cs") != registration.get("generated_sha256"):
        raise ValueError("Source generation Unity registration digest mismatch.")
    if not isinstance(manifest.get("client_ui_resources"), dict):
        raise ValueError("Source generation must bind its serialized resources.")
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
        raise OSError("Atomic publication requires macOS renameatx_np or Linux renameat2.")
    rename.argtypes = [ctypes.c_int, ctypes.c_char_p, ctypes.c_int, ctypes.c_char_p, ctypes.c_uint]
    rename.restype = ctypes.c_int
    if rename(cwd, os.fsencode(first), cwd, os.fsencode(second), 2):
        error = ctypes.get_errno()
        raise OSError(error, os.strerror(error))


def _publish(root: Path, generation: Path) -> None:
    manifest = json.loads((generation / "manifest.json").read_text())
    validate_source_inputs(root, manifest.get("source_files"))
    build = root / "build"
    output = build / "mdpro3-source"
    temporary = build / (".mdpro3-source-pointer-" + uuid.uuid4().hex)
    os.symlink(os.path.relpath(generation, build), temporary)
    try:
        if output.exists() and not output.is_symlink():
            raise ValueError("Source build output must be a generation pointer.")
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
            validate_source_inputs(root, manifest.get("source_files"))
            for name in ("Assembly-CSharp.dll", "Assembly-CSharp.pdb", "manifest.json", "validation.json"):
                path = staged / name
                if path.is_file():
                    with path.open("rb") as stream:
                        os.fsync(stream.fileno())
            _sync_directory(staged)
            validate_source_inputs(root, manifest.get("source_files"))
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
