"""Read-only reuse gate for the one actual QA full suite, not another test run."""
import hashlib,json,pathlib,re
ROOT=pathlib.Path(__file__).resolve().parents[5]
OUT=pathlib.Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
r=json.loads((OUT/'suite-result.json').read_text())
m=json.loads((OUT/'tested-inputs.json').read_text())
assert r['status']=='PASS' and r['exit_code']==0 and not r['changed_inputs'], 'Suite did not pass'
assert sha(OUT/'tested-inputs.json')==r['manifest_sha256'], 'Manifest changed'
assert sha(OUT/'full-suite.log')==r['log_sha256'], 'Suite log changed'
changed=[p for p,h in m.items() if not (ROOT/p).is_file() or sha(ROOT/p)!=h]
assert not changed, f'Tested inputs changed: {changed}'
log=(OUT/'full-suite.log').read_text()
assert re.search(r'Ran 65 tests in [\d.]+s\s+OK',log), 'Expected complete 65-test pass absent'
assert 'MCP_FIXTURE_EVIDENCE ' in log, 'Real MCP fixture transcript absent'
print(json.dumps({'status':'PASS','mode':'UNCHANGED_TEST_EVIDENCE_REUSE','tests':65,'tested_input_count':len(m),'suite_result_sha256':sha(OUT/'suite-result.json'),'effective_settings':'NOT_EXPOSED','usage_allowance':'UNAVAILABLE'}))
