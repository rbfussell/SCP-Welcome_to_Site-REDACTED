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
runpy.run_path(str(project / 'tools' / script), run_name='__main__')
