#!/usr/bin/env python3
"""Serve the game wiki and read-only original SCP dataset JSON on localhost."""
import argparse
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import unquote, urlsplit

ROOT = Path(__file__).resolve().parents[1]
WIKI = ROOT / 'source_material/game-wiki'
ARCHIVE = ROOT / 'source_material/scp-api'


class WikiHandler(SimpleHTTPRequestHandler):
    def translate_path(self, path):
        parsed = unquote(urlsplit(path).path)
        if parsed.startswith('/scp-api/'):
            target = (ARCHIVE / parsed.removeprefix('/scp-api/')).resolve()
            allowed = (ARCHIVE / 'docs/data/scp').resolve()
            if target.is_relative_to(allowed) and target.suffix == '.json' and target.is_file():
                return str(target)
            return str(WIKI / '__unavailable_dataset_path__')
        return super().translate_path(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--port', type=int, default=8765)
    args = parser.parse_args()
    handler = partial(WikiHandler, directory=str(WIKI))
    server = ThreadingHTTPServer(('127.0.0.1', args.port), handler)
    print(f'Game wiki: http://localhost:{args.port}/ (original dataset JSON available through /scp-api/)', flush=True)
    try: server.serve_forever()
    except KeyboardInterrupt: pass
    finally: server.server_close()


if __name__ == '__main__': main()
