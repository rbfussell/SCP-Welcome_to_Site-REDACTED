# Development and version control

Git was initialized locally on `main` on October 22, 2026. The first commit captures the existing design baseline, authoring template, diagnostic generator and reviewed sample assets before the OSCP/engine design pass. Local Git remains the source history. On November 4, 2026, the user authorized creating a private GitHub remote and uploading the project history and wiki.

## Source policy

- Commit design, engine/kernel source, content definitions, schema versions, tests, source assets, Godot scenes/resources and intentional reference fixtures.
- Exclude imported/editor caches, compiled outputs, personal tool/account configuration, credentials, local saves and logs through [.gitignore](../.gitignore).
- Keep the bulk `source_material/scp-api/` reference checkout in its own history, excluded from the parent game repository. Track the [lookup guide](../source_material/README.md), source revision references and authored adaptation decisions instead of duplicating the archive.
- Keep reviewed diagnostic PNGs and their manifest alongside the generator. Changes should be explainable from generator inputs/environment; regenerate intentionally rather than committing cache churn.
- Godot .NET presentation and the independent C# simulation direction are selected in [ADR-0005](adr/0005-retain-godot-after-falltergeist-review.md). Pin exact engine/SDK/import versions through M0 qualification; integration and performance still need evidence. Do not replace the engine/runtime silently during a content change.
- Use small commits with a concrete behavior or design decision. Check the diff and relevant requirements before committing. Do not rewrite accepted decision history; supersede an accepted ADR when changing direction.

## Local workflow

The repository-wide [agent rules](../AGENTS.md) require reading the relevant [game wiki](../source_material/game-wiki/index.html) pages before planning or changing a system. This is the authored game knowledge base, separate from the bulk SCP canon archive. Follow its links to affected dependencies, source evidence, SCP adaptations and applicable design/acceptance documents before editing.

Every addition or change includes updates to the affected wiki sources and other applicable Markdown documents. Give distinct new concepts their own pages, link them from their overview and all materially related pages, and link back to the relevant systems, SCPs, canon sources and implementation evidence. Keep planned mechanics, implemented behavior and canon claims distinct. See the [wiki development and consistency workflow](../source_material/game-wiki/development.html) and [build guide](../source_material/game-wiki/README.md).

Before completion, rebuild the wiki and run `python3 tools/build_game_wiki.py --check`, perform the checks appropriate to the change, and review the final implementation, wiki and other documentation for agreement. Link/hash validation is necessary but does not replace checking the accuracy of their descriptions. Keep documentation maintenance with the change; do not leave a stale wiki for a later pass.

```bash
git status --short --branch
git diff
git add -- <reviewed-paths>
git diff --cached
git commit -m "Describe the concrete change"
```

Use a feature branch for nontrivial implementation. The initial design baseline remains on `main`; current prototype development uses `prototype/m0-spatial-scene`. The private GitHub remote is [rbfussell/SCP-Welcome_to_Site-REDACTED](https://github.com/rbfussell/SCP-Welcome_to_Site-REDACTED). The remote name normalizes punctuation unsupported by GitHub; the game title is unchanged. CI and the longer-term branching/merge policy remain undecided.

Documentation checks validate local links, code fences, unique requirement IDs and references. They do not certify gameplay. Prototype gates require the actual runtime evidence in [scope and acceptance](04-scope-and-acceptance.md).

The [spatial prototype guide](15-spatial-prototype.md) records the first runnable Godot/.NET versions, the temporary-copy launcher required by this Linux workspace path, controls and actual spatial/visual checks. This implementation starts on `prototype/m0-spatial-scene`; the larger M0 gate is still open.

The [terminal design review](reviews/terminal-design-checks.md) records this pass's browser/documentation evidence and its implementation limits. Temporary preview wrappers and screenshots are ignored inspection output.

## Remote backup and recovery

The private `origin` remote uses `https://github.com/rbfussell/SCP-Welcome_to_Site-REDACTED.git`. Publish reviewed commits with `git push origin --all` and tags with `git push origin --tags`; pushes preserve both the baseline and prototype histories. The GitHub default branch should be the current `prototype/m0-spatial-scene` branch. A remote contains committed files only: uncommitted edits and ignored saves, caches, builds and local account configuration are not backed up.

For a fresh checkout, clone the private repository using an authorized GitHub account. The committed game assets, source, design documentation, wiki tooling and generated wiki travel together. Restore the separately maintained canon checkout using the [archive recovery guide](../source_material/README.md#restoring-the-reference-checkout) before rebuilding the wiki. Generated wiki pages remain readable without that bulk archive. See the [wiki build guide](../source_material/game-wiki/README.md) for required tooling and freshness checks.
