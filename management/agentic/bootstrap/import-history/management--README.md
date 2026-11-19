# Software project management

This is the active management profile for the existing SCP game. The outer portable
harness supplies the workspace entry point; the game remains its own checkout in
`Project/`. Nothing moves the engine, assets, canon archive or GitHub workflow again.

## Authority and records

| Concern | Authoritative location |
|---|---|
| Work selection, scope, dependencies, blockers, acceptance and evidence | `management/tasks/*.json` |
| Current mechanics and implementation limits | [Game wiki](../source_material/game-wiki/index.html), starting at [the ledger](../source_material/game-wiki/status.html) |
| Requirements, scope and milestone gates | [Scope and acceptance](../docs/04-scope-and-acceptance.md) |
| Design contracts, ADRs and historical reviews | [Development documents](../docs/12-development-workflow.md) and `docs/` |
| Source, assets, saves and tests | Existing game directories; no competing management copy |
| Change history and published game wiki | Existing `Project/.git` and `.github/workflows/game-wiki-pages.yml` |

The imported baseline is commit `cac3555`. Its earlier checks remain historical;
importing it does not pass M0, M1 or any new qualification gate. Proposed tasks are
candidates, not silently approved features. This board need not reproduce every
paragraph of the long-term scope document. Add the next selected slice explicitly.

The generic harness's `wiki/` JSON publication store is an optional strict-workflow
process record. It does not replace, override or regenerate the game knowledge base.
This adapter does not initialize that store or pretend to have completed strict
preflight, a dispatched crew or independent Internal Affairs.

## A slice

1. Read [game agent rules](../AGENTS.md), relevant wiki pages and linked design/source
   evidence. Check wiki freshness. Write a small task JSON from [the template](task-template.json),
   marking it `ready` only when selected. Specify measurable acceptance criteria,
   affected wiki pages, dependency IDs, blocking questions and **all changed input files**.
2. Create context JSON listing `wiki_read` and `docs_read`, each with `path` and real
   reading `notes`. Include the implementation ledger and development page. Start the
   slice. Reading receipts are developer attestations with hashes, not proof of cognition.
3. Implement, ask focused questions when design is ambiguous, and preserve unrelated
   work. Update the wiki's authored sources and affected Markdown alongside the change.
   Use actual research when needed; existing accepted decisions do not require a new
   three-turn startup sequence. Block dependent work when an answer is required.
4. Record appropriate real tests and documentation checks, then rebuild/check the wiki.
   Logs go to versioned `management/evidence/<slice>/`; inspect them for credentials or
   personal data before committing. Check commands use argv, without a shell.
5. Compare behavior, requirements, actual results, wiki and other documentation. Inspect
   generated pages and reciprocal links. Complete with a review JSON declaring its
   actual mode, every acceptance criterion's evidence, and remaining limitations.
6. Review the Git diff. Commit the slice, records, docs and wiki together. Push reviewed
   work using the existing remote policy. No management command commits or deploys.

From the **game root** (`Project/`):

```sh
python3 tools/manage_project.py status
python3 tools/manage_project.py start SLICE-ID --context management/contexts/SLICE-ID.json
python3 tools/manage_project.py record SLICE-ID tests -- python3 tools/run_spatial_scene.py --check
python3 tools/manage_project.py record SLICE-ID docs -- python3 tools/check_management_docs.py
python3 tools/manage_project.py record SLICE-ID wiki-build -- python3 tools/build_game_wiki.py
python3 tools/manage_project.py record SLICE-ID wiki-check -- python3 tools/build_game_wiki.py --check
python3 tools/manage_project.py complete SLICE-ID --review management/reviews/SLICE-ID.json
python3 tools/manage_project.py check
```

Choose tests appropriate to the slice; the example domain test is not sufficient for
changed rendered behavior. The management-document check covers this integration's
links/records only, and is not a universal game-document checker. `block SLICE-ID
--reason "specific blocker"` releases the single active slot. Resolve its question
record, then `start` with updated context to resume; earlier logs remain on disk.

`proposed → ready → active → done` is the usual lifecycle. `active/ready → blocked`
records unavailable work. Edit proposed task scope/selection deliberately; do not
mark it done by hand. Completion needs four passing labels (`tests`, `docs`,
`wiki-build`, `wiki-check`), unchanged declared inputs since those checks, intact
logs, a post-build wiki check and explicit semantic review. Exit zero reports the
command's result, not the truth of every assertion one could make about the game.
All changed source/data/docs, new files and management tooling must be in `inputs`;
exclude the task's own evolving record, logs and generated wiki outputs to avoid
self-reference. Completed snapshots are historical: later slices may change their
inputs without invalidating the recorded past result. Hashes are not tamper-proof.

One developer performs coordination, implementation, evidence collection, self-review
and documentation by default. Optional QA, canon/design, art, audio or independent
review workers are selected only when explicitly requested by the user or applicable
instructions. Declare actual receipts for independent review; never manufacture them.
No manufacturing roles, sales presenter, model API, MCP installation or paid account
is required. Self-review is useful but does not claim independent assurance. The CLI
is for one coordinator at a time; it is not a concurrent distributed task service.

## Outer-root installation and recovery

The outer root has an empty protected `.git` mount, not the game's Git history.
Authoritative tools, profile, overlay templates and task/evidence records therefore
live **inside the game repository**. The installed outer files are convenience copies.

```sh
# From the outer workspace root; safe to repeat, preserves unmanaged text.
python3 Project/tools/configure_project_harness.py --root .
python3 Project/tools/configure_project_harness.py --root . --check
python3 scripts/manage_game.py status
python3 scripts/manage_game.py check
```

The installer adds marked blocks to the root `AGENTS.md`/`README.md`, a small
`config/software-project.json` pointer and `scripts/manage_game.py` wrapper. Other
harness files and strict runtime are left intact. Reinstall after changing overlay
templates. A standalone game clone uses `tools/manage_project.py` directly and needs
no outer harness. Recreating the outer generic harness still requires the original
harness distribution; this repository versions the overlay, not that full release.

See [the integration contract](../docs/58-software-project-management.md),
[the game-wiki explanation](../source_material/game-wiki/project-management.html),
and [profile](profile.json).
