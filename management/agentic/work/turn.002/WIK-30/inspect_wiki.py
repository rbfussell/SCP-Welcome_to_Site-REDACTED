"""Inspect authored/generated loop claims and reciprocal links; not gameplay QA."""
from html.parser import HTMLParser
import json
from pathlib import Path
root = Path.cwd()
wiki = root/'source_material/game-wiki'
class Page(HTMLParser):
    def __init__(self, path):
        super().__init__(); self.links=[]; self.text=[]; self.feed(path.read_text())
    def handle_starttag(self, tag, attrs):
        data=dict(attrs)
        if tag=='a': self.links.append(data.get('href',''))
    def handle_data(self,data): self.text.append(data)
relations = ['project-management','development','status','engine']
loop = Page(wiki/'agentic-loop.html')
for name in relations:
    assert f'{name}.html' in loop.links, f'Loop misses return to {name}'
    assert 'agentic-loop.html' in Page(wiki/f'{name}.html').links, f'{name} misses loop'
assert 'agentic-loop.html' in Page(wiki/'index.html').links
assert 'doc-docs-59-agentic-development-loop-md.html' in loop.links
text=' '.join(loop.text)
for phrase in ['CONFIGURED_UNVERIFIED','ModuleNotFoundError: game_workflow','23 roles','registered role','Turn.001','WIK-30','IA-11','DOC-10']:
    assert phrase.casefold() in text.casefold(), f'Missing truthful loop claim: {phrase}'
roles=json.loads((root/'management/harness/runtime/config/game_roles.json').read_text())['agents']
contract=(root/'docs/59-agentic-development-loop.md').read_text()
for role in roles: assert role['agent_id'] in contract, f'Undocumented role {role["agent_id"]}'
manifest=json.loads((wiki/'manifest.json').read_text())
for path in ['tools/game_workflow.py','tools/agentic.py','management/profile.json','management/harness/runtime/config/game_roles.json','docs/59-agentic-development-loop.md','source_material/game-wiki/content/agentic-loop.md','AGENTS.md']:
    assert path in manifest['evidence_hashes'], f'Missing static source freshness coverage: {path}'
assert not any(p.startswith('management/agentic/') for p in manifest['evidence_hashes']), 'Evolving process records must not create circular wiki hashes'
print(f'WIKI_SEMANTIC_INSPECTION_PASS: {len(roles)} scoped roles, new page/navigation, reciprocal system/workflow links, current defect/host limits, static evidence coverage; manual descriptions compared with actual code/reports. No process acceptance/gameplay certification.')
