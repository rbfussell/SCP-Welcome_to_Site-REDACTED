# Agentic software project management

The active software-game workflow uses **real selected host agents**, with individual negation of every unused registered role. This replaces the single-developer execution default accepted in commit `2a86ca5`; those historical self-reviewed records are preserved. The game remains its own Git checkout in `Project/`, with its existing source, canon exclusion, wiki and GitHub workflow.

## Authorities

| Concern | Authoritative location |
|---|---|
| Bounded scope, dependencies, blockers, acceptance, reading and command checks | `management/tasks/*.json` |
| Actual registry/selection, dispatch receipts, handoffs, evidence, IA and publication | `management/agentic/` |
| Current mechanics, lore adaptations and implementation limits | [Game wiki](../source_material/game-wiki/index.html), starting at [the ledger](../source_material/game-wiki/status.html) |
| Requirements and game milestone gates | [Scope and acceptance](../docs/04-scope-and-acceptance.md) |
| Contracts, ADRs and historical reviews | [Development documents](../docs/12-development-workflow.md) and `docs/` |
| Actual implementation, assets and history | Existing game directories, `.git` and `.github/` |

The harness process publication JSON is not the game's actual wiki. WIK-30 maintains the real authored knowledge base before collection; DOC-10 publishes process outcomes last. No process record grants knowledge to the player, NPCs, OSCP or AIC.

## A turn and slice

Read [game rules](../AGENTS.md), relevant wiki/dependency/source/design evidence and [the agentic contract](../docs/59-agentic-development-loop.md) before planning. Use small acceptance-defined tasks; proposed backlog entries do not authorize new scope. Keep context JSON with actual `wiki_read`/`docs_read` notes. Declare all changed static input files; exclude evolving task/process/log records and generated wiki outputs to avoid self-reference.

1. Read registry/readiness and begin a scoped assessment. Every frozen registered role defaults YES; each unused role needs a distinct substantive task-specific NO. There are currently 23 software/game roles, including dedicated media, asset-pipeline, web-wiki, native UI/UX, lore and research scopes.
2. Record exact prompts, dependencies, ownership and selection. ENG-00, WIK-30, EVC-28, independent IA-11 and DOC-10 are mandatory. Before each host spawn obtain actual dispatch authorization; afterward record the genuine host receipt. Recording an invocation does not start or authenticate a worker. Distinct role receipts and independent review must be real.
3. Working specialists produce only their assigned scope. Ask material design questions; queue unfamiliar research and stop the requesting worker until a later accepted research response. Preserve interruption/failure and outstanding findings.
4. After every selected execution owner, WIK-30 updates actual authored wiki and affected Markdown, rebuilds/checks, inspects reciprocal links and records semantic agreement. No new execution specialist starts once Wiki begins.
5. EVC-28 inventories evidence and gaps, independent IA-11 audits, then DOC-10 publishes accepted/rejected/blocked outcomes last. No agent runs after DOC in that turn. Discovered late corrections require a new authorized turn rather than an invisible repair.
6. A new slice closes only against the matching accepted immutable publication, its task entity/all-role handoffs, real Wiki semantic handoff and independent IA receipt, alongside passing tests/docs/wiki-build/wiki-check records and stable declared inputs. Commit reviewed implementation, records and documentation together; commands do not commit/deploy.

Milestone declarations select PRT-05 Designer and BLD-27 Builder for an actual plan/attempt; this setup is not a gameplay milestone. [The full role table and ordered loop](../docs/59-agentic-development-loop.md) define software scopes and honest limits.

## Commands from the game root

```sh
python3 tools/manage_project.py status
python3 tools/install_agentic_dependencies.py
.venv-harness/bin/python tools/agentic.py init
.venv-harness/bin/python tools/agentic.py inventory
.venv-harness/bin/python tools/agentic.py status
.venv-harness/bin/python tools/agentic.py plan --input management/agentic/prompts/plan.json
```

The last command needs an actual JSON file containing `turn_id` for a selection-phase turn; it is a pattern, not an existing setup artifact. Use `--input` for exact named arguments and `--output` for a new actual result file. CLI and `workflow_*` stdio MCP operations share the genuine runtime, not a simulated agent loop. `tools/call_agentic_mcp.py` supports actual local protocol calls before host tool loading; preserve their inputs/results as evidence.

```sh
python3 tools/manage_project.py start SLICE-ID --context management/contexts/SLICE-ID.json
python3 tools/manage_project.py record SLICE-ID tests -- .venv-harness/bin/python -m unittest discover -s tests -p 'test_*.py'
python3 tools/manage_project.py record SLICE-ID docs -- python3 tools/check_management_docs.py
python3 tools/manage_project.py record SLICE-ID wiki-build -- python3 tools/build_game_wiki.py
python3 tools/manage_project.py record SLICE-ID wiki-check -- python3 tools/build_game_wiki.py --check
python3 tools/manage_project.py complete SLICE-ID --review management/reviews/SLICE-ID.json
python3 tools/manage_project.py check
```

Select tests appropriate to the changed subsystem; these harness tests do not test native gameplay. Required labels are actual `tests`, `docs`, `wiki-build`, `wiki-check`; argv executes without a shell. Complete only after the matching turn's accepted publication. `block SLICE-ID --reason "specific blocker"` releases the single active slot; resume with updated context after resolving its record. `proposed → ready → active → done` and blocked work remain explicit. Checks and one coordinator write at a time avoid competing record updates. Inspect all logs for secrets before public commits.

## Initialization, imported history and recovery

The historical adapter's accepted game baseline was `cac3555`; the scoped migration snapshots come from `2a86ca5`. Imported sources and actual authorization are immutable under `management/agentic/bootstrap/import-history/` and `imported-project.json`. Only a real accepted setup publication naming the exact import unlocks existing-project readiness; it never invents historical question/research/baseline rounds. New questions, pending research and IA findings still block production. M0 remains open.

Turns .001 and .002 are INTERRUPTED, not accepted. Turn.002 retains its independent REJECTED audit and confirmed SWE-13 case IA-002-001, followed by the actual DOC usage-limit failure; no DOC handoff/publication or accepted import exists for that turn. Its host failure event was recorded despite an MCP response-schema error. Preserve these records and adverse logs. Turn.003 corrects the wrapper import path and shared failure acknowledgement, with independent QA evidence; its terminal review/publication and task/import acceptance remain pending at WIK handoff. A specialist report, this guide or a protocol PASS cannot substitute for that outcome. The inherited fresh-product preflight remains available separately.

Pinned local MCP/jsonschema dependencies are installed in `.venv-harness`. The actual local probe passed initialization/list-tools/inventory (22 tools, 23 roles). A real Codex-exposed `workflow_readiness` call succeeded at 2026-11-11 16:42:06 UTC; `management/agentic/bootstrap/host-connection-observed.json` retains that external observation. The runtime's generic `host_mcp_connection: NOT_OBSERVED` checks no external transport and does not contradict the receipt. Configuration checks also cover configuration only. No model API or purchase is required; Python cannot authenticate all worker receipts or provide an OS sandbox/tamper-proof chain of custody.

The installed outer wrapper now prepends its validated game tools path before dispatch. Independent QA passed agents readiness from the outer root and unrelated cwd, plus legacy board/direct-CLI checks. Shared `failed` dispatch now returns `{status: FAILED, recorded: true, ...arguments}` only after the runtime append succeeds. Invalid, duplicate and sealed-turn calls stay errors. Five new regressions include a fresh real stdio MCP process with explicitly synthetic, isolated records; 40 agentic tests plus 13 adapter tests passed. This does not demonstrate resident host hot reload or a corrected host-exposed failure reply. Use `.venv-harness/bin/python` for MCP-dependent tests; the system-Python shortcut can report READY with `stdio_mcp_dependency: false`.

```sh
.venv-harness/bin/python tools/check_agentic_mcp.py
python3 tools/configure_agentic_client.py --workspace .. --check
python3 tools/configure_project_harness.py --root ..
python3 tools/configure_project_harness.py --root .. --check
```

Authoritative runtime/config/profile/overlay and records live inside the game Git repository. Outer managed docs and `scripts/manage_game.py` are convenience copies; reinstall after template changes. Preserve outer generic files and unrelated configuration. Standalone clones use game-root tools, installing the pinned environment for agentic operations. See [historical integration](../docs/58-software-project-management.md), [active loop](../docs/59-agentic-development-loop.md), [game-wiki management](../source_material/game-wiki/project-management.html) and [profile](profile.json).
