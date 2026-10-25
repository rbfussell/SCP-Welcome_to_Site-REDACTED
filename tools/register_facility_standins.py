"""Register unchanged generated sprite sources by atlas region/feet anchor; never edit PNG pixels."""
import argparse
import hashlib
import json
import statistics
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
FOLDER = ROOT / 'game/Assets/facility-standins'


def character_rows(alpha):
    cuts = [0]
    opaque = alpha.point(lambda value: 255 if value >= 160 else 0)
    for boundary in (256, 512, 768):
        clear = [y for y in range(boundary - 16, boundary + 21)
                 if opaque.crop((0, y, alpha.width, y + 1)).getbbox() is None]
        if not clear:
            raise ValueError('No clear row separating generated animation frames')
        cuts.append((clear[0] + clear[-1]) // 2)
    return cuts + [alpha.height]


def build():
    images, sprites, actors = {}, {}, {}
    for name in ('chen', 'ortiz', 'player', 'floor', 'wall', 'door', 'land', 'props'):
        with Image.open(FOLDER / f'{name}.png') as image:
            filename = f'facility-standins/{name}.png'
            images[filename] = list(image.size)
            if name in ('chen', 'ortiz', 'player'):
                if image.size != (1536, 1024) or image.mode != 'RGBA':
                    raise ValueError(f'{name}: expected transparent 6-column/4-row source')
                alpha = image.getchannel('A')
                cuts = character_rows(alpha)
                frames = []
                for row in range(4):
                    for column in range(6):
                        region = [column * 256, cuts[row], 256, cuts[row + 1] - cuts[row]]
                        cell = alpha.crop((region[0], region[1], region[0] + region[2], region[1] + region[3]))
                        mask = cell.point(lambda value: 255 if value >= 160 else 0)
                        box = mask.getbbox()
                        if not box or box[0] == 0 or box[1] == 0 or box[2] == cell.width or box[3] == cell.height:
                            raise ValueError(f'{name}/{row}/{column}: foreground clipped at frame boundary')
                        foot = mask.crop((0, max(box[1], box[3] - 26), cell.width, box[3])).getbbox()
                        frames.append((region, box, [(foot[0] + foot[2]) / 2, box[3] - 1]))
                scale = round(58 / statistics.median(f[1][3] - f[1][1] for f in frames[:6]), 6)
                for index, (region, _, anchor) in enumerate(frames):
                    row, column = divmod(index, 6)
                    state, frame = ('idle', 0) if row == 0 else ('walk', row - 1)
                    sprites[f'facility-{name}-{state}-{column}-{frame}'] = dict(file=filename, region=region, anchor=anchor, scale=scale)
                states = {state: [[f'facility-{name}-{state}-{facing}-{frame}' for frame in range(count)]
                                  for facing in range(6)] for state, count in [('idle', 1), ('walk', 3)]}
                actor_id = dict(chen='observer', ortiz='tech', player='avatar')[name]
                actors[actor_id] = dict(rig='human-isometric-six-v1', layers=[dict(id='baked-outfit', states=states)])
            elif name == 'props':
                if image.mode != 'RGBA' or image.width != image.height or image.width % 2:
                    raise ValueError('Expected transparent 2x2 prop sheet')
                cell = image.width // 2
                for prop, column, row, anchor, scale in [
                    ('crate', 0, 0, [320, 470], .085), ('cabinet', 1, 0, [315, 585], .11),
                    ('terminal', 0, 1, [320, 455], .10), ('trolley', 1, 1, [295, 450], .085)
                ]:
                    sprites['facility-' + prop] = dict(file=filename, region=[column * cell, row * cell, cell, cell],
                        anchor=[round(n * cell / 640, 3) for n in anchor], scale=round(scale * 640 / cell, 6))
            else:
                if image.width != image.height:
                    raise ValueError(f'{name}: expected square planar surface source')
                sprites['facility-' + name] = dict(file=filename, region=[0, 0, image.width, image.height], anchor=[0, 0], scale=1)
    return dict(version=1, images=images, sprites=sprites,
        rigs={'human-isometric-six-v1': dict(directions=['NE', 'E', 'SE', 'SW', 'W', 'NW'], frames=dict(idle=1, walk=3))},
        actors=actors, objects={kind: 'facility-' + kind for kind in ('crate', 'cabinet', 'terminal', 'trolley')},
        surfaces={kind: 'facility-' + kind for kind in ('floor', 'wall', 'door', 'land')}, mounts={'trolley-support': [0, -5]})


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Check registration without writing files.')
    args = parser.parse_args()
    manifest = build()
    provenance = json.loads((FOLDER / 'provenance.json').read_text())
    for source in provenance['assets'].values():
        if hashlib.sha256((FOLDER / source['file']).read_bytes()).hexdigest() != source['sha256']:
            raise SystemExit('Generated source changed: ' + source['file'])
    text = json.dumps(manifest, indent=2) + '\n'
    path = FOLDER / 'manifest.json'
    if args.check:
        if path.read_text() != text:
            raise SystemExit('Sprite manifest differs from registered sources')
    else:
        path.write_text(text)
    print(f"FACILITY_ART_REGISTRATION_PASS: {len(manifest['images'])} untouched source images, {len(manifest['sprites'])} bindings, 72 character frames")


if __name__ == '__main__':
    main()
