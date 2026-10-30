"""Author the native nine-shoulder body and its matching clothing/contact masks.

Rows use odd-row offset coordinates: odd rows need even-width mirrored spans.
The runtime JSON is derived here; the ordinary fit evaluator stays authoritative.
"""
import argparse
from copy import deepcopy
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / 'src/SiteSimulation/Inventory/Content/body-fit.json'
KIT = ROOT / 'src/SiteSimulation/Inventory/Content/field-kit.json'
BODY = [
    '...###...', '..####...', '...###...', '...##....',
    '#########', '########.', '#.#####.#', '#.####.#.',
    '#.#####.#', '#.####.#.', '#.#####.#', '.######..',
    '..#####..', '.##..##..', '..##.##..', '.##..##..',
    '..##.##..', '.##..##..', '..##.##..', '###..###.',
]
DIRECTIONS = [(1, -1), (1, 0), (0, 1), (-1, 1), (-1, 0), (0, -1)]
RANGES = {'shirt': (4, 8), 'coat': (4, 11), 'trousers': (11, 18),
          'socks': (18, 19), 'boots': (17, 19), 'chem-suit': (0, 19)}
POSES = {'shirt': (-2, 4), 'coat': (-2, 4), 'trousers': (-6, 12),
         'socks': (-9, 18), 'boots': (-9, 18), 'chem-suit': (0, 0),
         'backpack': (-2, 4), 'belt': (-6, 12)}


def cells(rows):
    return {(c - len(row) // 2 - r // 2, r) for r, row in enumerate(rows) for c, v in enumerate(row) if v == '#'}


def point(q, r):
    return {'q': q, 'r': r}


def local(points, pose):
    return [point(q - pose[0], r - pose[1]) for q, r in sorted(points, key=lambda c: (c[1], c[0]))]


def mask(surface, points, pose):
    return {'surface': surface, 'cells': local(points, pose)}


def pretty_catalog(value):
    text = json.dumps(value, indent=2)
    text = re.sub(r'\{\n\s+"q": (-?\d+),\n\s+"r": (-?\d+)\n\s+\}', r'{ "q": \1, "r": \2 }', text)
    text = re.sub(r'\{\n\s+"cell": (\{ "q": -?\d+, "r": -?\d+ \}),\n\s+"direction": (\d+)\n\s+\}', r'{ "cell": \1, "direction": \2 }', text)
    return text + '\n'


def author():
    catalog = json.loads(CATALOG.read_text())
    kit = json.loads(KIT.read_text())
    body = next(b for b in catalog['bodies'] if b['id'] == 'human-nine-shoulder')
    body['rows'] = BODY
    body_points = cells(BODY)
    assert all((-q - r, r) in body_points for q, r in body_points)
    for shape in catalog['shapes']:
        name = shape['id']
        if name not in POSES:
            continue
        if name in RANGES:
            first, last = RANGES[name]
            rows = [row if first <= r <= last else '.' * 9 for r, row in enumerate(BODY)]
        elif name == 'belt':
            rows = ['..#####..' if r == 12 else '.' * 9 for r in range(20)]
        else:
            rows = ['##.....##' if r == 4 else ''.join('#' if value == '#' and 2 <= c <= (5 if r % 2 else 6) else '.'
                    for c, value in enumerate(row)) if 5 <= r <= 9 else '.' * 9 for r, row in enumerate(BODY)]
        pose = POSES[name]
        shape['rows'] = rows
        shape['defaultPose'] = dict(q=pose[0], r=pose[1], rotation=0)
        points = cells(rows)
        if name == 'belt':
            shape['linearSpan'] = {'minCells': 5, 'maxCells': 10}
            shape['contacts'] = [{'cell': point(-2, 0), 'direction': 4}, {'cell': point(2, 0), 'direction': 1}]
        else:
            # Every body-boundary edge is closed; an edge continuing into body remains open.
            shape['contacts'] = [{'cell': point(q - pose[0], r - pose[1]), 'direction': d}
                for q, r in sorted(points, key=lambda c: (c[1], c[0]))
                for d, (dq, dr) in enumerate(DIRECTIONS) if (q + dq, r + dr) not in body_points]
        sides = ['mounted'] if name == 'backpack' else ['front', 'back']
        closed = [mask(side, points, pose) for side in sides]
        opened = deepcopy(closed)
        if name == 'coat':
            # Shoulders remain covered; open fronts leave sleeves and side hems only.
            edge_points = {p for p in points if p[1] == 4 or abs(p[0] + p[1] / 2) ==
                           max(abs(q + r / 2) for q, r in points if r == p[1])}
            opened[0] = mask('front', edge_points, pose)
        shape['coverage'] = dict(closed=closed, open=opened)
    for definition in kit['definitions']:
        if definition['id'] == 'trousers':
            mouths = {'left': [(-7, 11), (-7, 12)], 'right': [(-4, 11), (-5, 12)],
                      'cargo-left': [(-8, 14), (-9, 15)], 'cargo-right': [(-6, 14), (-6, 15)]}
            for region in definition['regions']:
                region['accessFootprint'] = [mask('front', mouths[region['id']], POSES['trousers'])]
        elif definition['id'] == 'coat':
            center = {(q, r) for q, r in body_points if 4 <= r <= 11 and abs(q + r / 2) <= .5}
            definition['wear']['fasteningFootprint'] = [mask('front', center, POSES['coat'])]
    return {CATALOG: pretty_catalog(catalog), KIT: json.dumps(kit, indent=2) + '\n'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true', help='Verify generated native fixture sources without writing.')
    args = parser.parse_args()
    for path, text in author().items():
        if args.check:
            if path.read_text() != text:
                raise SystemExit('Out-of-date body-fit fixture: ' + str(path.relative_to(ROOT)))
        else:
            path.write_text(text)
    print('BODY_FIXTURE_' + ('CHECKS_PASS' if args.check else 'AUTHORED') + ': symmetric body, clothing, contacts and mapped access')


if __name__ == '__main__':
    main()
