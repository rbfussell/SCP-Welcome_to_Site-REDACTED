import hashlib,json,pathlib,subprocess,tempfile
ROOT=pathlib.Path(__file__).resolve().parents[5]; OUT=pathlib.Path(__file__).resolve().parent
PYTHON=str(ROOT/'.venv-harness/bin/python'); records=[]
def run(args,cwd=ROOT,expected=0):
 r=subprocess.run([PYTHON,*map(str,args)],cwd=cwd,capture_output=True,text=True,timeout=30)
 records.append({'argv':[PYTHON,*map(str,args)],'cwd':str(cwd),'exit_code':r.returncode,'stdout':r.stdout,'stderr':r.stderr,'expected_exit_code':expected})
 assert r.returncode==expected, records[-1]
 return r
wrapper=ROOT.parent/'scripts/manage_game.py'
assert wrapper.read_bytes()==(ROOT/'management/harness/manage_game.py').read_bytes()
for cwd in [ROOT.parent,pathlib.Path('/tmp')]: run([wrapper,'agents','readiness'],cwd)
run([wrapper,'status'],ROOT.parent);run([ROOT/'tools/agentic.py','readiness'])
run([ROOT/'tools/configure_agent_models.py','--workspace',ROOT.parent,'--check'])
run([ROOT/'tools/agent_model_policy.py','--role','QA-16','--task','agentic-loop-setup'])
run([ROOT/'tools/agent_model_policy.py','--role','QA-16','--task','agentic-loop-setup','--effort','max'],expected=1)
with tempfile.TemporaryDirectory() as tmp:
 observed=pathlib.Path(tmp)/'synthetic-observed.json';observed.write_text('{"model":"gpt-6.1-sol"}')
 run([ROOT/'tools/agent_model_policy.py','--role','QA-16','--task','agentic-loop-setup','--receipt','SYNTHETIC_QA_NEGATIVE_ONLY','--observed',observed],expected=2)
(OUT/'interfaces.json').write_text(json.dumps({'status':'PASS','checks':records},indent=2)+'\n')
print('PASS: installed wrapper/board/direct CLI, read-only config check, resolver and negative CLI cases')
