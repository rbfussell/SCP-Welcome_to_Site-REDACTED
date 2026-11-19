#!/usr/bin/env python3
"""Forward to the versioned game management adapter selected by this workspace."""
import json
from pathlib import Path
import runpy
import sys

root = Path(__file__).resolve().parents[1]
config = json.loads((root / 'config/software-project.json').read_text())
project = (root / config['project_path']).resolve()
if config['profile'] != 'software-game' or not project.is_relative_to(root):
    raise ValueError('Invalid software project pointer')
script = 'manage_project.py'
if sys.argv[1:2] == ['agents']:
    sys.argv.pop(1)
    script = 'agentic.py'
tools = project / 'tools'
# run_path does not add the target script's directory for sibling imports.
sys.path.insert(0, str(tools))
runpy.run_path(str(tools / script), run_name='__main__')
