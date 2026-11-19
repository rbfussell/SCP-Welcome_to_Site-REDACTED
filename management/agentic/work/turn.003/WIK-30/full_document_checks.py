from pathlib import Path
import re
root=Path.cwd()
files=[root/'README.md',*sorted((root/'docs').rglob('*.md')),*sorted((root/'templates').rglob('*.md')),root/'src/SiteSimulation/README.md',root/'AGENTS.md',root/'management/README.md',root/'management/harness/AGENTS.md',root/'management/harness/README.md',root/'source_material/game-wiki/README.md',*sorted((root/'source_material/game-wiki/content').glob('*.md'))]
errors=[]
for f in files:
 text=f.read_text()
 if sum(1 for line in text.splitlines() if line.startswith('```'))%2: errors.append(f'{f}: unmatched fence')
 for target in re.findall(r'\]\(([^)]+)\)',text):
  target=target.split('#')[0]
  if not target or '://' in target or target.startswith('mailto:'): continue
  target=target.strip('<>')
  base = root/'source_material/game-wiki' if f.parent == root/'source_material/game-wiki/content' else root.parent if f == root/'management/harness/README.md' else f.parent
  if not (base/target).exists(): errors.append(f'{f.relative_to(root)}: missing {target}')
ids=re.findall(r'^\| ([A-Z0-9]+-\d+) \|',(root/'docs/04-scope-and-acceptance.md').read_text(),re.M)
if len(ids)!=len(set(ids)): errors.append('Duplicate acceptance identifiers')
if errors: raise SystemExit('\n'.join(errors))
print(f'DOCUMENT_CHECKS_PASS: {len(files)} documents, local links/fences, {len(ids)} unique acceptance IDs')
