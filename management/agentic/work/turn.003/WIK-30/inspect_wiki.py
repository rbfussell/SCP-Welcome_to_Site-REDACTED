from pathlib import Path
from html.parser import HTMLParser
import json,hashlib,re
root=Path.cwd(); wiki=root/'source_material/game-wiki'; own=root/'management/agentic/work/turn.003/WIK-30'
class Page(HTMLParser):
 def __init__(self,path):
  super().__init__();self.links=set();self.chunks=[];self.in_article=False;self.feed(path.read_text())
 def handle_starttag(self,tag,attrs):
  if tag=='article':self.in_article=True
  if tag=='a' and self.in_article:
   value=dict(attrs).get('href',''); self.links.add(value.split('#')[0])
 def handle_endtag(self,tag):
  if tag=='article':self.in_article=False
 def handle_data(self,data):
  if self.in_article:self.chunks.append(data)
 @property
 def text(self):return ' '.join(self.chunks)
names=['index.html','agentic-loop.html','project-management.html','development.html','status.html','engine.html','doc-docs-59-agentic-development-loop-md.html','doc-management-readme-md.html','doc-docs-12-development-workflow-md.html','evidence-tools-agentic-py.html','evidence-management-harness-manage-game-py.html','evidence-tools-game-workflow-py.html','evidence-tests-test-agentic-entrypoints-py.html']
pages={n:Page(wiki/n) for n in names}
checks=[]
def check(label,result):
 assert result,label
 checks.append(label)
for name in ('project-management.html','development.html','status.html','engine.html'):
 check('reciprocal: agentic-loop <-> '+name, name in pages['agentic-loop.html'].links and 'agentic-loop.html' in pages[name].links)
check('discoverable from home','agentic-loop.html' in pages['index.html'].links)
for name in ('evidence-tools-agentic-py.html','evidence-management-harness-manage-game-py.html','evidence-tools-game-workflow-py.html','evidence-tests-test-agentic-entrypoints-py.html'):
 check('loop links actual source evidence: '+name,name in pages['agentic-loop.html'].links)
text=pages['agentic-loop.html'].text
for phrase in ('INTERRUPTED','REJECTED','IA-002-001','53 distinct','16:42:06 UTC','NOT_OBSERVED','synthetic','hot-reloaded','pending at this WIK handoff'):
 check('loop semantic statement: '+phrase,phrase in text)
manifest=json.loads((wiki/'manifest.json').read_text())
for name in ('tests/test_agentic_entrypoints.py','management/harness/manage_game.py','tools/agentic.py'):
 check('static evidence hash: '+name,manifest['evidence_hashes'][name]==hashlib.sha256((root/name).read_bytes()).hexdigest())
qa=json.loads((root/'management/agentic/work/turn.003/QA-16/source-after.json').read_text())
for entry in qa:
 check('execution source unchanged since independent QA: '+entry['path'],hashlib.sha256((root/entry['path']).read_bytes()).hexdigest()==entry['sha256'])
task=json.loads((root/'management/tasks/agentic-loop-setup.json').read_text())
for label in ('tests','docs','wiki-build','wiki-check'):
 entry=task['checks'][label]
 check('recorded '+label+' PASS/stable',entry['result']=='PASS' and entry['exit_code']==0 and not entry['inputs_changed_during_check'])
 check('recorded '+label+' all current declared inputs agree',all(hashlib.sha256((root/name).read_bytes()).hexdigest()==value for name,value in entry['inputs'].items()))
 check('recorded '+label+' log intact',hashlib.sha256((root/entry['log']).read_bytes()).hexdigest()==entry['sha256'])
result={'status':'PASS','checks':checks,'pages_inspected':names,'generated_page_count':len(manifest['generated_pages']),'semantic_review':'Compared final authored/generated wording with shared failed dispatch, unchanged runtime validation/MCP dictionary facade, isolated real-MCP fixture tests and independent QA logs, host observation and actual interrupted/rejected history. Reciprocal pages preserve current native-game authority and M0 open. Hash/link checks supplement this manual comparison; they do not establish acceptance, worker authenticity or resident host hot reload.'}
(own/'semantic-inspection.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
