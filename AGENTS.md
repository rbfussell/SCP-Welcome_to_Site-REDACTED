# Repository development rules

## Required game-wiki workflow

These rules apply to every addition or change to a game system, including design, simulation, SCP modules, content/data, presentation, UI, assets, persistence, and development tooling. Documentation maintenance is part of the change and part of its completion criteria.

### Before changing anything

1. **Preload and examine the relevant game-wiki pages before planning implementation or editing the affected system.** The game wiki is [source_material/game-wiki/index.html](source_material/game-wiki/index.html), with an [agent entry point](source_material/game-wiki/llms.txt). It is distinct from the bulk SCP canon archive at `source_material/scp-api/`; consulting the canon dump does not satisfy this requirement.
2. Start with the wiki's implementation ledger and relevant subsystem or SCP pages. Follow their links to affected dependencies, interactions, source/function pages, design decisions, and verification evidence. Read the specific pages needed to understand what the change could affect; do not substitute a directory listing or search-result snippets for reading them.
3. Read the relevant underlying source and applicable repository Markdown documents as well, including design guides, ADRs, contracts, templates, reviews, and acceptance criteria. The wiki complements these documents; it does not replace them. Identify implemented behavior, planned behavior, known limits, and unresolved decisions before proceeding.
4. Check the wiki's freshness using the documented build/check workflow. If pages are missing or stale, inspect the underlying evidence and restore useful documentation before relying on it. If a material ambiguity or conflict remains, ask the user a focused design question while continuing work that does not depend on that answer. Never silently adopt a speculative rule or reinterpret canon to fill the gap.

### Alongside every change

1. **Update the relevant wiki sources in the same work as the implementation or design change.** Reflect changed mechanics, interfaces, states, dependencies, permissions, persistence, player-facing behavior, verification evidence, and known limitations as applicable. Even an internal refactor must leave its source/function reference and affected system descriptions current. Do not defer wiki maintenance to an unspecified later task.
2. Create a dedicated wiki page when the addition introduces a distinct mechanic, system, SCP adaptation, or concept that warrants its own explanation. Link it from the appropriate directory or overview and **all materially related pages**, and link back to those pages. Preserve useful connections among game mechanics, in-game SCP lore and behavior, canon sources, implementation, and verification; avoid orphan pages and unrelated link clutter.
3. Keep canon claims, selected game adaptations, proposed requirements, and currently implemented behavior explicitly distinct. Cite the relevant canon sources and record intentional departures or unresolved contradictions. Preserve provenance and attribution; never modify the canon archive to make it match the game. Do not present reference material as information automatically known by players, NPCs, or AICs.
4. Update other affected Markdown documentation alongside the wiki. Preserve historical decisions and test reports as historical evidence, adding a clear superseding/current reference where needed rather than rewriting history to imply earlier support or checks.
5. Follow the [wiki authoring/build guide](source_material/game-wiki/README.md): edit authored Markdown or the appropriate generator metadata, not generated HTML. Rebuild generated pages, navigation, search, source references, and manifests after the final source/documentation edits. Generated source listings alone do not replace a needed explanation of changed behavior.

### Final consistency gate

Before declaring the work complete:

- Compare the final implementation/content, tests and actual results, game wiki, and all affected development documentation. Resolve contradictory descriptions, obsolete current-state claims, missing relationships, and unsupported implementation or validation claims.
- Run the checks appropriate to the change, the applicable documentation checks, and both wiki commands from the repository root:

  ```sh
  python3 tools/build_game_wiki.py
  python3 tools/build_game_wiki.py --check
  ```

- Inspect the affected generated pages and cross-links. Check new pages are discoverable and link back to related systems, SCPs, and evidence. A successful link/hash check does not establish semantic agreement; review the descriptions against the actual change.
- Keep the change and its documentation together in the reviewed Git work. Preserve unrelated local changes. In the completion report, identify the documentation updated, verification performed, and any remaining limitation. If rebuilding or validation is blocked, report the exact gap and do not claim the wiki is current or the consistency gate passed.

See the [development workflow](docs/12-development-workflow.md) and the [wiki maintenance page](source_material/game-wiki/development.html) for the corresponding human-readable workflow.
