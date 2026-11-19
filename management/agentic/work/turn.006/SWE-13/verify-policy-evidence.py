#!/usr/bin/env python3
"""Verify the retained focused test transcript against the files it exercised."""
import hashlib
import json
from pathlib import Path
import re
import sys

OWNED = Path(__file__).resolve().parent
ROOT = OWNED.parents[4]
INPUTS = {
    'management/model-policy/policy.json',
    'management/harness/runtime/config/game_roles.json',
    'tools/agent_model_policy.py',
    'tools/configure_agent_models.py',
    'tests/test_agent_model_policy.py',
}
ARGV = ['python3', '-m', 'unittest', 'discover', '-s', 'tests',
        '-p', 'test_agent_model_policy.py', '-v']


def check():
    record = json.loads((OWNED / 'policy-test-evidence.json').read_text())
    if record['argv'] != ARGV or record['exit_code'] != 0:
        raise ValueError('Expected successful focused unittest run')
    if set(record['input_hashes']) != INPUTS:
        raise ValueError('Focused suite input coverage mismatch')
    for path, expected in record['input_hashes'].items():
        if hashlib.sha256((ROOT / path).read_bytes()).hexdigest() != expected:
            raise ValueError(f'Test input changed: {path}')
    log = (OWNED / 'policy-tests.log').read_bytes()
    if hashlib.sha256(log).hexdigest() != record['log_sha256']:
        raise ValueError('Test transcript hash mismatch')
    match = re.search(rb'(?m)^Ran (\d+) tests? in ', log)
    if not match or int(match[1]) != record['test_count'] or not log.rstrip().endswith(b'\nOK'):
        raise ValueError('Transcript lacks the recorded successful test summary')
    print(f"Verified retained focused unittest transcript: {record['test_count']} passing tests")


if __name__ == '__main__':
    try:
        check()
    except (OSError, ValueError, KeyError) as error:
        sys.exit(f'FAIL: {error}')
