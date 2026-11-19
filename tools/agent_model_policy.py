#!/usr/bin/env python3
"""Resolve requested host parameters; report observed settings without inference."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
POLICY = ROOT / 'management/model-policy/policy.json'
REGISTRY = ROOT / 'management/harness/runtime/config/game_roles.json'
SOL = 'gpt-6.2-sol'
ASTRA = 'gpt-6.2-astra'
EFFORTS = ('low', 'medium', 'high', 'xhigh')
HARD_CAPS = {SOL: 'xhigh', ASTRA: 'high'}


def effort_rank(effort):
    return EFFORTS.index(effort)


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f'Duplicate key: {key}')
        result[key] = value
    return result


def read_json(path):
    return json.loads(Path(path).read_text(), object_pairs_hook=unique)


def load_policy(path=POLICY, registry=REGISTRY):
    policy = read_json(path)
    roles = [r['agent_id'] for r in read_json(registry)['agents']]
    if policy['schema_version'] != 2 or set(policy['roles']) != set(roles) or len(roles) != len(set(roles)):
        raise ValueError('Policy version or registry coverage mismatch')
    supported = {model: list(EFFORTS[:effort_rank(cap) + 1]) for model, cap in HARD_CAPS.items()}
    if policy['supported_models'] != supported:
        raise ValueError('Unsupported model capabilities or hard caps')
    for pair in [policy['coordinator'], *policy['work_classes'].values()]:
        if set(pair) != {'model', 'reasoning_effort'} or pair['model'] not in supported or pair['reasoning_effort'] not in ('low', 'medium'):
            raise ValueError('Invalid coordinator or work-class settings')
    if policy['coordinator']['model'] != SOL:
        raise ValueError('Coordinator must default to Sol')
    for role, allocation in policy['roles'].items():
        if set(allocation) != {'default_work_class', 'floor', 'ceilings'}:
            raise ValueError(f'Invalid allocation fields for {role}')
        default = policy['work_classes'].get(allocation['default_work_class'])
        floor = allocation['floor']
        ceilings = allocation['ceilings']
        if floor not in EFFORTS or SOL not in ceilings:
            raise ValueError(f'Invalid floor or missing Sol ceiling for {role}')
        for model, limit in ceilings.items():
            if limit not in supported.get(model, ()) or effort_rank(limit) < effort_rank(floor):
                raise ValueError(f'Invalid {model} ceiling for {role}')
        if not default or default['model'] != SOL or not (
            effort_rank(floor) <= effort_rank(default['reasoning_effort']) <= effort_rank(ceilings[SOL])
        ):
            raise ValueError(f'Invalid Sol default for {role}')
    return policy


def resolve(role, task, work_class=None, effort=None, reason=None, policy_path=POLICY, registry=REGISTRY):
    policy = load_policy(policy_path, registry)
    if role not in policy['roles'] or not isinstance(task, str) or not task.strip():
        raise ValueError('Known role and nonempty task required')
    allocation = policy['roles'][role]
    default_class = allocation['default_work_class']
    default = policy['work_classes'][default_class]
    selected = work_class if work_class is not None else default_class
    if selected not in policy['work_classes']:
        raise ValueError('Unknown work class')
    pair = dict(policy['work_classes'][selected])
    if effort is not None:
        effort = effort.strip().lower()
        pair['reasoning_effort'] = 'low' if effort == 'light' else effort
    model, requested_effort = pair['model'], pair['reasoning_effort']
    ceiling = allocation['ceilings'].get(model)
    if requested_effort not in policy['supported_models'][model]:
        raise ValueError(f'{model} rejects {requested_effort!r}; hard cap {HARD_CAPS[model]}')
    if ceiling is None:
        raise ValueError(f'{role} does not permit {model}')
    if not effort_rank(allocation['floor']) <= effort_rank(requested_effort) <= effort_rank(ceiling):
        raise ValueError(f"{role} requires {allocation['floor']} through {ceiling} on {model}")
    escalated = model == ASTRA or effort_rank(requested_effort) > effort_rank(default['reasoning_effort'])
    if escalated and (not isinstance(reason, str) or len(reason.strip()) < 30 or task.lower() not in reason.lower()):
        raise ValueError('Escalation requires substantive reason naming this task')
    return {'schema_version': 1, 'role': role, 'task': task, 'work_class': selected,
            'selection_source': 'work_class_override' if work_class else 'role_default',
            'reason': reason, 'policy_sha256': hashlib.sha256(Path(policy_path).read_bytes()).hexdigest(),
            'allocation': {'floor': allocation['floor'], 'default': default, 'ceilings': allocation['ceilings']},
            'escalated': escalated,
            'requested': pair, 'spawn_parameters': {**pair, 'fork_turns': 'none'},
            'dispatch_mechanism': 'explicit_host_overrides', 'effective': {'model': None, 'reasoning_effort': None},
            'effective_status': 'NOT_EXPOSED'}


def validate_spawn_parameters(resolution, arguments):
    if not isinstance(arguments, dict):
        raise ValueError('Spawn arguments must be a JSON object')
    if arguments.get('agent_type', 'default') != 'default':
        raise ValueError('Explicit overrides require the default collaboration agent type')
    for key, expected in resolution['spawn_parameters'].items():
        if arguments.get(key) != expected:
            raise ValueError(f'Spawn argument {key} differs from the policy request')
    return 'MATCH'


def provenance(resolution, receipt, observed=None):
    if not isinstance(receipt, str) or not receipt.strip():
        raise ValueError('Actual host receipt required')
    observed = observed or {}
    effective = {key: observed.get(key) for key in ('model', 'reasoning_effort')}
    requested = resolution['requested']
    mismatched = any(value is not None and value != requested[key] for key, value in effective.items())
    status = 'MISMATCH' if mismatched else ('NOT_EXPOSED' if None in effective.values() else 'MATCH')
    return {**resolution, 'host_receipt': receipt, 'host_reported': observed,
            'effective': effective, 'effective_status': status,
            'authentication': 'External evidence assertion; not authenticated by this helper'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--role', required=True)
    parser.add_argument('--task', required=True)
    parser.add_argument('--work-class')
    parser.add_argument('--effort')
    parser.add_argument('--reason')
    parser.add_argument('--spawn-args', type=Path, help='JSON object of actual collaboration spawn arguments')
    parser.add_argument('--receipt')
    parser.add_argument('--observed', type=Path, help='JSON of actual host-reported metadata only')
    args = parser.parse_args()
    try:
        result = resolve(args.role, args.task, args.work_class, args.effort, args.reason)
        if args.spawn_args:
            result['spawn_arguments_status'] = validate_spawn_parameters(result, read_json(args.spawn_args))
        if args.observed and not args.receipt:
            raise ValueError('Observed metadata requires receipt')
        if args.receipt:
            result = provenance(result, args.receipt, read_json(args.observed) if args.observed else None)
        print(json.dumps(result, indent=2))
        return 2 if result['effective_status'] == 'MISMATCH' else 0
    except (ValueError, KeyError, OSError) as error:
        parser.exit(1, f'BLOCKED: {error}\n')


if __name__ == '__main__':
    raise SystemExit(main())
