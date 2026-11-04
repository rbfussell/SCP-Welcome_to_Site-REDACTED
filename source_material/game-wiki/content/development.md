# Development and documentation consistency

The repository's [agent rules](evidence-agents-md.html) make this workflow mandatory for game-system additions and changes. It also applies to design, content, UI, assets, persistence and development tools. The [development and version-control guide](doc-docs-12-development-workflow-md.html) remains applicable alongside this wiki.

## Read before changing a system

Start from the [implementation ledger](status.html), then read the affected [shared system](systems.html) or [SCP entry](scp-index.html). Follow relevant connections to dependent systems, [source/function evidence](functions.html), [design documents and decisions](documents.html), and verification sources. Load and examine these specific pages before implementation planning or editing, then inspect the underlying source and applicable Markdown requirements.

This is the game wiki. The [canon source directory](canon-index.html) and sibling SCP archive supply source lore and provenance; reading them alone does not replace reading the game's adopted mechanics. Distinguish source accounts, deliberate game adaptations, proposed requirements and implemented behavior. Resolve material ambiguity with the user before implementing the disputed behavior. Missing or stale documentation must be reconciled with evidence rather than treated as an empty design space.

## Maintain the connected record

Update affected wiki sources during every change. Explain new or changed behavior, its interactions and limits, and connect it to the implementation and relevant checks. Generated declaration listings keep source references current but do not replace a necessary mechanics explanation.

A distinct new system, mechanic or SCP adaptation needs its own page when it warrants a separate explanation. Link it from the appropriate overview or directory and all materially related pages, with return links. Include relevant canon and game-lore relationships and source evidence. A new page must be discoverable through the existing knowledge base.

Update affected design guides, contracts, ADR references, templates, reviews and acceptance documents alongside the wiki. Keep historical evidence historical; add explicit superseding/current references rather than claiming earlier implementations or tests existed. Do not change canon records to agree with the adaptation, or expose authoring knowledge automatically to game actors.

## Private remote and recovery

The user selected a private GitHub remote on November 4, 2026: [rbfussell/SCP-Welcome_to_Site-REDACTED](https://github.com/rbfussell/SCP-Welcome_to_Site-REDACTED). The [version-control guide](doc-docs-12-development-workflow-md.html) records branch publishing and recovery. Committed game source, assets, wiki sources, tooling and generated pages belong in this backup. Ignored runtime data and the independent bulk canon checkout do not. The [archive guide](doc-source-material-readme-md.html) records its upstream and pinned revision for restoration. This repository remains private; remote storage does not imply a public release.

## Author, rebuild and verify

Use the [research and maintenance guide](research.html) and repository `source_material/game-wiki/README.md` for authoring locations. Overview pages live in `content/*.md`; researched SCP entries and subsystem metadata currently live in `tools/build_game_wiki.py`. Project design-library pages derive from their Markdown sources. Do not edit generated HTML by hand.

Links in project Markdown that target generated wiki HTML are preserved as wiki-page links during generation, even before those pages have been regenerated. The final link check verifies their destinations; a missing target must not be hidden by converting the link to plain text.

From the repository root, after the final source and documentation changes:

```sh
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

Run checks appropriate to the implementation and applicable documentation checks too. Inspect changed generated pages, new-page discovery, reciprocal links and evidence references. Compare the final behavior and actual check results with this wiki and all affected documents; resolve disagreements before declaring completion. Fresh hashes and valid links do not prove descriptions are correct, and the wiki build does not run gameplay tests.

Keep documentation and implementation together in the reviewed Git work, preserving unrelated local changes. Report the pages updated and verification performed. If a check or rebuild cannot run, identify the gap and do not claim the wiki is current or that the consistency gate passed.

Related: [main archive](index.html), [engine overview](engine.html), [implementation ledger](status.html), [design library](documents.html), [research and provenance](research.html).
