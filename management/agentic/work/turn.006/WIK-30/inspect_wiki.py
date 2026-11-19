"""Check connected generated documentation and its static routing evidence."""
import hashlib
import json
from pathlib import Path

wiki = Path('source_material/game-wiki')
neighbors = ('development.html', 'project-management.html', 'status.html')
loop = (wiki / 'agentic-loop.html').read_text()
for page in neighbors:
    text = (wiki / page).read_text()
    assert f'href="{page}"' in loop, page
    assert 'href="agentic-loop.html"' in text, page
    assert 'IA-003-001' in text, page
    print(f'Reciprocal link and rejected case: {page}')

contract = (wiki / 'doc-docs-59-agentic-development-loop-md.html').read_text()
assert 'href="doc-docs-59-agentic-development-loop-md.html"' in loop
assert 'href="agentic-loop.html"' in contract
assert 'href="evidence-management-model-policy-readme-md.html"' in contract
for page in (loop, contract):
    for phrase in ('NOT_EXPOSED', 'xhigh', 'IA-003-001', 'interrupted'):
        assert phrase in page, phrase
    assert 'pending at this WIK handoff' not in page
llms = (wiki / 'llms.txt').read_text()
older_guide = (wiki / 'doc-docs-58-software-project-management-md.html').read_text()
for page in (llms, older_guide):
    assert 'resolves the game tools import path' in page
    assert 'November 11, 2026' in page
    assert 'current outer agents shortcut has an import-path defect' not in page
    assert 'unobserved live host MCP loading' not in page
print('Current wrapper and observed readiness wording verified in both IA-003-001 targets')

manifest = json.loads((wiki / 'manifest.json').read_text())['evidence_hashes']
paths = (
    'management/model-policy/README.md',
    'management/model-policy/policy.json',
    'tools/agent_model_policy.py',
    'tools/configure_agent_models.py',
    'tests/test_agent_model_policy.py',
)
for path in paths:
    digest = hashlib.sha256(Path(path).read_text().encode()).hexdigest()
    assert manifest[path] == digest, path
    print(f'Policy evidence hash verified: {path}')
