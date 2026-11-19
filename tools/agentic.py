#!/usr/bin/env python3
"""CLI for the genuine game harness. A recorded invocation never spawns a worker."""
import argparse
import json
from pathlib import Path
import sys

from game_workflow import GameWorkflow

ROOT = Path(__file__).resolve().parents[1]
OPERATIONS = ('init', 'inventory', 'status', 'begin', 'plan', 'select', 'authorize',
              'invoke', 'handoff', 'advance', 'collect', 'audit', 'publish', 'interrupt',
              'import', 'register', 'question', 'answer', 'request-research',
              'respond-research', 'failed', 'readiness')


def execute(operation, arguments=None, root=ROOT):
    """Shared dispatch table used by CLI and MCP; all mutations use the real runtime."""
    args = dict(arguments or {})
    workflow = GameWorkflow(root)
    if operation == 'init':
        return workflow.initialize()
    if operation == 'inventory':
        return workflow.inventory()
    if operation == 'status':
        return workflow.status(**args)
    if operation == 'begin':
        return workflow.begin(args['assessment'])
    if operation == 'plan':
        return workflow.plan(**args)
    if operation == 'select':
        return workflow.select(**args)
    if operation == 'authorize':
        return workflow.authorize_dispatch(**args)
    if operation == 'invoke':
        return workflow.invoke(**args)
    if operation == 'handoff':
        return workflow.handoff(args['turn_id'], args['value'])
    if operation == 'advance':
        return workflow.advance(**args)
    if operation == 'collect':
        return workflow.build_evidence_package(**args)
    if operation == 'audit':
        return workflow.audit(**args)
    if operation == 'publish':
        return workflow.publish(**args)
    if operation == 'interrupt':
        return workflow.interrupt(**args)
    if operation == 'import':
        return workflow.create_import(args['value'])
    if operation == 'register':
        return workflow.register(**args)
    if operation == 'question':
        return workflow.ask_question(**args)
    if operation == 'answer':
        return workflow.answer_question(**args)
    if operation == 'request-research':
        return workflow.request_research(**args)
    if operation == 'respond-research':
        return workflow.respond_research(**args)
    if operation == 'failed':
        workflow.failed(**args)
        # The runtime appends the failure and returns None. Acknowledge only after
        # that succeeds so CLI and MCP share a dictionary result and keep errors.
        return {**args, 'status': 'FAILED', 'recorded': True}
    if operation == 'readiness':
        return workflow.readiness(**args)
    raise ValueError(f'Unknown harness operation: {operation}')


def unique_keys(pairs):
    value = {}
    for key, item in pairs:
        if key in value:
            raise ValueError(f'Duplicate JSON key: {key}')
        value[key] = item
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT)
    parser.add_argument('operation', choices=OPERATIONS)
    parser.add_argument('--input', type=Path, help='JSON object of named runtime arguments; exact prompts stay in files')
    parser.add_argument('--output', type=Path, help='Save JSON result instead of printing it')
    args = parser.parse_args()
    try:
        arguments = json.loads(args.input.read_text(), object_pairs_hook=unique_keys) if args.input else {}
        if not isinstance(arguments, dict):
            raise ValueError('Input must be a JSON object of named arguments')
        result = execute(args.operation, arguments, args.root)
        rendered = json.dumps(result, indent=2, ensure_ascii=False) + '\n'
        if args.output:
            args.output.parent.mkdir(parents=True, exist_ok=True)
            with args.output.open('x') as output:
                output.write(rendered)
        else:
            print(rendered, end='')
        return 0
    except (ValueError, KeyError, TypeError, OSError) as error:
        print(f'BLOCKED: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
