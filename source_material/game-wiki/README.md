# Game wiki

Open [index.html](index.html), or serve this directory with:

```sh
python3 tools/serve_game_wiki.py
```

The wiki is independent of the `../scp-api/` reference checkout. It uses a classic SCP-inspired layout, local search, ordinary HTML links, per-function source pages and explicit implementation status. Reading works offline, including search. No external assets or services are required.

From the repository root, rebuild and validate:

```sh
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

Building requires the repository's .NET SDK, the existing local archive, and Python's `Markdown` package (`python3 -m pip install -r tools/wiki-requirements.txt` if missing). The Roslyn indexer uses compiler assemblies shipped with the SDK and clears NuGet sources; no external .NET packages are used. Its temporary build copy handles the colon in the workspace path.

Edit `content/*.md` for overview pages, and `tools/build_game_wiki.py` for researched SCP entries and subsystem metadata. HTML, search data, and manifests are generated. Function URLs derive from file/type/declaration identity rather than line number; declaration changes can change the URL. Source anchors reflect the current snapshot.

The repository [agent rules](../../AGENTS.md) require examining relevant game-wiki pages before planning or changing a system, maintaining the wiki and applicable development documents alongside every change, and reviewing their agreement with the final implementation. See [Development and documentation consistency](development.html) for this workflow. New distinct concepts need discoverable pages linked to and from all materially related pages. Rebuild and validate after the final edits; successful link/hash checks do not replace reviewing semantic accuracy.

`manifest.json` records source hashes, evidence hashes, checkout revisions and coverage. Its `game_revision` identifies the source commit used for generation; a subsequent commit of generated output can leave that revision at its parent. Source and evidence hashes determine freshness. The [remote recovery workflow](../../docs/12-development-workflow.md#remote-backup-and-recovery) includes these generated pages and documents how to restore the separate canon checkout for rebuilding. `symbols.json` provides machine-readable declarations and candidate call links. `article-evidence.json` and `archive-research.json` retain local research evidence. `llms.txt` gives coding agents an entry point. Current-state claims follow native code; historical design documents retain their original status statements.

Call links are syntax-derived candidates, not a semantically resolved runtime graph. The catalog covers repository-owned C# declarations under `src/` and `game/`, including properties/accessors and smoke drivers; external Godot/.NET APIs and compiler-generated record members are outside that scope.

Canon references use a standard Hosted ↗ / Local snapshot / JSON link group. See [Canon sources](canon-index.html). Local article views provide source provenance and backlinks to game pages. `canon-records/` contains exact single-record extractions, so browsing works under the existing server and offline without exposing or changing the clone. `canon-index.json` maps these copies to their original dataset shard, JSON pointer and archive revision. Validation compares each extraction to the original record.

The wiki server also exposes the original JSON shards through a read-only `/scp-api/docs/data/scp/` route. Local snapshot pages link directly to the original shard as well as the extracted record; the original-shard links also work when opening the wiki through `file://`. A generic server restricted to the wiki directory still supports extracted snapshots, but cannot serve the sibling archive.
