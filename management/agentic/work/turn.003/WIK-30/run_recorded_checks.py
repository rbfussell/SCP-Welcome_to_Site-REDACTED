from pathlib import Path
import subprocess, json, time, sys
root=Path.cwd(); own=root/'management/agentic/work/turn.003/WIK-30'
checks=[('tests',[str(root/'.venv-harness/bin/python'),'-m','unittest','discover','-s','tests','-p','test_*.py','-v']),('docs',['python3','tools/check_management_docs.py']),('wiki-build',['python3','tools/build_game_wiki.py']),('wiki-check',['python3','tools/build_game_wiki.py','--check'])]
results=[]
for label,command in checks:
 argv=['python3','tools/manage_project.py','record','agentic-loop-setup',label,'--',*command]
 started=time.monotonic()
 result=subprocess.run(argv,cwd=root,capture_output=True,text=True,timeout=180)
 (own/f'managed-{label}-result.json').write_text(result.stdout)
 (own/f'managed-{label}-stderr.log').write_text(result.stderr)
 try:
  data=json.loads(result.stdout)
  log=root/data['log']
  (own/f'{label}.log').write_bytes(log.read_bytes())
  passed=data['result']=='PASS'
 except (KeyError,ValueError,FileNotFoundError):
  data={}; passed=False
 results.append({'label':label,'argv':argv,'cwd':str(root),'returncode':result.returncode,'duration_seconds':round(time.monotonic()-started,3),'recorded_status':data.get('result'),'log':data.get('log'),'pass':passed})
 (own/'recorded-check-results.json').write_text(json.dumps(results,indent=2)+'\n')
 print(json.dumps(results[-1]),flush=True)
 if result.returncode or not passed:
  print(result.stderr,flush=True);sys.exit(result.returncode or 1)
