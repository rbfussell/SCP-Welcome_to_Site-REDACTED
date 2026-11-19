"""Read-only evidence checks; writes only EVC-owned inventory and semantic observations."""
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
from workflow import digest

workflow = GameWorkflow(ROOT)
owned = ROOT / 'management/agentic/work/turn.003/EVC-28'
turn = workflow.turn('turn.003')
package = json.loads((owned / 'evidence-package.json').read_text())
task = json.loads((ROOT / 'management/tasks/agentic-loop-setup.json').read_text())
hex_hash = re.compile(r'[0-9a-f]{64}')
rows = []


def hashed(path):
    return sha256(path.read_bytes()).hexdigest()


def read(name):
    return json.loads((ROOT / name).read_text())


def write(name, value):
    (owned / name).write_text(json.dumps(value, indent=2, ensure_ascii=False) + '\n')


def ref(name, expected, origin, classification='CURRENT'):
    # The installed convenience wrapper is outside the game root, read-only here.
    path = (ROOT / name).resolve()
    actual = hashed(path) if path.is_file() else None
    row = {'path': name, 'sha256': expected, 'actual_sha256': actual,
           'origin': origin, 'classification': classification,
           'status': 'PASS' if actual == expected else 'MISSING' if actual is None else 'CHANGED'}
    rows.append(row)
    return row


def visit(value, origin, classification='CURRENT'):
    if isinstance(value, dict):
        if isinstance(value.get('sha256'), str) and hex_hash.fullmatch(value['sha256']):
            name = value.get('path') or value.get('log')
            if isinstance(name, str):
                ref(name, value['sha256'], origin, classification)
        for key, child in value.items():
            if key == 'inputs' and isinstance(child, dict):
                for name, expected in child.items():
                    if isinstance(expected, str) and hex_hash.fullmatch(expected):
                        ref(name, expected, origin + '/inputs', classification)
            else:
                visit(child, origin + '/' + key, classification)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            visit(child, origin + '/' + str(index), classification)


for value in package['artifact_inventory']:
    ref(value['path'], value['sha256'], 'genuine-runtime-package')
for aid, handoff in turn['handoffs'].items():
    visit(handoff, 'turn.003-handoff/' + aid)
for label, check in task['checks'].items():
    visit(check, 'final-task-check/' + label)
visit(task['context'], 'pre-edit-task-reading-attestation', 'HISTORICAL_READING_CONTEXT')
for name in ['QA-16/source-before.json', 'QA-16/source-after.json', 'SWE-13/tested-source-hashes.json']:
    visit(read('management/agentic/work/turn.003/' + name), name)
for name, classification in [('before-source-hashes.json', 'HISTORICAL_WIK_PRE_EDIT'),
                             ('final-source-hashes.json', 'CURRENT')]:
    for target, expected in read('management/agentic/work/turn.003/WIK-30/' + name).items():
        ref(target, expected, 'WIK-30/' + name, classification)

snapshots = []
for item in read('management/agentic/work/turn.003/SWE-13/before-source-manifest.json'):
    snapshot = ref(item['copy'], item['sha256'], 'preserved-before-manifest')
    live = ref(item['source'], item['sha256'], 'preserved-before-manifest/source', 'HISTORICAL_PRE_FIX')
    snapshots.append({'source': item['source'], 'copy': item['copy'], 'snapshot_status': snapshot['status'],
                      'source_now': live['status'], 'original_sha256': item['sha256']})

historical = []
for tid in ['turn.001', 'turn.002']:
    old = workflow.turn(tid)  # Validates the real event chain and any publication.
    for value in workflow.evidence_inventory(old):
        name = value['path']
        classification = 'HISTORICAL_IMMUTABLE_RECORD' if name.startswith('management/agentic/') else 'HISTORICAL_LIVE_SOURCE'
        ref(name, value['sha256'], tid + '-indexed-reference', classification)
    historical.append({'turn_id': tid, 'state': old['state'], 'event_count': len(old['events']),
                       'failures': old['failures'], 'audit': old['audit'],
                       'has_doc_handoff': 'DOC-10' in old['handoffs'],
                       'publication_exists': workflow.path('wiki/' + tid + '.json').exists(),
                       'head': old['events'][-1]['hash']})

import_record = read('management/agentic/imported-project.json')
visit(import_record, 'existing-project-import')
manifest = read('source_material/game-wiki/manifest.json')
for field in ['source_hashes', 'evidence_hashes', 'binary_hashes']:
    for name, expected in manifest[field].items():
        ref(name, expected, 'final-wiki-manifest/' + field)

registry = [a['agent_id'] for a in turn['registry']['agents']]
selected = [aid for aid, row in turn['selection'].items() if row['selected']]
excluded = {aid: row['reason'] for aid, row in turn['selection'].items() if not row['selected']}
events = turn['events']
dispatch = []
for aid, inv in turn['invocations'].items():
    auth = next(e for e in events if e['kind'] == 'DISPATCH_AUTHORIZED' and e['data']['agent_id'] == aid)
    call = next(e for e in events if e['kind'] == 'INVOKED' and e['data']['agent_id'] == aid)
    hand = next((e for e in events if e['kind'] == 'HANDOFF' and e['data']['agent_id'] == aid), None)
    deps = turn['selection'][aid]['dependencies']
    dependency_order = {d: next(e['sequence'] for e in events if e['kind'] == 'HANDOFF' and e['data']['agent_id'] == d) for d in deps}
    prompt = turn['selection'][aid]['prompt']
    dispatch.append({**inv, 'authorization_sequence': auth['sequence'], 'invocation_sequence': call['sequence'],
                     'handoff_sequence': hand['sequence'] if hand else None, 'dependencies': dependency_order,
                     'authorization_precedes_recorded_invocation': auth['sequence'] < call['sequence'],
                     'dependency_handoffs_precede_authorization': all(n < auth['sequence'] for n in dependency_order.values()),
                     'ticket_matches': auth['data']['authorization_id'] == inv['authorization_id'],
                     'prompt_hash_matches': sha256(prompt.encode()).hexdigest() == inv['prompt_hash'],
                     'saved_exact_prompt_matches': (ROOT / f'management/agentic/prompts/turn.003/{aid}.txt').read_text() == prompt})
wiki_start = next(e['sequence'] for e in events if e['kind'] == 'INVOKED' and e['data']['agent_id'] == 'WIK-30')
post_wiki = [e['data']['agent_id'] for e in events if e['kind'] == 'INVOKED' and e['sequence'] > wiki_start and e['data']['agent_id'] in package['reviewed_agents']]
handoffs = []
for aid, hand in turn['handoffs'].items():
    owner = workflow.path(turn['invocations'][aid]['owned_directory'])
    handoffs.append({'agent_id': aid, 'task_id': hand['task_id'], 'handoff_status': hand['handoff_status'],
                     'artifact_count': len(hand['artifacts']),
                     'all_artifacts_within_owned_directory': all(workflow.path(x['path']).is_relative_to(owner) for x in hand['artifacts']),
                     'test_claims': hand['tests'], 'limitations': hand['limitations']})

mcp = read('management/agentic/work/turn.003/QA-16/mcp-fixture-transcript.json')
mcp_calls = [{'label': c['label'], 'is_error': c['response']['isError'],
              'acknowledgement': c['response'].get('structuredContent'),
              'failed_event_count': sum(e['kind'] == 'FAILED' for e in c['events'])} for c in mcp['calls']]

llms = ROOT / 'source_material/game-wiki/llms.txt'
stale = [{'line': n, 'text': line} for n, line in enumerate(llms.read_text().splitlines(), 1)
         if 'current outer agents shortcut has an import-path defect' in line]
old_guide = ROOT / 'docs/58-software-project-management.md'
guide_stale = [{'line': n, 'text': line} for n, line in enumerate(old_guide.read_text().splitlines(), 1)
               if 'Current setup limitations' in line and 'unobserved' in line]
semantic = {'status': 'OBSERVED_CURRENT_PROSE_CONFLICT_FOR_IA',
            'path': str(llms.relative_to(ROOT)), 'sha256': hashed(llms), 'observations': stale,
            'additional_current_pointer_conflict': {'path': str(old_guide.relative_to(ROOT)),
                'sha256': hashed(old_guide), 'observations': guide_stale},
            'conflicting_evidence': ['management/agentic/work/turn.003/QA-16/report.md',
                                    'management/agentic/bootstrap/host-connection-observed.json',
                                    'source_material/game-wiki/content/agentic-loop.md',
                                    'management/harness/manage_game.py'],
            'meaning': 'The active agent reading entry point and the newer superseding paragraph of docs58 still assert current wrapper import defect and unobserved host connection, contradicting corrected source, independent QA and the real observed host call. The docs58 sentence is a current pointer rather than preserved adapter history. No repair, severity or acceptance decision by Collector.'}
write('semantic-observation.json', semantic)

files = set()
for directory in ['management/agentic/work/turn.003', 'management/agentic/bootstrap',
                  'management/agentic/rpc/turn.003', 'management/agentic/prompts/turn.003',
                  'management/evidence/agentic-loop-setup']:
    files.update(p for p in (ROOT / directory).rglob('*') if p.is_file() and not p.is_relative_to(owned))
for tid in ['turn.001', 'turn.002', 'turn.003']:
    files.add(ROOT / f'management/agentic/evidence/{tid}.json')
    files.update((ROOT / f'management/agentic/evidence/{tid}.events').glob('*.json'))
for name in ['management/tasks/agentic-loop-setup.json', 'management/contexts/agentic-loop-setup.json',
             'management/agentic/imported-project.json', 'source_material/game-wiki/manifest.json']:
    files.add(ROOT / name)
supplemental = [{'path': str(p.relative_to(ROOT)), 'sha256': hashed(p)} for p in sorted(files)]

historical_diffs = [r for r in rows if r['status'] != 'PASS' and r['classification'].startswith('HISTORICAL_') and r['classification'] != 'HISTORICAL_IMMUTABLE_RECORD']
current_issues = [r for r in rows if r['status'] != 'PASS' and r not in historical_diffs]
checks = {
    'package_has_no_missing_invocations': not package['missing_invocations'],
    'package_has_no_missing_handoffs': not package['missing_handoffs'],
    'package_has_no_failed_invocations': not package['failed_invocations'],
    'package_has_no_integrity_errors': not package['integrity_issues'],
    'package_matches_current_index': package['artifact_inventory'] == workflow.evidence_inventory(turn),
    'collected_head_matches': package['collected_head_hash'] == events[-1]['hash'],
    'selection_hash_matches': package['selection_hash'] == digest(turn['selection']),
    'registry_exactly_accounted_once': len(registry) == len(set(registry)) and set(registry) == set(turn['selection']),
    'all_exclusions_unique_substantive': len(set(excluded.values())) == len(excluded) and all(len(r) >= 12 for r in excluded.values()),
    'mandatory_roles_selected': {'ENG-00', 'WIK-30', 'EVC-28', 'IA-11', 'DOC-10'} <= set(selected),
    'distinct_recorded_receipts': len({i['receipt'] for i in turn['invocations'].values()}) == len(turn['invocations']),
    'dispatch_prompts_tickets_order': all(all(d[k] for k in ['authorization_precedes_recorded_invocation', 'dependency_handoffs_precede_authorization', 'ticket_matches', 'prompt_hash_matches', 'saved_exact_prompt_matches']) for d in dispatch),
    'all_current_handoffs_match_task_and_ownership': all(h['task_id'] == task['id'] and h['handoff_status'] == 'COMPLETE' and h['all_artifacts_within_owned_directory'] for h in handoffs),
    'no_execution_dispatch_after_wiki': not post_wiki,
    'no_current_or_immutable_reference_issues': not current_issues,
    'task_final_checks_pass': all(c['result'] == 'PASS' and c['exit_code'] == 0 and set(c['inputs']) == set(task['inputs']) for c in task['checks'].values()),
    'prior_turns_remain_interrupted_without_doc_or_publication': all(h['state'] == 'INTERRUPTED' and not h['has_doc_handoff'] and not h['publication_exists'] for h in historical),
    'import_self_hash_matches': import_record['hash'] == digest({k:v for k,v in import_record.items() if k != 'hash'}),
}
inventory = {'turn_id': 'turn.003', 'recorded_at': datetime.now(timezone.utc).isoformat(),
             'status': 'COMPLETE_INVENTORY_NOT_ACCEPTANCE', 'verification_checks': checks,
             'package': {'path': str((owned / 'evidence-package.json').relative_to(ROOT)),
                         'sha256': hashed(owned / 'evidence-package.json'), 'indexed_reference_count': len(package['artifact_inventory']),
                         'reviewed_agents': package['reviewed_agents'], 'integrity_issues': package['integrity_issues']},
             'registry_revision': turn['registry']['revision'], 'registry_count': len(registry),
             'selected_assignments': {a:turn['selection'][a] for a in selected}, 'individual_exclusions': excluded,
             'dispatch_inventory': dispatch, 'handoff_inventory': handoffs, 'post_wiki_execution_dispatches': post_wiki,
             'reference_checks': rows, 'reference_status_counts': dict(Counter(r['status'] for r in rows)),
             'current_or_immutable_reference_issues': current_issues, 'historical_source_differences': historical_diffs,
             'historical_difference_meaning': 'Reading attestations, WIK pre-edit maps and prior live source hashes describe earlier bytes. Immutable records and preserved SWE before copies are verified separately; no historical source expectation is rewritten.',
             'preserved_before_snapshots': snapshots, 'historical_turns': historical,
             'import_hash': import_record['hash'], 'semantic_observation': semantic,
             'real_mcp_fixture': {'scope': mcp['scope'], 'protocol': mcp['protocol_version'],
                                  'receipts_are_production_evidence': mcp['receipts_are_production_evidence'], 'calls': mcp_calls},
             'task_final_check_labels': list(task['checks']), 'declared_input_count': len(task['inputs']),
             'supplemental_file_inventory': supplemental,
             'limits': ['Receipt/order records do not authenticate host identity or exact spawn time; partial live host inventory is separately recorded.',
                        'Collector does not repair source/wiki/task/process history, decide acceptance, rerun native gameplay or claim resident MCP hot reload.',
                        'Unresolved IA-002-001 remains confirmed until independent IA/DOC disposition; import/task acceptance remains pending.',
                        'An exploratory schema-summary command initially assumed supplemental-check-results was a dict and raised AttributeError; this was an inspection helper error, not a product test.']}
write('inventory.json', inventory)
for name, value in checks.items():
    print(('PASS' if value else 'FAIL') + ' ' + name)
print('Runtime indexed references:', len(package['artifact_inventory']))
print('Nested/reference checks:', len(rows), dict(Counter(r['status'] for r in rows)))
print('Historical source/context differences retained:', len(historical_diffs))
print('Current/immutable hash issues:', len(current_issues))
print('Semantic prose conflicts recorded for independent IA:', len(stale) + len(guide_stale))
print('Supplemental files inventoried:', len(supplemental))
sys.exit(0 if all(checks.values()) else 1)
