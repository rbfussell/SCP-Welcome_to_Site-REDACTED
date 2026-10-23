"""Original deterministic raster stand-ins for the M0 spatial scene (Pillow only)."""
from pathlib import Path
import argparse
import json
import random
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'game/Assets'


def emit(name, image, anchor, records):
    image.save(OUT / f'{name}.png')
    records[name] = {'file': f'{name}.png', 'anchor': anchor, 'size': list(image.size)}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--walk-frames", type=int, default=3)
    args = parser.parse_args()
    if not 1 <= args.walk_frames <= 24:
        parser.error("Choose 1–24 walk frames.")
    OUT.mkdir(parents=True, exist_ok=True)
    records = {}
    for kind, base in [('floor', (93, 98, 94)), ('land', (48, 55, 43))]:
        for index in range(6):
            rng = random.Random(702 + index)
            image = Image.new('RGBA', (81, 37))
            mask = Image.new('1', image.size)
            draw = ImageDraw.Draw(mask)
            draw.polygon([(0, 12), (48, 0), (80, 24), (32, 36)], fill=1)
            for y in range(37):
                for x in range(81):
                    if mask.getpixel((x, y)):
                        n = rng.choice([-6, -3, -1, 0, 0, 1, 3, 5])
                        image.putpixel((x, y), tuple(c+n for c in base)+(255,))
            draw = ImageDraw.Draw(image)
            if kind == 'floor':
                draw.line([(0, 12), (48, 0), (80, 24)], fill=(119, 123, 115, 255))
                draw.line([(0, 13), (32, 36), (80, 24)], fill=(62, 68, 66, 255))
                for x, y in [(22, 12), (53, 21)]:
                    draw.point((x, y), (143, 145, 131, 255))
            emit(f'{kind}-{index}', image, [0, 12], records)
    for axis, (dx, dy) in [('u', (48, -12)), ('v', (32, 24))]:
        for door in [False, True]:
            a, b = (5, 75), (5+dx, 75+dy)
            image = Image.new('RGBA', (dx+12, 105))
            d = ImageDraw.Draw(image)
            top_a, top_b = (a[0], a[1]-56), (b[0], b[1]-56)
            face = (106, 101, 111, 255) if axis == 'u' else (77, 77, 87, 255)
            d.polygon([a, b, top_b, top_a], fill=face)
            d.polygon([top_a, top_b, (top_b[0]-4, top_b[1]-3), (top_a[0]-4, top_a[1]-3)], fill=(154, 151, 153, 255))
            d.line([top_a, top_b], fill=(194, 187, 170, 255))
            d.line([a, b], fill=(31, 37, 37, 255), width=3)
            for step in [15, 32, 47]:
                d.line([(a[0], a[1]-step), (b[0], b[1]-step)], fill=(83, 82, 90, 255))
            d.line([top_a, a], fill=(137, 132, 139, 255))
            if door:
                d.polygon([(a[0]+4, a[1]-3), (b[0]-4, b[1]-3), (b[0]-4, b[1]-47), (a[0]+4, a[1]-47)], fill=(51, 71, 68, 255))
                d.line([(a[0]+dx//2, a[1]+dy//2-5), (a[0]+dx//2, a[1]+dy//2-45)], fill=(21, 35, 33, 255), width=2)
                d.rectangle((a[0]+7, a[1]-46, a[0]+12, a[1]-43), fill=(229, 181, 94, 255))
            emit(f'{"door" if door else "wall"}-{axis}', image, list(a), records)
    for kind, color in [('player', (199, 77, 121)), ('npc', (61, 143, 163))]:
        for state in ['idle', 'walk']:
            for facing in range(6):
                for frame in range(args.walk_frames if state == 'walk' else 1):
                    image = Image.new('RGBA', (34, 62))
                    d = ImageDraw.Draw(image)
                    offset = [0, 2, -2][frame % 3]
                    d.ellipse((5, 53, 29, 61), fill=(8, 13, 12, 130))
                    d.rectangle((11-offset, 41, 15-offset, 56), fill=(40, 46, 47, 255))
                    d.rectangle((20+offset, 41, 24+offset, 56), fill=(35, 40, 41, 255))
                    d.rectangle((9-offset, 54, 15-offset, 58), fill=(18, 23, 24, 255))
                    d.rectangle((20+offset, 54, 27+offset, 58), fill=(18, 23, 24, 255))
                    d.polygon([(9, 20), (24, 20), (28, 41), (7, 41)], fill=color+(255,))
                    d.rectangle((10, 37, 25, 40), fill=(36, 39, 38, 255))
                    d.rectangle((6, 23, 10, 37), fill=tuple(max(c-20, 0) for c in color)+(255,))
                    d.rectangle((25, 23, 29, 37), fill=color+(255,))
                    d.rectangle((12, 7, 23, 19), fill=(174, 154, 129, 255))
                    d.rectangle((11, 5, 24, 9), fill=(57, 56, 49, 255))
                    # Direction marker belongs to the frame binding, not movement authority.
                    marker = [(21, 3, 31, 5), (29, 19, 31, 30), (23, 58, 32, 60), (1, 58, 10, 60), (1, 19, 3, 30), (3, 3, 12, 5)][facing]
                    d.rectangle(marker, fill=(97, 255, 112, 255))
                    emit(f'{kind}-{state}-{facing}-{frame}', image, [17, 58], records)
    for kind in ['crate', 'cabinet', 'pose']:
        image = Image.new('RGBA', (44, 78))
        d = ImageDraw.Draw(image)
        if kind == 'crate':
            d.polygon([(2, 53), (26, 47), (41, 58), (17, 65)], fill=(176, 133, 74, 255))
            d.polygon([(2, 53), (17, 65), (17, 76), (2, 64)], fill=(108, 78, 43, 255))
            d.polygon([(17, 65), (41, 58), (41, 68), (17, 76)], fill=(132, 98, 50, 255))
            d.line([(7, 53), (21, 64), (21, 74)], fill=(199, 158, 87, 255), width=2)
        elif kind == 'cabinet':
            d.polygon([(7, 13), (29, 7), (39, 17), (17, 23)], fill=(113, 122, 117, 255))
            d.polygon([(7, 13), (17, 23), (17, 75), (7, 65)], fill=(55, 68, 65, 255))
            d.polygon([(17, 23), (39, 17), (39, 69), (17, 75)], fill=(78, 93, 88, 255))
            for y in [35, 51, 67]:
                d.line([(20, y), (36, y-4)], fill=(39, 52, 49, 255))
                d.rectangle((25, y-9, 29, y-7), fill=(161, 157, 123, 255))
        else:
            d.rectangle((13, 10, 29, 75), fill=(61, 64, 66, 255), outline=(151, 135, 163, 255), width=2)
            d.rectangle((6, 26, 36, 34), fill=(108, 91, 122, 255))
            d.rectangle((17, 15, 25, 21), fill=(190, 177, 194, 255))
        emit(kind, image, [22, 75], records)
    (OUT / 'spatial-manifest.json').write_text(json.dumps({'version': 1, 'source': 'tools/generate_spatial_assets.py; original diagnostic artwork', 'animation': {'idle_frames': 1, 'walk_frames': args.walk_frames, 'walk_cycle_ms': 300}, 'sprites': records}, indent=2)+'\n')
    print(f'Generated {len(records)} original spatial stand-ins.')

if __name__ == '__main__':
    main()
