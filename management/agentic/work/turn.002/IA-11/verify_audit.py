"""Read-only independent checks; writes only the IA-owned JSON result."""
from pathlib import Path
import hashlib
import json
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[5]
sys.path.insert(0, str(ROOT / 'tools'))
from game_workflow import GameWorkflow

OUT = Path(__file__).parent
def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

w = GameWorkflow(ROOT)
t = w.turn('turn.002')
old = w.turn('turn.001')
package_path = ROOT / t['handoffs']['EVC-28']['collection']['package_path']
package = json.loads(package_path.read_text())
selection = t['selection']
ids = {a['agent_id'] for a in t['registry']['agents']}
assert set(selection) == ids
excluded = {k: v['reason'] for k, v in selection.items() if not v['selected']}
assert len(excluded) == len(set(excluded.values()))
assert len(ids) == 23 and len(excluded) == 15
current_refs = []
def refs(value, origin):
    if isinstance(value, dict):
        if isinstance(value.get('path'), str) and isinstance(value.get('sha256'), str):
            p = w.path(value['path'])
            actual = sha(p) if p.is_file() else None
            current_refs.append({'path':value['path'], 'expected':value['sha256'], 'actual':actual,
                                 'origin':origin, 'matches':actual == value['sha256']})
        for k, item in value.items():
            refs(item, origin + '/' + str(k))
    elif isinstance(value, list):
        for k, item in enumerate(value):
            refs(item, origin + '/' + str(k))

refs(package, 'collector/package')
refs(t['handoffs'], 'current/handoffs')
qa = json.loads((ROOT / 'management/agentic/work/turn.002/QA-16/tested-source-snapshot.json').read_text())
refs(qa, 'QA/final-source')
task = json.loads((ROOT/'management/tasks/agentic-loop-setup.json').read_text())
for label, check in task['checks'].items():
    assert check['result'] == 'PASS' and check['exit_code'] == 0
    assert sha(ROOT/check['log']) == check['sha256']
    assert len(check['inputs']) == 39
    for path, expected in check['inputs'].items():
        assert sha(ROOT/path) == expected, (label,path)

historical = []
for item in task['context']['sources']:
    current = sha(ROOT/item['path'])
    if current != item['sha256']:
        historical.append({'path':item['path'], 'old':item['sha256'], 'current':current,
                           'kind':item['kind'], 'recorded_at':task['context']['recorded_at']})
assert len(historical) == 6
assert all(i['kind'] in ('wiki_read','docs_read') for i in historical)
assert task['context']['recorded_at'] < t['timestamp']

dispatches=[]
for aid, inv in t['invocations'].items():
    authorization=next(e for e in t['events'] if e['kind']=='DISPATCH_AUTHORIZED' and e['data']['agent_id']==aid)
    invocation=next(e for e in t['events'] if e['kind']=='INVOKED' and e['data']['agent_id']==aid)
    assert authorization['sequence'] < invocation['sequence']
    assert authorization['data']['authorization_id'] == inv['authorization_id']
    prompt = selection[aid]['prompt']
    assert hashlib.sha256(prompt.encode()).hexdigest() == inv['prompt_hash']
    assert (ROOT/f'management/agentic/prompts/turn.002/{aid}.txt').read_text() == prompt
    for dep in selection[aid].get('dependencies',[]):
        handoff=next(e for e in t['events'] if e['kind']=='HANDOFF' and e['data']['agent_id']==dep)
        assert handoff['sequence'] < authorization['sequence']
    dispatches.append({'role':aid,'authorization_sequence':authorization['sequence'],
                       'invoke_sequence':invocation['sequence'],'receipt':inv['receipt']})
assert len({i['receipt'] for i in t['invocations'].values()}) == len(t['invocations'])
wiki_event=next(e['sequence'] for e in t['events'] if e['kind']=='INVOKED' and e['data']['agent_id']=='WIK-30')
execution={a['agent_id'] for a in t['registry']['agents'] if a['phase'] not in ('collection','audit','documentation') and a['agent_id']!='WIK-30'}
assert not [e for e in t['events'] if e['kind']=='INVOKED' and e['sequence']>wiki_event and e['data']['agent_id'] in execution]
collected_event=next(e for e in t['events'] if e['hash']==package['collected_head_hash'])
assert collected_event['sequence']==22
assert not package['integrity_issues'] and not package['missing_invocations'] and not package['missing_handoffs']

record=w.import_record()
refs(record,'import/exact-record')
baseline=record['baseline_commit']
historical_git=[]
for item in record['historical_sources']:
    filename=Path(item['path']).name
    # The directory README is an authored provenance note, not the game's README.
    # The four delimiter-named files are the actual commit snapshots.
    if '--' not in filename:
        historical_git.append({'path':item['path'],'kind':'local provenance note; hashed separately'})
        continue
    original=filename.replace('--','/')
    result=subprocess.run(['git','show',f'{baseline}:{original}'],cwd=ROOT,capture_output=True,check=True)
    assert hashlib.sha256(result.stdout).hexdigest()==item['sha256'],original
    historical_git.append({'path':original,'matches_commit':baseline})
assert old['state']=='INTERRUPTED' and set(old['failures'])=={'SWE-13','QA-16'}
assert not w.path('wiki/turn.001.json').exists()
preflight=w.preflight_status()
assert not preflight['imported_history_accepted'] and not preflight['production_allowed']
assert not preflight['question_round_complete'] and not preflight['initial_research_complete'] and not preflight['baseline_complete']
bad = [r for r in current_refs if not r['matches']]
assert not bad,bad
changed=subprocess.run(['git','diff','--name-only','HEAD'],cwd=ROOT,text=True,capture_output=True,check=True).stdout.splitlines()
assert not any(p.startswith(('src/','game/','source_material/scp-api/','.github/')) for p in changed)
result={'status':'PASS','turn_id':t['turn_id'],'phase':t['state'],
        'registry_count':len(ids),'selected_count':len(ids)-len(excluded),'excluded':excluded,
        'dispatch_order_checks':dispatches,'collected_sequence':collected_event['sequence'],
        'current_hash_assertions':len(current_refs),'current_reference_mismatches':bad,
        'reference_checks':current_refs,'historical_reading_attestation_differences':historical,
        'managed_checks':list(task['checks']),'unchanged_declared_inputs':39,
        'git_import_comparisons':historical_git,'exact_import_hash':record['hash'],
        'prior_turn':{'state':old['state'],'failures':old['failures']},'preflight':preflight,
        'tracked_changed_paths_count':len(changed),'native_source_asset_canon_ci_tracked_changes':[],
        'limitations':['This verifies actual records, hashes and dependency order; external host spawn authenticity is not cryptographically established.',
                      'No live Codex workflow tool has been observed; actual local MCP protocol results remain separate.',
                      'Focused read-only verification does not qualify gameplay or override the independently reproduced wrapper failure.']}
(OUT/'independent-verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('reference_checks','excluded','preflight')},indent=2))
