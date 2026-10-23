# Development and version control

Git was initialized locally on `main` on October 22, 2026. The first commit captures the existing design baseline, authoring template, diagnostic generator and reviewed sample assets before the OSCP/engine design pass. Local Git is the source history; publishing a remote is a separate action.

## Source policy

- Commit design, engine/kernel source, content definitions, schema versions, tests, source assets, Godot scenes/resources and intentional reference fixtures.
- Exclude imported/editor caches, compiled outputs, personal tool/account configuration, credentials, local saves and logs through [.gitignore](../.gitignore).
- Keep the bulk `source_material/scp-api/` reference checkout in its own history, excluded from the parent game repository. Track the [lookup guide](../source_material/README.md), source revision references and authored adaptation decisions instead of duplicating the archive.
- Keep reviewed diagnostic PNGs and their manifest alongside the generator. Changes should be explainable from generator inputs/environment; regenerate intentionally rather than committing cache churn.
- Godot .NET presentation and the independent C# simulation direction are selected in [ADR-0005](adr/0005-retain-godot-after-falltergeist-review.md). Pin exact engine/SDK/import versions through M0 qualification; integration and performance still need evidence. Do not replace the engine/runtime silently during a content change.
- Use small commits with a concrete behavior or design decision. Check the diff and relevant requirements before committing. Do not rewrite accepted decision history; supersede an accepted ADR when changing direction.

## Local workflow

```bash
git status --short --branch
git diff
git add -- <reviewed-paths>
git diff --cached
git commit -m "Describe the concrete change"
```

Use a feature branch for nontrivial implementation once work begins. This initial setup remains on `main` and creates reviewable local commits. Remote hosting, CI provider and branching/merge policy remain to be selected; no public repository or external publication is implied.

Documentation checks validate local links, code fences, unique requirement IDs and references. They do not certify gameplay. Prototype gates require the actual runtime evidence in [scope and acceptance](04-scope-and-acceptance.md).

The [terminal design review](reviews/terminal-design-checks.md) records this pass's browser/documentation evidence and its implementation limits. Temporary preview wrappers and screenshots are ignored inspection output.
