#!/usr/bin/env python3
"""Install/reconcile only the managed software overlay; preserve generic harness files."""
import argparse
import json
from pathlib import Path
import sys

GAME = Path(__file__).resolve().parents[1]
BEGIN = '<!-- BEGIN SOFTWARE-GAME PROFILE -->'
END = '<!-- END SOFTWARE-GAME PROFILE -->'


def configure(root, game=GAME, check=False):
    root, game = Path(root).resolve(), Path(game).resolve()
    if root == game or not game.is_relative_to(root):
        raise ValueError('Game must remain a nested checkout under the harness root')
    for name in ('workflow.py', 'AGENTS.md', 'README.md'):
        if not (root / name).is_file():
            raise ValueError(f'Not a harness root: missing {name}')
    template = game / 'management/harness'
    replacements = {}
    for name in ('AGENTS.md', 'README.md'):
        original = (root / name).read_text()
        if BEGIN in original or END in original:
            if original.count(BEGIN) != 1 or original.count(END) != 1 or original.index(BEGIN) > original.index(END):
                raise ValueError(f'Malformed managed block in {name}; inspect before editing')
            first, remainder = original.split(BEGIN)
            _, tail = remainder.split(END)
            original = first + tail.lstrip('\n')
        content = (template / name).read_text().replace('Project/', game.relative_to(root).as_posix() + '/')
        replacements[root / name] = f'{BEGIN}\n{content.rstrip()}\n{END}\n\n{original}'
    replacements[root / 'config/software-project.json'] = json.dumps({
        'profile': 'software-game', 'project_path': game.relative_to(root).as_posix(),
        'authoritative_profile': 'management/profile.json', 'agentic_entry': 'tools/agentic.py'}, indent=2) + '\n'
    replacements[root / 'scripts/manage_game.py'] = (template / 'manage_game.py').read_text()
    changed = [str(path.relative_to(root)) for path, text in replacements.items() if not path.is_file() or path.read_text() != text]
    if not check:
        for path, text in replacements.items():
            if not path.is_file() or path.read_text() != text:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(text)
    return {'result': 'FAIL' if check and changed else 'PASS', 'changed': changed,
            'note': 'Only managed documentation blocks, project pointer and wrapper are installed; no strict runtime state is migrated.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, required=True)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    result = configure(args.root, check=args.check)
    print(json.dumps(result, indent=2))
    return 1 if result['result'] == 'FAIL' else 0


if __name__ == '__main__':
    try: sys.exit(main())
    except (ValueError, OSError) as error:
        print(f'BLOCKED: {error}', file=sys.stderr); sys.exit(1)
