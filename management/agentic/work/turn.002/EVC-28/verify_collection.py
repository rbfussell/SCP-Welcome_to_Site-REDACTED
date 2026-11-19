"""Read-only Collector inventory; does not repair or accept reviewed work."""
from collections import Counter
from datetime import datetime, timezone
from hashlib import sha256
import json
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / 'tools'))
from game_workflow import GameWorkflow

workflow = GameWorkflow(ROOT)
owned = ROOT / 'management/agentic/work/turn.002/EVC-28'
turn = workflow.turn('turn.002')
previous = workflow.turn('turn.001')
package = json.loads((owned / 'evidence-package.json').read_text())
hex_hash = re.compile(r'[0-9a-f]{64}')


def hashed(path):
    return sha256(path.read_bytes()).hexdigest()


def verify_ref(path, expected, origin):
    resolved = workflow.path(path)
    actual = hashed(resolved) if resolved.is_file() else None
    return {'path': str(resolved.relative_to(ROOT)), 'sha256': expected,
            'actual_sha256': actual, 'status': 'PASS' if actual == expected else
            ('MISSING' if actual is None else 'CHANGED'), 'origin': origin}


refs = []


def visit(value, origin):
    if isinstance(value, dict):
        if isinstance(value.get('sha256'), str) and hex_hash.fullmatch(value['sha256']):
            target = value.get('path') or value.get('log')
            if isinstance(target, str):
                refs.append(verify_ref(target, value['sha256'], origin))
        inputs = value.get('inputs')
        if isinstance(inputs, dict):
            for path, expected in inputs.items():
                if isinstance(expected, str) and hex_hash.fullmatch(expected):
                    refs.append(verify_ref(path, expected, origin + ':inputs'))
        for key, child in value.items():
            visit(child, origin + '/' + key)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            visit(child, origin + '/' + str(index))


for ref in package['artifact_inventory']:
    refs.append(verify_ref(ref['path'], ref['sha256'], 'runtime-package'))

supplemental = set()
for location in [
    'management/agentic/bootstrap', 'management/agentic/rpc/turn.002',
    'management/agentic/prompts/turn.002', 'management/agentic/evidence/turn.001.events',
    'management/agentic/evidence/turn.002.events',
    'management/agentic/work/turn.001', 'management/evidence/agentic-loop-setup',
]:
    supplemental.update(p for p in (ROOT / location).rglob('*') if p.is_file())
for name in [
    'management/agentic/evidence/turn.001.json', 'management/agentic/evidence/turn.002.json',
    'management/agentic/selection-turn.001.json', 'management/agentic/selection-turn.002.json',
    'management/tasks/agentic-loop-setup.json', 'management/contexts/agentic-loop-setup.json',
    'source_material/game-wiki/manifest.json', 'management/harness/manage_game.py',
]:
    supplemental.add(ROOT / name)
for aid in package['reviewed_agents']:
    supplemental.update(p for p in workflow.path(turn['invocations'][aid]['owned_directory']).rglob('*')
                        if p.is_file())
json_sources = {ROOT / ref['path'] for ref in package['artifact_inventory']
                if ref['path'].endswith('.json')}
json_sources.update(p for p in supplemental if p.suffix == '.json')
for path in sorted(json_sources):
    visit(json.loads(path.read_text()), str(path.relative_to(ROOT)))

registry_ids = [a['agent_id'] for a in turn['registry']['agents']]
selection = turn['selection']
selected = sorted(a for a, row in selection.items() if row['selected'])
excluded = sorted(a for a, row in selection.items() if not row['selected'])
events = turn['events']
dispatch_rows = []
for aid, invocation in turn['invocations'].items():
    authorization_event = next(e for e in events if e['kind'] == 'DISPATCH_AUTHORIZED'
                               and e['data']['agent_id'] == aid)
    invocation_event = next(e for e in events if e['kind'] == 'INVOKED'
                            and e['data']['agent_id'] == aid)
    handoff_event = next((e for e in events if e['kind'] == 'HANDOFF'
                          and e['data']['agent_id'] == aid), None)
    dependencies = selection[aid].get('dependencies', [])
    dependency_events = {dep: next(e['sequence'] for e in events if e['kind'] == 'HANDOFF'
                                  and e['data']['agent_id'] == dep) for dep in dependencies}
    prompt = selection[aid]['prompt']
    prompt_file = ROOT / f'management/agentic/prompts/turn.002/{aid}.txt'
    dispatch_rows.append({
        **invocation, 'dependencies': dependencies,
        'authorization_sequence': authorization_event['sequence'],
        'invocation_sequence': invocation_event['sequence'],
        'handoff_sequence': handoff_event['sequence'] if handoff_event else None,
        'handoff_status': turn['handoffs'].get(aid, {}).get('handoff_status'),
        'authorization_precedes_recorded_invocation': authorization_event['sequence'] < invocation_event['sequence'],
        'dependency_handoffs_precede_authorization': all(s < authorization_event['sequence'] for s in dependency_events.values()),
        'exact_prompt_hash_matches': sha256(prompt.encode()).hexdigest() == invocation['prompt_hash'],
        'saved_prompt_matches': prompt_file.read_text() == prompt,
        'authorization_ticket_matches': authorization_event['data']['authorization_id'] == invocation['authorization_id'],
    })

handoff_rows = []
for aid, handoff in turn['handoffs'].items():
    owner = workflow.path(turn['invocations'][aid]['owned_directory'])
    handoff_rows.append({
        'agent_id': aid, 'task_id': handoff['task_id'],
        'status': handoff['handoff_status'], 'artifacts': handoff['artifacts'],
        'evidence_count': len(handoff['evidence']), 'test_claims': handoff['tests'],
        'all_artifacts_within_owned_directory': all(workflow.path(ref['path']).is_relative_to(owner)
                                                   for ref in handoff['artifacts']),
        'limitations': handoff['limitations'],
    })

current_head = events[-1]['hash']
wiki_start = turn['invocations']['WIK-30']
wiki_sequence = next(e['sequence'] for e in events if e['kind'] == 'INVOKED'
                     and e['data']['agent_id'] == 'WIK-30')
execution = package['reviewed_agents']
post_wiki_execution_invocations = [e['data']['agent_id'] for e in events
    if e['kind'] == 'INVOKED' and e['sequence'] > wiki_sequence
    and e['data']['agent_id'] in execution]
issues = [ref for ref in refs if ref['status'] != 'PASS']
historical_context_differences = [ref for ref in issues if '/context/sources/' in ref['origin']]
current_reference_issues = [ref for ref in issues if ref not in historical_context_differences]
result = {
    'turn_id': 'turn.002', 'recorded_at': datetime.now(timezone.utc).isoformat(),
    'phase': turn['state'], 'collection_status': 'COMPLETE_INVENTORY_NOT_ACCEPTANCE',
    'package': {'path': str((owned/'evidence-package.json').relative_to(ROOT)),
                'sha256': hashed(owned/'evidence-package.json'),
                'indexed_references': len(package['artifact_inventory']),
                'collected_head_matches': current_head == package['collected_head_hash'],
                'missing_invocations': package['missing_invocations'],
                'missing_handoffs': package['missing_handoffs'],
                'failed_invocations': package['failed_invocations'],
                'integrity_issues': package['integrity_issues']},
    'registry_revision': turn['registry']['revision'], 'registry_count': len(registry_ids),
    'selection_exact_registry_coverage': set(selection) == set(registry_ids),
    'selected_roles': selected, 'excluded_roles': {aid: selection[aid]['reason'] for aid in excluded},
    'unique_exclusion_reasons': len({selection[a]['reason'] for a in excluded}) == len(excluded),
    'selected_role_assignments': {aid: selection[aid] for aid in selected},
    'dispatch_inventory': dispatch_rows,
    'distinct_recorded_receipts': len({i['receipt'] for i in turn['invocations'].values()}) == len(turn['invocations']),
    'handoff_inventory': handoff_rows,
    'post_wiki_execution_invocations': post_wiki_execution_invocations,
    'reference_checks': refs,
    'reference_status_counts': dict(Counter(ref['status'] for ref in refs)),
    'missing_or_changed_references': issues,
    'historical_reading_context_differences': historical_context_differences,
    'historical_context_interpretation': 'These hashes attest the documented 10:29:49 pre-edit reading snapshot; they are retained and differ from the later WIK source. IA must assess this meaning; Collector does not rewrite context history.',
    'current_reference_integrity_issues': current_reference_issues,
    'supplemental_observed_files': [{'path':str(p.relative_to(ROOT)), 'sha256':hashed(p)}
                                    for p in sorted(supplemental)],
    'selected_role_owned_file_inventory': {aid:[
        {'path':str(p.relative_to(ROOT)), 'sha256':hashed(p),
         'indexed_in_runtime_package':str(p.relative_to(ROOT)) in {r['path'] for r in package['artifact_inventory']}}
        for p in sorted(workflow.path(turn['invocations'][aid]['owned_directory']).rglob('*')) if p.is_file()]
        for aid in package['reviewed_agents']},
    'historical_turn': {'turn_id':'turn.001', 'state':previous['state'],
                        'failures':previous['failures'], 'events':len(previous['events']),
                        'accepted_publication_present':workflow.path('wiki/turn.001.json').exists()},
    'preflight': workflow.preflight_status(),
    'pending_terminal_roles_expected_at_collection': ['IA-11', 'DOC-10'],
    'known_gaps': [
        'Observed outer agents wrapper raises ModuleNotFoundError: game_workflow. Preserve for IA; SWE correction requires new authorized turn.',
        'Live Codex host workflow tool call not observed: CONFIGURED_UNVERIFIED; actual local protocol probe does not establish host connection.',
        'Current host list corroborates QA, WIK, EVC and root only; earlier ENG/KRA/SWE receipts remain recorded externally reviewable assertions.',
        'Earlier quota failures are parent-recorded; no raw quota-service response is attached. Turn.001 is interrupted and unaccepted.',
        'First WIK handoff and collection-phase RPC attempts failed with generic TaskGroup errors; exact envelope cause is parent explanation. Corrected v2 records and adverse logs remain.',
        'IA acceptance, DOC publication, exact import acceptance and task closure are intentionally still pending.',
    ],
    'upstream_hash_interpretation': 'UPSTREAM source_hashes_before_game_adapter_patches are historical provenance, not current-file hash assertions.',
    'limits': 'Read-only file/log and recorded-order inventory. Does not authenticate past host workers, accept the turn, rerun gameplay, or repair source/wiki.',
}
(owned/'inventory.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({
    'status':result['collection_status'], 'phase':result['phase'],
    'registry_count':len(registry_ids), 'selected':len(selected), 'excluded':len(excluded),
    'runtime_indexed_references':len(package['artifact_inventory']),
    'reference_status_counts':result['reference_status_counts'],
    'supplemental_observed_files':len(supplemental),
    'missing_or_changed_references':issues,
    'historical_reading_context_differences':len(historical_context_differences),
    'current_reference_integrity_issues':current_reference_issues,
    'package_head_matches':result['package']['collected_head_matches'],
    'post_wiki_execution_invocations':post_wiki_execution_invocations,
},indent=2))
assert set(selection) == set(registry_ids)
assert not current_reference_issues
assert result['package']['collected_head_matches']
assert not post_wiki_execution_invocations
assert all(all(row[k] for k in ('authorization_precedes_recorded_invocation',
    'dependency_handoffs_precede_authorization','exact_prompt_hash_matches',
    'saved_prompt_matches','authorization_ticket_matches')) for row in dispatch_rows)
assert all(row['all_artifacts_within_owned_directory'] for row in handoff_rows)
