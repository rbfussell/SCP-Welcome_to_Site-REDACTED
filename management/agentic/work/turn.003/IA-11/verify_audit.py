"""Independent read-only turn.003 audit; writes only this role's evidence."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time

ROOT = Path(__file__).resolve().parents[5]
OWN = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tools'))
from game_workflow import GameWorkflow

def read(path):
    return json.loads((ROOT / path).read_text())

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def save(name, value):
    (OWN / name).write_text(json.dumps(value, indent=2) + '\n')

def run(label, argv, cwd=ROOT):
    started = time.monotonic()
    result = subprocess.run(list(map(str, argv)), cwd=cwd, capture_output=True,
                            text=True, timeout=60)
    (OWN / (label + '.log')).write_text(result.stdout + result.stderr)
    return dict(label=label, argv=list(map(str, argv)), cwd=str(cwd),
                exit_code=result.returncode, seconds=round(time.monotonic()-started, 3))

w = GameWorkflow(ROOT)
t = w.turn('turn.003')  # Runtime independently verifies the actual event chain.
task = read('management/tasks/agentic-loop-setup.json')
package = read('management/agentic/work/turn.003/EVC-28/evidence-package.json')
checks = []
def check_ref(ref, origin):
    path = ROOT / ref['path']
    match = path.is_file() and sha(path) == ref['sha256']
    checks.append(dict(origin=origin, path=ref['path'], matches=match))
    assert match, (origin, ref['path'])

for ref in package['artifact_inventory']:
    check_ref(ref, 'collector-index')
for aid, handoff in t['handoffs'].items():
    for group in ('artifacts', 'evidence'):
        for ref in handoff.get(group, []):
            check_ref(ref, aid + '.' + group)
    for test in handoff.get('tests', []):
        for ref in test.get('evidence', []):
            check_ref(ref, aid + '.tests')
for ref in read('management/agentic/work/turn.003/QA-16/source-after.json'):
    check_ref(ref, 'QA-final-source')
for label, entry in task['checks'].items():
    check_ref({'path':entry['log'], 'sha256':entry['sha256']}, 'task-' + label)
    assert entry['result'] == 'PASS' and entry['exit_code'] == 0
    assert not entry['inputs_changed_during_check']
    assert set(entry['inputs']) == set(task['inputs'])
    for path, expected in entry['inputs'].items():
        check_ref({'path':path, 'sha256':expected}, 'task-input-' + label)
assert task['checks']['wiki-check']['finished_at'] >= task['checks']['wiki-build']['finished_at']

roles = {a['agent_id']:a for a in t['registry']['agents']}
assert set(roles) == set(t['selection']) and len(roles) == 23
assert all(row['reason'].strip() for row in t['selection'].values())
no = {a:row['reason'] for a,row in t['selection'].items() if not row['selected']}
assert len(no) == 15 and len(set(no.values())) == 15
assert {'ENG-00','WIK-30','EVC-28','IA-11','DOC-10'} <= {a for a,r in t['selection'].items() if r['selected']}
assert len({v['receipt'] for v in t['invocations'].values()}) == len(t['invocations'])
assert t['invocations']['IA-11']['receipt'] == '/root/ia_correction'
chronology=[]
for aid, inv in t['invocations'].items():
    auth = next(e for e in t['events'] if e['kind']=='DISPATCH_AUTHORIZED' and e['data']['agent_id']==aid)
    invoked = next(e for e in t['events'] if e['kind']=='INVOKED' and e['data']['agent_id']==aid)
    assert auth['sequence'] < invoked['sequence']
    assert auth['data']['authorization_id'] == inv['authorization_id']
    prompt_hash = hashlib.sha256(t['selection'][aid]['prompt'].encode()).hexdigest()
    assert prompt_hash == auth['data']['prompt_hash'] == inv['prompt_hash']
    assert (ROOT/'management/agentic/prompts/turn.003'/ (aid+'.txt')).read_text() == t['selection'][aid]['prompt']
    for dep in t['selection'][aid].get('dependencies', []):
        assert any(e['kind']=='HANDOFF' and e['data']['agent_id']==dep and
                   e['sequence'] < auth['sequence'] for e in t['events'])
    if aid in t['handoffs']:
        assert t['handoffs'][aid]['prompt_hash']==prompt_hash
        assert t['handoffs'][aid]['task_id']==task['id']
        assert all(a['path'].startswith('management/agentic/work/turn.003/'+aid+'/')
                   for a in t['handoffs'][aid]['artifacts'])
    chronology.append(dict(agent_id=aid, authorization_sequence=auth['sequence'],
                           invocation_sequence=invoked['sequence'], receipt=inv['receipt']))
wiki_start=next(x['invocation_sequence'] for x in chronology if x['agent_id']=='WIK-30')
assert not any(x['invocation_sequence']>wiki_start for x in chronology
               if roles[x['agent_id']]['phase'] not in ('collection','audit','documentation')
               and x['agent_id']!='WIK-30')
assert package['collected_head_hash']==t['events'][21]['hash']
assert not package['integrity_issues'] and not package['missing_handoffs'] and not package['missing_invocations']

record=w.import_record()
historical=[]
for ref in record['historical_sources']:
    check_ref(ref,'immutable-import')
    name=Path(ref['path']).name
    if '--' in name:
        source=name.replace('--','/',1)
        git_bytes=subprocess.check_output(['git','show',record['baseline_commit']+':'+source],cwd=ROOT)
        assert hashlib.sha256(git_bytes).hexdigest()==ref['sha256']
        historical.append(source)
for ref in record['authorization_evidence']:
    check_ref(ref,'import-authorization')
assert record['hash']=='cf12434737bb7519349565abfe1d415aee97dda8cbf3fc71af22394b79b3a9b1'
prior={i:w.turn(i) for i in ('turn.001','turn.002')}
assert all(v['state']=='INTERRUPTED' for v in prior.values())
assert prior['turn.002']['audit']['disposition']=='REJECTED'
assert 'DOC-10' in prior['turn.002']['failures'] and 'DOC-10' not in prior['turn.002']['handoffs']
assert not (ROOT/'management/agentic/publications/turn.002.json').exists()
preflight=w.preflight_status()
assert not preflight['imported_history_accepted'] and not preflight['production_allowed']
assert not preflight['question_round_complete'] and not preflight['initial_research_complete'] and not preflight['baseline_complete']
assert sha(ROOT/'management/harness/manage_game.py') == sha(ROOT.parent/'scripts/manage_game.py')

semantic=[]
for path,line in [('source_material/game-wiki/llms.txt',31),('docs/58-software-project-management.md',69)]:
    p=ROOT/path
    semantic.append(dict(path=path,line=line,text=p.read_text().splitlines()[line-1],sha256=sha(p)))
assert 'import-path defect' in semantic[0]['text'] and 'CONFIGURED_UNVERIFIED' in semantic[0]['text']
assert 'import defect' in semantic[1]['text'] and 'unobserved live host' in semantic[1]['text']

baseline={p:sha(ROOT/p) for p in task['inputs']}
results=[]
if '--initial-execution' in sys.argv:
    results.append(run('focused-entrypoint-tests',[sys.executable,'-m','unittest','discover','-s','tests','-p','test_agentic_entrypoints.py','-v']))
    wrapper=ROOT.parent/'scripts/manage_game.py'
    results.append(run('outer-agents-root',['python3',wrapper,'agents','readiness'],ROOT.parent))
    results.append(run('outer-agents-unrelated',['python3',wrapper,'agents','readiness'],Path('/tmp')))
    results.append(run('outer-legacy-board',['python3',wrapper,'status'],ROOT.parent))
    results.append(run('direct-cli',[sys.executable,'tools/agentic.py','readiness']))
    results.append(run('wiki-freshness',['python3','tools/build_game_wiki.py','--check']))
else:
    # Retain the actual initial adverse run and separately qualified real IPC run.
    initial=(OWN/'focused-entrypoint-tests.log').read_text()
    final=(OWN/'focused-entrypoint-real-ipc.log').read_text()
    assert 'TimeoutError' in initial and 'FAILED (errors=1)' in initial
    assert 'Ran 5 tests' in final and '\nOK\n' in final
    transcript=json.loads(next(line.removeprefix('MCP_FIXTURE_EVIDENCE ') for line in final.splitlines() if line.startswith('MCP_FIXTURE_EVIDENCE ')))
    save('mcp-fixture-transcript.json',transcript)
    results.extend([dict(label='initial-default-sandbox-entrypoints',status='FAIL',limitation='MCP initialization IPC timeout; four other tests passed; original log preserved.'),
                    dict(label='approved-real-ipc-entrypoints',status='PASS',distinct_tests=5)])
    for label in ('outer-agents-root','outer-agents-unrelated','direct-cli'):
        assert json.loads((OWN/(label+'.log')).read_text())['status']=='READY'
        results.append(dict(label=label,status='PASS'))
    assert json.loads((OWN/'outer-legacy-board.log').read_text())['game_root']==str(ROOT)
    results.append(dict(label='outer-legacy-board',status='PASS'))
    assert 'Wiki validation passed: 2251 HTML pages' in (OWN/'wiki-freshness.log').read_text()
    results.append(dict(label='wiki-freshness',status='PASS'))
assert baseline=={p:sha(ROOT/p) for p in task['inputs']}
save('verification.json',dict(integrity='PASS',current_reference_comparisons=len(checks),
    references=checks,registry_revision=t['registry']['revision'],role_count=len(roles),
    selected_count=8,excluded=no,chronology=chronology,current_task_input_count=len(task['inputs']),
    collected_head_matches=True,immutable_git_snapshots=historical,preflight=preflight,
    source_inputs_stable_during_IA=True,checks=results,semantic_consistency='FAIL',
    semantic_conflicts=semantic,limits=['Record checks do not authenticate host receipt strings or exact spawn timestamps.',
        'Fresh MCP subprocess fixture is synthetic; resident host hot reload was not tested.']))
print(json.dumps(dict(integrity='PASS',references=len(checks),checks=results,semantic_consistency='FAIL'),indent=2))
