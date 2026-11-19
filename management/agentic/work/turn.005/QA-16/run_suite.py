import hashlib,json,pathlib,subprocess,sys,time
ROOT=pathlib.Path(__file__).resolve().parents[5]
OUT=pathlib.Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
paths=set(ROOT.glob('tools/*.py'))|set(ROOT.glob('tests/*.py'))
paths.discard(ROOT/'tools/build_game_wiki.py')
for directory in ['management/harness/runtime','management/model-policy']:
 paths.update(p for p in (ROOT/directory).rglob('*') if p.is_file() and p.suffix in ('.py','.json','.toml','.txt'))
paths.update([ROOT/'management/harness/manage_game.py',ROOT/'management/profile.json',ROOT/'management/harness/requirements.txt'] if (ROOT/'management/harness/requirements.txt').exists() else [ROOT/'management/harness/manage_game.py',ROOT/'management/profile.json'])
manifest={str(p.relative_to(ROOT)):sha(p) for p in sorted(paths)}
(OUT/'tested-inputs.json').write_text(json.dumps(manifest,indent=2)+'\n')
argv=[str(ROOT/'.venv-harness/bin/python'),'-m','unittest','discover','-s','tests','-p','test_*.py','-v']
start=time.time()
with (OUT/'full-suite.log').open('w') as log:
 try: result=subprocess.run(argv,cwd=ROOT,stdout=log,stderr=subprocess.STDOUT,timeout=180); code=result.returncode
 except subprocess.TimeoutExpired: code=124; log.write('\nQA HARNESS TIMEOUT 180 seconds\n')
changed=[p for p,h in manifest.items() if not (ROOT/p).is_file() or sha(ROOT/p)!=h]
result={'argv':argv,'cwd':str(ROOT),'exit_code':code,'elapsed_seconds':time.time()-start,'status':'PASS' if code==0 and not changed else 'FAIL','changed_inputs':changed,'log_sha256':sha(OUT/'full-suite.log'),'manifest_sha256':sha(OUT/'tested-inputs.json'),'real_local_mcp':True,'effective_model':'NOT_EXPOSED','effective_effort':'NOT_EXPOSED','usage_allowance':'UNAVAILABLE'}
(OUT/'suite-result.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2));sys.exit(0 if result['status']=='PASS' else 1)
