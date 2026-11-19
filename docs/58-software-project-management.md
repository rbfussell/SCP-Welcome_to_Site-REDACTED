# Software project management integration

Status: historical lightweight adapter, accepted in commit `2a86ca5`. Its single-developer default is superseded by [the active agentic loop](59-agentic-development-loop.md) at the user's later request. The text below preserves the original adapter scope and limits; it is not current permission to skip real role selection or independent review. Game mechanics and acceptance milestone status remain unchanged. The existing game checkout now resides under
`Project/` inside a portable project harness workspace.

## Selected workflow

Use the [software management guide](../management/README.md) and
[profile](../management/profile.json) for the active workflow. Coordination records
live with the game in Git. Relevant wiki reading, small bounded changes, appropriate
runtime checks, design/document maintenance and the final semantic consistency gate
remain mandatory. The outer harness is a convenience entry point, not another game
state store or source of mechanics truth.

The generic harness's mandatory crew, specialist ownership, independent IA and
three-stage new-product preflight remain available through its original strict
workflow. The user requested adapting the workflow for this existing software game,
so these are not active requirements for ordinary slices. A single developer records
self-review honestly; optional independent review needs an actual receipt. No claim
is made that the adapter passed strict-harness preflight or independent assurance.

[The software adapter](../tools/manage_project.py) provides status, record checks, start, complete, block and
integrity validation. It records actual subprocess argv, logs, exit codes and SHA-256
hashes. Completion requires four passing check categories, stable **declared inputs**,
a wiki check after rebuilding, every acceptance criterion's evidence and a semantic
consistency review. It does not authenticate workers, verify the meaning of test
claims, spawn agents, install dependencies, commit, push or deploy. Missing input
coverage remains a review concern. Later changes do not erase accepted historical
snapshots. Concurrent coordinators are outside this bounded adapter.

## Layout and portability

`Project/.git` remains the game repository, with its original GitHub origin and
Actions/Pages workflow. The outer `.git` path is an empty protected mount, not an
initialized repository. Do not flatten or reinitialize the game to make the wrapper
work. The authoritative adapter, profile, overlay templates and records are committed
under the game checkout; the outer harness distribution is separate.

[The overlay installer](../tools/configure_project_harness.py) installs/checks the versioned overlay into a
containing harness. It preserves text outside marked blocks and does not alter the
strict engine, crew catalog or release manifest. Run game commands from `Project/`;
the outer `scripts/manage_game.py` wrapper forwards to the same versioned CLI and
resolves the game root independently of the calling directory. A standalone clone
continues to work without the outer harness or its Python packages.

## Documentation and verification

The [game wiki](../source_material/game-wiki/project-management.html) explains these
boundaries and links back to the existing [development workflow](12-development-workflow.md).
Its builder includes management guide/profile/overlay/tool sources as evidence and
makes the new concept discoverable through navigation and related pages. Evolving
task/evidence records are intentionally not copied into the generated mechanics wiki;
this avoids recursive hashes and keeps process history separate from mechanics claims.

[Management tests](../tests/test_project_management.py) exercise closure failures, stale inputs, evidence
integrity, truthful review modes, context requirements, path containment, independent
standalone use and repeatable non-destructive overlay installation.
[The documentation check](../tools/check_management_docs.py) checks integration links/fences/board integrity.
Existing game runtime checks, wiki rebuild/freshness checks and semantic inspection
remain separate. This tooling change does not itself requalify gameplay or close M0.

Acceptance: both root/game CLI entry points share one board; original generic files
remain available; the game is independently runnable/versioned; completed slices have
real evidence and honest review; wiki and development docs agree with the profile.


## Superseding workflow

[The active agentic contract](59-agentic-development-loop.md), [management guide](../management/README.md) and [game-wiki loop page](../source_material/game-wiki/agentic-loop.html) now require every registered role to be considered, individual unused-role negations, actual authorized host workers, WIK-30 for the real game wiki, Collector → independent IA → Document Chief and matching accepted-publication slice closure. Historical self-review records are not rewritten as independent review. The CLI's original task/evidence mechanism remains alongside that loop. The installed outer agents shortcut resolves the game tools import path, and a Codex-exposed readiness call was observed on November 11, 2026. The active contract distinguishes that observation from configuration checks and fresh-process testing.
