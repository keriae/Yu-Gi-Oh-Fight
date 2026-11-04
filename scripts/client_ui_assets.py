"""Exact source inventory shared by compilation and UI asset packaging."""
import hashlib
from pathlib import Path

def content_hashes(source: Path) -> dict[str, str]:
    if not source.is_dir():
        raise ValueError("Missing new client UI assets: " + str(source))
    result = {}
    for path in sorted(source.rglob("*")):
        if path.is_symlink():
            raise ValueError("Client UI assets must not contain symbolic links.")
        if path.is_file() and path.name != ".DS_Store":
            result[path.relative_to(source).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    if not result:
        raise ValueError("The new client UI asset directory is empty.")
    return result
