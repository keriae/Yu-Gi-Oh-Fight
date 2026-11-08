"""Extract the approved Tika atlas into transparent, consistently sized sprites."""
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "docs/design/rarity/atlas.png"
TARGET = ROOT / "content/client-ui/rarity"
CENTERS = {
    "N": (349, 237), "R": (900, 237),
    "SR": (349, 505), "UR": (900, 505),
    "SE": (349, 774), "P": (900, 774),
    "special": (349, 1036), "unknown": (900, 1036),
}


def extract(source, center):
    x, y = center
    image = source.crop((x - 216, y - 108, x + 216, y + 108)).convert("RGBA")
    width, height = image.size
    pixels = image.load()
    exterior = set()
    queue = deque([(x, 0) for x in range(width)] + [(x, height - 1) for x in range(width)]
                  + [(0, y) for y in range(height)] + [(width - 1, y) for y in range(height)])
    # Remove only black connected to the outside. Keep the dark enamel interior.
    while queue:
        x, y = queue.popleft()
        if not (0 <= x < width and 0 <= y < height) or (x, y) in exterior:
            continue
        if max(pixels[x, y][:3]) > 60:
            continue
        exterior.add((x, y))
        queue.extend(((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)))
    for x, y in exterior:
        red, green, blue, _ = pixels[x, y]
        alpha = max(red, green, blue) / 60
        pixels[x, y] = ((int(red / alpha), int(green / alpha), int(blue / alpha), round(alpha * 255))
                        if alpha else (0, 0, 0, 0))
    return image.resize((192, 96), Image.Resampling.LANCZOS)


def main():
    TARGET.mkdir(parents=True, exist_ok=True)
    atlas = Image.open(SOURCE)
    sprites = {label: extract(atlas, center) for label, center in CENTERS.items()}
    # Reuse the generated gold plates and star-lit bezels for less common codes.
    # The client supplies their exact lettering rather than changing rarity names.
    interior = Image.new("L", (192, 96), 0)
    ImageDraw.Draw(interior).polygon(((36, 20), (173, 20), (173, 64), (156, 80), (20, 80), (20, 37)), fill=255)
    sprites["special-neutral"] = ImageEnhance.Color(sprites["special"]).enhance(0)
    sprites["special-shine"] = Image.composite(sprites["special"], sprites["UR"], interior)
    sprites["special-brilliant"] = ImageEnhance.Brightness(
        Image.composite(sprites["special"], sprites["SE"], interior)).enhance(1.12)
    preview = Image.new("RGB", (640, ((len(sprites) + 1) // 2) * 100), "#14202b")
    draw = ImageDraw.Draw(preview)
    for index, (label, sprite) in enumerate(sprites.items()):
        sprite.save(TARGET / (label + ".png"), optimize=True)
        x, y = (index % 2) * 320, (index // 2) * 100
        draw.text((x + 12, y + 12), label, fill="white")
        preview.paste(sprite, (x + 72, y + 2), sprite)
        tiny = sprite.resize((24, 12), Image.Resampling.LANCZOS)
        preview.paste(tiny, (x + 284, y + 44), tiny)
    preview_path = ROOT / "build/rarity-art/sprites-preview.png"
    preview_path.parent.mkdir(parents=True, exist_ok=True)
    preview.save(preview_path)


if __name__ == "__main__":
    main()
