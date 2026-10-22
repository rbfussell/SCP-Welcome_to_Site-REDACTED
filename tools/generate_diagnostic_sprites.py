"""Generate labeled development sprite samples; no game/runtime integration."""

from hashlib import sha256
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, __version__ as pillow_version


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "assets/development/diagnostic-sprites"
SIZE = 192
SUPERSAMPLE = 4
FACING_COLOR = "#61FF70"
FONT = Path("/usr/share/fonts/open-sans/OpenSans-Regular.ttf")
BOLD_FONT = Path("/usr/share/fonts/open-sans/OpenSans-Bold.ttf")
PALETTE = {
    "landscape": ("#6B5D3E", "LANDSCAPE", "Ground"),
    "map_object": ("#61717C", "MAP OBJECT", "Oak tree"),
    "building": ("#9B7BA6", "BUILDING", "Wall tile"),
    "object": ("#C88E41", "OBJECT", "Supply box"),
    "npc": ("#3D8FA3", "NPC", "Mara Chen"),
    "player": ("#C74D79", "PLAYER", "Player"),
}
BAR_RECTS = {
    "north": (18, 14, 174, 22),
    "east": (168, 18, 176, 174),
    "south": (18, 168, 174, 176),
    "west": (14, 18, 22, 174),
}


def sprite(category, state, facing, frame=1, total=1):
    color, label, name = PALETTE[category]
    canvas = Image.new("RGBA", (SIZE * SUPERSAMPLE,) * 2)
    draw = ImageDraw.Draw(canvas)

    def rect(bounds, fill, radius=0, outline=None):
        bounds = tuple(value * SUPERSAMPLE for value in bounds)
        draw.rounded_rectangle(
            bounds, radius=radius * SUPERSAMPLE, fill=fill,
            outline=outline, width=2 * SUPERSAMPLE,
        )

    def text(y, value, size, bold=False, fill="white"):
        font = ImageFont.truetype(str(BOLD_FONT if bold else FONT), size * SUPERSAMPLE)
        draw.text(
            (SIZE * SUPERSAMPLE / 2, y * SUPERSAMPLE), value,
            font=font, fill=fill, anchor="mm",
        )

    rect((12, 12, 180, 180), color, radius=14, outline="#101820")
    if facing:
        rect(BAR_RECTS[facing], FACING_COLOR, radius=2)
    text(42, label, 14, bold=True)
    text(77, name, 21, bold=True)
    text(110, f"{state}:{frame}/{total}", 19)
    text(140, facing[0].upper() if facing else "STATIC", 16, bold=True)
    # A diagnostic phase marker, not a collision or gameplay animation event.
    if total > 1:
        for index in range(total):
            x = 65 + index * 24
            rect((x, 154, x + 14, 159), "white" if index == frame - 1 else "#18313B", radius=2)
    return canvas.resize((SIZE, SIZE), Image.Resampling.LANCZOS)


def main():
    for path in (FONT, BOLD_FONT):
        if not path.is_file():
            raise SystemExit(f"Required reference font missing: {path}")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    entries = []
    images = {}

    def emit(category, state, facing, frame=1, total=1):
        direction = facing or "none"
        key = f"{category}/{state}/{direction}/frame-{frame:02}"
        filename = f"{category}-{state}-{direction}-{frame:02}.png"
        picture = sprite(category, state, facing, frame, total)
        picture.save(OUTPUT / filename)
        images[key] = picture
        entries.append({
            "key": key, "file": filename, "category": category,
            "sample_name": PALETTE[category][2], "state": state,
            "facing": facing, "frame": frame, "frame_count": total,
            "canvas_px": [SIZE, SIZE], "anchor_px": [96, 180],
            "draw_bounds_px": [12, 12, 180, 180],
            "precomposed_debug_labels": True,
        })
        return key

    categories = []
    for category in PALETTE:
        facing = "north" if category == "npc" else "south" if category == "player" else None
        categories.append(emit(category, "idle" if facing else "illustration", facing))
    walks = [
        emit("npc", "walk", facing, frame, 3)
        for frame in range(1, 4) for facing in BAR_RECTS
    ]

    sheet = Image.new("RGBA", (1240, 1116), "#101820")
    draw = ImageDraw.Draw(sheet)

    def heading(x, y, value, size=20, bold=False, color="#DCE7EB"):
        font = ImageFont.truetype(str(BOLD_FONT if bold else FONT), size)
        draw.text((x, y), value, font=font, fill=color)

    heading(24, 17, "Diagnostic sprite reference", 30, bold=True)
    heading(24, 64, "Generated development stand-ins | Proposed colors | Finished art replaces visual bindings", 17)
    for index, key in enumerate(categories):
        sheet.alpha_composite(images[key], (24 + index * 200, 105))
    heading(24, 322, "One NPC: three walk frames across four facing directions", 22, bold=True)
    heading(24, 360, "Green edge = perception facing. Frame label and white phase dot change with the frame.", 17)
    for index, key in enumerate(walks):
        row, col = divmod(index, 4)
        sheet.alpha_composite(images[key], (212 + col * 208, 405 + row * 208))
    heading(24, 1050, "Sample asset sheet only. No game scene, animation playback, or employment controller is implemented.", 17)
    sheet.save(OUTPUT / "reference-sheet.png")

    manifest = {
        "schema_version": "diagnostic-reference/0.1",
        "purpose": "Engine-independent precomposed samples, not a runtime asset API",
        "animation_runtime_implemented": False,
        "reference_projection": "top-down north-up",
        "frame_numbering": "one-based",
        "facing_color": FACING_COLOR,
        "palette": {key: value[0] for key, value in PALETTE.items()},
        "directions": list(BAR_RECTS),
        "pillow_version": pillow_version,
        "font_sha256": {path.name: sha256(path.read_bytes()).hexdigest() for path in (FONT, BOLD_FONT)},
        "reference_sheet": "reference-sheet.png",
        "entries": entries,
    }
    (OUTPUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(f"Generated {len(entries)} sample frames, reference-sheet.png and manifest.json in {OUTPUT}")


if __name__ == "__main__":
    main()
