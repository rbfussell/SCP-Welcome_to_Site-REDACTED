#!/usr/bin/env python3
"""Install/reuse the local pinned harness environment, including colon-path workspaces."""
import argparse
from pathlib import Path
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[1]


def install(root=ROOT, dependencies=True):
    root = Path(root).resolve()
    python = root / '.venv-harness/bin/python'
    if not python.exists():
        # Python venv refuses ':' in the requested pathname; an alias avoids changing the game root.
        with tempfile.TemporaryDirectory(prefix='scp-harness-install-') as directory:
            alias = Path(directory) / 'game'
            alias.symlink_to(root, target_is_directory=True)
            subprocess.run([sys.executable, '-m', 'venv', str(alias / '.venv-harness')], check=True)
    if dependencies:
        subprocess.run([str(python), '-m', 'pip', 'install', '-r', str(root / 'management/harness/requirements.txt')], check=True)
    subprocess.run([str(python), '-m', 'pip', 'check'], check=True)
    return python


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--no-install', action='store_true', help='Verify the existing environment without downloading dependencies')
    args = parser.parse_args()
    print(install(dependencies=not args.no_install))
