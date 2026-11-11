#!/usr/bin/env python3
"""Check integration Markdown links/fences and software board integrity, not gameplay."""
from pathlib import Path
import re
from urllib.parse import unquote, urlsplit
from manage_project import ROOT, SoftwareProject


def main():
    files = [ROOT / p for p in ('AGENTS.md', 'management/README.md', 'docs/58-software-project-management.md',
                              'docs/12-development-workflow.md', 'source_material/game-wiki/README.md')]
    errors = []
    for path in files:
        text = path.read_text()
        if len(re.findall(r'^```', text, re.M)) % 2:
            errors.append(f'Unbalanced code fence: {path.name}')
        for destination in re.findall(r'\[[^\]]*\]\(([^)]+)\)', text):
            parts = urlsplit(destination)
            if parts.scheme or parts.netloc or not parts.path:
                continue
            target = (path.parent / unquote(parts.path)).resolve()
            if not target.exists():
                errors.append(f'{path.relative_to(ROOT)}: missing {destination}')
    if errors:
        raise ValueError('\n'.join(errors))
    print(SoftwareProject().check())
    print(f'MANAGEMENT_DOCS_PASS: {len(files)} Markdown sources; links, fences and task integrity. No gameplay qualification.')


if __name__ == '__main__':
    main()
