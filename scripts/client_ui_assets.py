"""Exact source inventory shared by compilation and UI asset packaging."""
import hashlib
import json
import math
import re
import subprocess
import sys
from pathlib import Path


def validate_scene_catalog(source: Path) -> None:
    """Keep a broken scene or preview out of the shipped content inventory."""
    catalog = source / "duel-scenes/catalog.json"
    if not catalog.exists():
        return
    data = json.loads(catalog.read_text())
    if not isinstance(data, dict) or type(data.get("schema")) is not int or data["schema"] != 2:
        raise ValueError("Unsupported duel scene catalog schema.")
    scenes = data.get("scenes")
    if not isinstance(scenes, list) or not scenes:
        raise ValueError("The duel scene catalog is empty.")
    identifiers = set()
    for scene in scenes:
        if not isinstance(scene, dict):
            raise ValueError("Invalid duel scene definition.")
        identifier, builder = scene.get("id"), scene.get("builder")
        if any(not isinstance(value, str) or not re.fullmatch(r"[a-z][a-z0-9-]{0,63}", value)
               for value in (identifier, builder)) or identifier in identifiers:
            raise ValueError("Invalid or duplicate duel scene identifier.")
        identifiers.add(identifier)
        if identifier == "classic" and builder != "classic":
            raise ValueError("The default duel scene must use the classic builder.")
        if builder not in {"classic", "battle-academy", "ancient-egypt", "skyscraper"}:
            raise ValueError("Unsupported duel scene builder: " + builder)
        if any(not isinstance(scene.get(key), str) or not scene[key].strip() for key in ("name", "description", "style")):
            raise ValueError("Missing duel scene name, style or description.")
        if not isinstance(scene.get("accent"), str) or not re.fullmatch(r"#[0-9a-fA-F]{6}", scene["accent"]):
            raise ValueError("Invalid duel scene accent colour.")
        image_keys = ["preview_path", "overview_path"] + (["texture_path"] if builder != "classic" else [])
        for key in image_keys:
            relative = scene.get(key)
            if (not isinstance(relative, str) or not relative.startswith("duel-scenes/")
                    or "\\" in relative or ":" in relative
                    or any(part in {"", ".", ".."} for part in relative.split("/"))):
                raise ValueError("Invalid duel scene asset path.")
            path = source / relative
            if not path.is_file() or path.is_symlink():
                raise ValueError("Missing duel scene asset: " + relative)
            if not _valid_scene_image(path):
                raise ValueError("Invalid duel scene image: " + relative)
        if builder != "classic":
            for part in ("controls-fine", "court-v3", "field-materials-v4", "opening-parts-v2", "opening-order-v3"):
                relative = "duel-scenes/" + builder + "-" + part + ".png"
                path = source / relative
                if not path.is_file() or path.is_symlink() or not _valid_scene_image(path):
                    raise ValueError("Missing or invalid integrated scene asset: " + relative)
            _validate_fixture_layout(source / "duel-scenes" / (builder + "-fixture-layout-v3.json"), builder)
    if "classic" not in identifiers:
        raise ValueError("The duel scene catalog must retain its default scene.")
    if any(scene["builder"] != "classic" for scene in scenes):
        for filename in ("action-glyphs-v2.png", "hud-glyphs-v2.png"):
            relative = "duel-scenes/" + filename
            path = source / relative
            if not path.is_file() or path.is_symlink() or not _valid_scene_image(path):
                raise ValueError("Missing or invalid native glyph atlas: " + relative)


def _validate_fixture_layout(path: Path, builder: str) -> None:
    if not path.is_file() or path.is_symlink():
        raise ValueError("Missing fixture layout: " + builder)
    data = json.loads(path.read_text())
    width, height = data.get("width"), data.get("height")
    if type(width) is not int or type(height) is not int or width < 1000 or height < 600:
        raise ValueError("Invalid fixture image dimensions: " + builder)
    fixtures = data.get("fixtures")
    expected = {"far-grave", "far-banish", "timer", "phase", "near-banish", "near-grave"}
    if not isinstance(fixtures, list) or len(fixtures) != 6 or {f.get("id") for f in fixtures if isinstance(f, dict)} != expected:
        raise ValueError("Invalid fixture identities: " + builder)
    for fixture in fixtures:
        quad = fixture.get("hit")
        if not isinstance(quad, list) or len(quad) != 8 or any(type(v) not in (float, int) or not math.isfinite(v) for v in quad):
            raise ValueError("Invalid fixture quad: " + builder + "/" + fixture["id"] + "/hit")
        if any(not 0 <= v <= (width if i % 2 == 0 else height) for i, v in enumerate(quad)):
            raise ValueError("Fixture quad outside image: " + builder)
        area = sum(quad[i * 2] * quad[((i + 1) % 4) * 2 + 1]
                   - quad[((i + 1) % 4) * 2] * quad[i * 2 + 1] for i in range(4))
        if abs(area) < 2:
            raise ValueError("Empty fixture quad: " + builder)


def _valid_scene_image(path: Path) -> bool:
    expected = {".png": "PNG", ".jpg": "JPEG", ".jpeg": "JPEG"}.get(path.suffix.lower())
    if expected is None:
        return False
    # Use the existing pinned Pillow environment, also used by the resource
    # importer. Headers alone do not prove that Unity can decode the image.
    root = Path(__file__).resolve().parents[1]
    python = root / ".tools/client-ui-resources" / ("Scripts/python.exe" if sys.platform == "win32" else "bin/python")
    if not python.is_file():
        raise ValueError("Restore image validation dependencies with python3 scripts/setup-client-ui-resources.py.")
    decoder = """
import sys,json
from PIL import Image
try:
    with Image.open(sys.argv[1]) as image:
        if image.format != sys.argv[2] or not all(16 <= size <= 8192 for size in image.size):
            sys.exit(1)
        image.verify()
    with Image.open(sys.argv[1]) as image:
        image.load()
        if sys.argv[1].endswith("-opening-parts-v2.png"):
            if image.width < 900 or image.height < 600 or "A" not in image.getbands():
                sys.exit(1)
            alpha = image.getchannel("A")
            # A real six-part cutout atlas must have transparent margins in
            # every cell, plus an actual opaque component, not a green/black
            # rectangular sheet or a missing layer.
            for row in range(2):
                for column in range(3):
                    box = (image.width * column // 3, image.height * row // 2,
                           image.width * (column + 1) // 3, image.height * (row + 1) // 2)
                    cell = alpha.crop(box)
                    hist = cell.histogram()
                    count = cell.width * cell.height
                    if sum(hist[:9]) < count * .12 or sum(hist[240:]) < count * .025:
                        sys.exit(1)
        if sys.argv[1].endswith("-opening-order-v3.png"):
            if image.width < 1000 or image.height < 300 or "A" not in image.getbands():
                sys.exit(1)
            alpha = image.getchannel("A")
            for side in range(2):
                cell = alpha.crop((image.width * side // 2, 0, image.width * (side + 1) // 2, image.height))
                hist = cell.histogram()
                count = cell.width * cell.height
                if sum(hist[:9]) < count * .10 or sum(hist[240:]) < count * .10:
                    sys.exit(1)
        if sys.argv[1].endswith("-field-materials-v4.png") and (image.width != image.height or image.width < 1024):
            sys.exit(1)
        if sys.argv[1].endswith("-court-v3.png"):
            layout=json.load(open(sys.argv[1].replace("-court-v3.png", "-fixture-layout-v3.json")))
            if image.size != (layout.get("width"),layout.get("height")):
                sys.exit(1)
        if sys.argv[1].endswith("/hud-glyphs-v2.png") or sys.argv[1].endswith("\\\\hud-glyphs-v2.png"):
            if image.size != (320, 240) or image.mode != "RGBA":
                sys.exit(1)
        if sys.argv[1].endswith("/action-glyphs-v2.png") or sys.argv[1].endswith("\\\\action-glyphs-v2.png"):
            if image.size != (384, 384) or image.mode != "RGBA":
                sys.exit(1)
            alpha = image.getchannel("A")
            for index in range(16):
                column, row = index % 4, index // 4
                cell = alpha.crop((column * 96, row * 96, (column + 1) * 96, (row + 1) * 96))
                histogram = cell.histogram()
                if index < 13:
                    if sum(histogram[9:]) < 200 or histogram[0] < 96 * 96 * .60:
                        sys.exit(1)
                elif histogram[0] != 96 * 96:
                    sys.exit(1)
except (OSError, ValueError, SyntaxError):
    sys.exit(1)
"""
    return subprocess.run([str(python), "-c", decoder, str(path), expected], capture_output=True).returncode == 0


def content_hashes(source: Path) -> dict[str, str]:
    if not source.is_dir():
        raise ValueError("Missing new client UI assets: " + str(source))
    result = {}
    for path in sorted(source.rglob("*")):
        if path.is_symlink():
            raise ValueError("Client UI assets must not contain symbolic links.")
        if path.is_file() and path.name not in {".DS_Store", "serialized-bindings.json"}:
            result[path.relative_to(source).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    if not result:
        raise ValueError("The new client UI asset directory is empty.")
    validate_scene_catalog(source)
    return result
