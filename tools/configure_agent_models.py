#!/usr/bin/env python3
"""Install workspace defaults and owned custom-agent files; preserve unrelated config."""
import argparse
import json
from pathlib import Path
import re
import tomllib
from agent_model_policy import load_policy, POLICY, REGISTRY, read_json

MARKER = '# Managed by SCP game model policy v1'


def safe_target(root, path):
    if not path.is_relative_to(root):
        raise ValueError('Target escapes workspace')
    for candidate in [path, *path.parents]:
        if candidate == root.parent:
            break
        if candidate.is_symlink():
            raise ValueError(f'Symlink target rejected: {candidate}')
    if path.exists() and not path.is_file():
        raise ValueError(f'Target is not a regular file: {path}')


def configure(workspace, check=False, policy_path=POLICY, registry=REGISTRY):
    raw_root = Path(workspace).absolute()
    if raw_root.is_symlink():
        raise ValueError('Symlink workspace rejected')
    root = raw_root.resolve()
    policy = load_policy(policy_path, registry)
    config = root / '.codex/config.toml'
    safe_target(root, config)
    old = config.read_text() if config.exists() else ''
    before = tomllib.loads(old)
    # Replace only ordinary top-level scalar lines. Exotic multiline/dotted syntax
    # fails parsed-preservation validation before any write.
    lines = old.splitlines(keepends=True)
    boundary = next((i for i, line in enumerate(lines) if line.lstrip().startswith('[')), len(lines))
    head = ''.join(line for line in lines[:boundary] if not re.match(r'^\s*(model|model_reasoning_effort)\s*=', line))
    defaults = policy['coordinator']
    new = (f'model = {json.dumps(defaults["model"])}\n'
           f'model_reasoning_effort = {json.dumps(defaults["reasoning_effort"])}\n' + head + ''.join(lines[boundary:]))
    agent_defaults = {'default_subagent_model': defaults['model'],
                      'default_subagent_reasoning_effort': defaults['reasoning_effort']}
    block = ''.join(f'{key} = {json.dumps(value)}\n' for key, value in agent_defaults.items())
    if 'agents' in before:
        matches = list(re.finditer(r'(?m)^\[agents\][ \t]*(?:#[^\n]*)?\n', new))
        if len(matches) != 1 or not isinstance(before['agents'], dict):
            raise ValueError('Cannot safely locate agents table')
        start = matches[0].end()
        next_table = re.search(r'(?m)^\s*\[', new[start:])
        end = start + next_table.start() if next_table else len(new)
        body = ''.join(line for line in new[start:end].splitlines(keepends=True)
                       if not re.match(r'^\s*default_subagent_(model|reasoning_effort)\s*=', line))
        new = new[:start] + block + body + new[end:]
    else:
        new = new.rstrip() + '\n\n[agents]\n' + block
    after = tomllib.loads(new)
    expected = dict(before)
    expected.update(model=defaults['model'], model_reasoning_effort=defaults['reasoning_effort'])
    expected['agents'] = {**before.get('agents', {}), **agent_defaults}
    if after != expected:
        raise ValueError('Cannot safely preserve unrelated configuration')
    plans = [(config, old, new)]
    registered = {r['agent_id']: r for r in read_json(registry)['agents']}
    for role, allocation in policy['roles'].items():
        path = root / '.codex/agents' / f'scp-{role.lower()}.toml'
        safe_target(root, path)
        previous = path.read_text() if path.exists() else ''
        if path.exists() and not previous.startswith(MARKER + '\n'):
            raise ValueError(f'Unmanaged custom agent conflict: {path}')
        if previous:
            tomllib.loads(previous)
        definition = registered[role]
        pair = policy['work_classes'][allocation['default_work_class']]
        instructions = '\n'.join([
            f"Act as {role} ({definition['name']}) for the authorized assignment.",
            definition['jurisdiction'],
            *definition['prohibited_actions'],
            'Follow repository AGENTS.md and the exact assignment, including its dependencies and owned paths.',
            'Preserve independent review and actual evidence. Requested model allocation requires host observation to establish effective settings.',
        ])
        values = {'name': f'scp-{role.lower()}', 'description': definition['description'],
                  'model': pair['model'], 'model_reasoning_effort': pair['reasoning_effort'],
                  'developer_instructions': instructions}
        content = MARKER + '\n' + ''.join(f'{key} = {json.dumps(value)}\n' for key, value in values.items())
        tomllib.loads(content)
        plans.append((path, previous, content))
    changes = [str(p.relative_to(root)) for p, previous, content in plans if previous != content]
    # All content and targets are validated before the first mutation.
    if not check:
        for path, previous, content in plans:
            if previous != content:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content)
    return {'status': 'MISSING' if check and changes else 'CONFIGURED_UNVERIFIED',
            'changed_paths': changes, 'workspace': str(root), 'role_count': len(policy['roles']),
            'effective_status': 'NOT_EXPOSED', 'scope': 'Future starts; live coordinator unchanged. Active collaboration uses explicit overrides.'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--workspace', type=Path, required=True)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    try:
        result = configure(args.workspace, args.check)
        print(json.dumps(result, indent=2))
        raise SystemExit(1 if result['status'] == 'MISSING' else 0)
    except (ValueError, OSError, KeyError) as error:
        parser.exit(1, f'BLOCKED: {error}\n')
