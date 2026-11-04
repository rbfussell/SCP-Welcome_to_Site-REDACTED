# Research, provenance and maintenance

## Scope

This wiki researches the checked-out game, not every article in the SCP archive as playable content. Every SCP designation found in the project's README, design documents, templates, native C# source, and scenario content receives an entry. The cloned archive supplies article metadata and research evidence. It is not copied wholesale or modified.

The C# index includes all checked-in `.cs` files under `src/` and `game/`, excluding build/import directories. Roslyn parses declared methods, constructors, operators, destructors, local functions, properties, indexers, and explicit accessors. Auto-property accessors have entries even without executable bodies. Lambdas and implicit/compiler-generated record members remain inside their enclosing source; external Godot/.NET functions are not repository-owned pages.

Function pages show exact declarations, source, documentation comments, containing types, candidate calls and reverse links. **Call relationships are syntax-derived candidates, not a resolved runtime call graph.** Overloads, extension methods, delegates, interface dispatch, and same-name methods can be ambiguous. Candidate links are explicitly labeled; source excerpts remain the authority. Type references likewise show textual syntax use, not proof that a code path executes.

## Evidence precedence

Current native code and scenario data determine implementation. Later prototype guides clarify supported bounds. Design documents establish intention, not implementation. Source articles establish their own fictional accounts; selected game adaptations are identified separately. Historical review pages document earlier checks; the wiki build does not rerun them.

The repository commit, content hashes, source coverage, archive revision, and build time are recorded in [the build manifest](manifest.json). Rebuild after code changes. Generated HTML is a snapshot and cannot update itself while the game evolves.

## Archive and attribution

The local SCP JSON checkout lives at `../scp-api/`. The archive guide records the previously inspected revision `278a69a68e4b85ac7135d061b9fbd6f1027e1a96`; the build manifest records the actual checkout revision used here. Each SCP page retains its source URL, archive creator metadata, and relevant project documents. Creator metadata alone does not replace the article's full credit notice.

SCP-derived summaries in this wiki are attributed to their linked SCP Wiki works and made available under [CC BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/). This statement applies to those summaries; it does not relicense the game's source or unrelated assets. No SCP article artwork or SCP-173 sculpture photograph is bundled. The header uses text and CSS, rather than an imported Foundation mark.

The project already records cross-wiki findings for digital operations and the device-source review. The generated research register lists additional archive pages mentioning SCP-650 or SCP-1499 as discovery leads. A mention is not an adopted mechanic or a compatible shared canon. Full conflicting portrayals require an explicit adaptation decision before implementation.

## Rebuilding and serving

The [development and documentation consistency workflow](development.html) requires reading affected game-wiki pages before planning or editing, updating them alongside every change, and checking final agreement with implementation and other project documents. New concepts receive dedicated pages when appropriate, linked from their overview and all materially related pages with return links. The repository [agent rules](evidence-agents-md.html) and [development guide](doc-docs-12-development-workflow-md.html) record the same requirement.

From the repository root:

```sh
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
python3 tools/serve_game_wiki.py
```

Open `http://localhost:8765/`. The generated wiki also works by opening `index.html` directly: search data is a local script and page links are ordinary relative HTML links. No npm install, API key, external font, or online service is needed to read it.

Author narrative entries in `content/`; modify subsystem metadata and extraction/build logic in `tools/build_game_wiki.py`. Generated pages carry their evidence and build metadata. Source pages include the full indexed C# files, so the browsable reference stays useful without a running IDE. Do not edit generated HTML by hand.


## Traversing canon and game adaptations

[Canon source directory](canon-index.html) pairs hosted SCP Wiki links throughout this wiki with readable local snapshots and exact JSON records. The same **Hosted ↗ · Local snapshot · JSON** convention appears beside external SCP Wiki references, including design documents and research notes. Each local snapshot links back to its citing game pages and related in-game SCP entries.

The local JSON download is an unchanged single-record extraction from the cloned dataset. Its source collection, original shard path, record key/JSON pointer, archive revision, and SHA-256 hashes are recorded for comparison. Full article source is displayed as escaped text; archive HTML and scripts are never executed. The original clone remains untouched. Sources missing from the local indexes are labeled **Local copy unavailable**.

The hosted page may have changed since the snapshot. These are comparison links, not a claim of synchronized content or one universal canon. `canon-index.json` exposes the source mappings and backlinks to other tools; `--check` verifies extracted JSON against its original dataset record and detects archive drift.

The wiki server also exposes the original JSON shards through a read-only `/scp-api/docs/data/scp/` route. Local snapshot pages link directly to the original shard as well as the extracted record; the original-shard links also work when opening the wiki through `file://`. A generic server restricted to the wiki directory still supports extracted snapshots, but cannot serve the sibling archive.
