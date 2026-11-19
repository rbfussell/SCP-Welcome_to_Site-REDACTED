# SWE-13 correction — turn.003

COMPLETE for the assigned tooling correction and owner verification. Setup/import/task acceptance remains pending independent QA, WIK and terminal review.

Read the game AGENTS, management guide and board, docs59, actual generated ledger/agentic-loop/project-management/development pages and wiki build guidance, ENG/KRA turn.003 handoffs, independent IA-002-001 and the observed host failure-response defect before editing. The current wiki still describes the earlier defect and unobserved host connection; WIK owns its scheduled refresh after execution owners.

`management/harness/manage_game.py` now prepends the validated nested game's tools directory before `runpy`. The existing profile/containment checks and both command routes remain. Reinstalled through `python3 tools/configure_project_harness.py --root ..`; its actual result changed only `scripts/manage_game.py`. Installed/template hashes match in `tested-source-hashes.json`.

The shared `tools/agentic.py` failure dispatch first calls the genuine runtime's `failed` operation, then returns `{turn_id, agent_id, reason, status: "FAILED", recorded: true}`. Its validation exceptions still propagate. The copied upstream runtime and MCP facade bytes are unchanged. This acknowledges a recorded invocation failure; it does not accept a turn or fabricate a new worker result.

Added `tests/test_agentic_entrypoints.py`: five regression tests covering outer/unrelated-cwd agents dispatch, legacy board/direct CLI, pointer containment, CLI failure acknowledgement/duplicate rejection, and an actual fresh stdio MCP server against an isolated labelled fixture. Every synthetic invocation/receipt is explicitly fixture-only. No production failure mutation was replayed.

Verification:

- Focused entrypoint suite: PASS, 5 tests, 4.163s. Real protocol transcript is `after-mcp-fixture-transcript.json` (protocol 2025-11-25). Invalid agent/reason calls append no event; one valid failure returns a dictionary and appends exactly one FAILED event; duplicate/sealed calls remain MCP errors without an additional FAILED event.
- Full agentic/client/entrypoint suite: PASS, 40 tests, 26.924s; legacy adapter suite: PASS, 13 tests, 0.452s. These overlap the focused suite, totaling 53 distinct tests, not 58.
- Actual installed wrapper agents readiness from the outer root and `/tmp`: PASS/READY. Installed legacy board status and direct game-root CLI readiness: PASS. Managed overlay check: PASS. Exact argv/cwd, logs and hashes are retained in `installed-entrypoints.json` and `suite-results.json`.
- Pre-fix source bytes/manifests, actual installed-wrapper FAIL, and red fixture logs remain under this directory. With real IPC the pre-fix suite reproduced the wrapper import failures and MCP response-schema error after one genuine fixture FAILED event. The earlier default-sandbox run's MCP timeout is also retained; it is not passed off as protocol success. Corrected real IPC runs used a 35-second process-group watchdog.

WIK must include the new test in generator evidence metadata and update affected real wiki/technical docs, temporal history and externally observed host-connection description; the parent has added the test to declared task inputs. WIK's final build/check and semantic review are NOT_RUN by SWE. Independent QA must rerun the corrected entrypoints and real fixture. A resident host MCP process may retain earlier imports: this verifies a freshly started real local server, without claiming hot-reload or a newly exercised host acknowledgement.

Turn.001/turn.002 interruption, prior rejected IA-002-001, actual DOC quota failure, adverse response evidence and absent prior acceptance/publication remain historical evidence. No native gameplay, M0, art, canon, new research/finding-resolution matrix or completion milestone is qualified. No new material ambiguity or research request arose. No source/wiki/process acceptance is asserted here.
