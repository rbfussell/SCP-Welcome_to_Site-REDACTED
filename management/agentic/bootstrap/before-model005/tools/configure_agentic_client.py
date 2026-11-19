#!/usr/bin/env python3
"""Configure only this game's workspace MCP server; never alter global settings."""
import argparse
import json
from pathlib import Path
import re
import sys
import tomllib

GAME = Path(__file__).resolve().parents[1]
NAME = 'scp-game-harness'


def configure(workspace, game=GAME, output=None, check=False):
    workspace, game = Path(workspace).resolve(), Path(game).resolve()
    if not game.is_relative_to(workspace):
        raise ValueError('Game must be within this workspace')
    entry = {'command': str(game / '.venv-harness/bin/python'),
             'args': [str(game / 'tools/agentic_mcp.py')], 'cwd': str(game),
             'startup_timeout_sec': 30, 'tool_timeout_sec': 3600}
    for name in ('command',):
        if not Path(entry[name]).is_file():
            raise ValueError(f'Missing installed harness interpreter: {entry[name]}')
    if not Path(entry['args'][0]).is_file():
        raise ValueError('Create and test the game MCP bridge before configuring it')
    target = Path(output) if output else workspace / '.codex/config.toml'
    target = target.resolve()
    if not target.is_relative_to(workspace):
        raise ValueError('Configuration target escapes workspace')
    old = target.read_text() if target.exists() else ''
    current = tomllib.loads(old).get('mcp_servers', {}).get(NAME)
    block = f'[mcp_servers.{NAME}]\n' + ''.join(f'{key} = {json.dumps(value)}\n' for key, value in entry.items())
    if current is not None and current != entry:
        raise ValueError('Existing custom game MCP entry differs; preserve and inspect it')
    if current is not None:
        matches = list(re.finditer(r'(?ms)^\[mcp_servers\.scp-game-harness\][^\n]*\n.*?(?=^\[|\Z)', old))
        if len(matches) != 1:
            raise ValueError('Cannot safely locate existing game MCP section')
        match = matches[0]
        updated = old[:match.start()] + block + '\n' + old[match.end():]
    else:
        updated = old.rstrip() + ('\n\n' if old.strip() else '') + block + '\n'
    # Compare parsed entries, preserving unrelated text verbatim on already-configured repeats.
    changed = current != entry
    if changed and not check:
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(updated)
    return {'status': 'MISSING' if check and changed else 'CONFIGURED_UNVERIFIED',
            'changed': changed, 'path': str(target.relative_to(workspace)),
            'server': NAME, 'host_tool_observed': False,
            'note': 'Configuration is not a live app connection. Verify in the host after its MCP reload.'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--workspace', type=Path, required=True)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    try:
        result = configure(args.workspace, output=args.output, check=args.check)
        print(json.dumps(result, indent=2))
        sys.exit(1 if result['status'] == 'MISSING' else 0)
    except (ValueError, OSError) as error:
        print(f'BLOCKED: {error}', file=sys.stderr); sys.exit(1)
