# Software-engineering handoff — turn.002 / SWE-13

Assigned result: **COMPLETE** for the bounded runtime/CLI/MCP implementation.
This is not IA acceptance, process publication, final wiki consistency, host MCP
registration or game qualification. Source is frozen for WIK-30 and terminal review.

## Context and recovery

Read the required game AGENTS, management guide/task board, authored game-wiki
status/development/project-management pages, wiki authoring guide, linked development
workflow/integration contract, actual copied workflow/preflight/schema/config code,
and ENG-00/KRA-14 reports for the original and recovery setup. Their old active
single-developer descriptions are superseded by the latest explicit human request;
WIK owns the canonical documentation update, not this engineering report.

The parent recorded turn.001 INTERRUPTED after actual quota failures. Preserve it,
its partial files and provisional logs. This recovery assignment uses turn.002;
no accepted turn.001, historical survey/research, host connection or game milestone
is invented. Source edits made during bootstrap/recovery are identified by their
actual final files rather than retroactively represented as separately accepted work.

## Actual implementation

- `tools/game_workflow.py` subclasses the genuine copied `Workflow` engine. It maps
  state, evidence, owned artifacts and process publications into `management/agentic/`.
  Config/schema remain versioned under `management/harness/runtime/`. This process
  JSON publication store never replaces the actual game wiki or canon archive.
- Fresh `initialize()` creates managed parents and registers the versioned software
  crew via real append-only registry revisions. `config/game_roles.json` now contains
  23 scoped roles, including WIK-30 and media/assets, web-wiki, native UI/UX, lore,
  research and simulation expertise. It preserves existing registry history.
- Every turn accounts for its complete frozen registry exactly once. All default
  YES. The inherited engine checks substantive individual/unique exclusions, exact
  prompts, dependencies and corrections. Game policy makes ENG/EVC/independent IA/
  DOC/WIK mandatory, requires Wiki to depend on every selected execution role, and
  stops new specialist dispatch once Wiki begins. The CLI rejects duplicate JSON keys.
- Real authorization precedes host spawning; actual external receipts are then
  recorded. Distinct receipts are enforced for every role, including an atomic check
  inside the workflow lock. The runtime does not spawn workers or authenticate strings.
- WIK completion requires real authored game-wiki Markdown pages, semantic review,
  real build/check evidence and passing corresponding command records. Wiki reports
  before collection; blocked/failed work remains visible and cannot be accepted.
- Collector paths are canonicalized consistently across mapped aliases, artifacts
  and package handoffs. Genuine Collector → independent IA → Document Chief phases,
  hashes, findings and sealed publication remain the inherited engine's mechanism.
  DOC publishes process records last; no game source/wiki rewriting belongs there.
- `tools/agentic.py` exposes init/inventory/status/begin/plan/select/authorize/invoke/
  handoff/advance/collect/audit/publish/interrupt plus import/register/question/answer/
  research/failure/readiness operations. Exact prompts and arguments live in JSON
  input files. `tools/agentic_mcp.py` exposes the same 22 operations as actual stdio
  MCP tools with explicit server-wide operating instructions. Both use one runtime.
- `tools/check_agentic_mcp.py` genuinely initializes a subprocess protocol session,
  lists tools and calls inventory. A 30-second process-group watchdog bounds blocked
  IPC and cleanup. Its result explicitly distinguishes local protocol success from
  Codex host registration. No model API, network listener or paid service was added.
- `tools/manage_project.py` accepts the agentic profile. New active work binds a
  genuine turn. Completion requires the matching accepted immutable publication,
  a `task.<slice>` entity, matching all-role task IDs, actual WIK semantic handoff
  and the actual independent IA receipt. Current agentic policy also applies to
  active work started with old metadata. Historical completed self-review records
  remain historical and are not silently requalified.

The copied engine has two small documented extension patches: work selection uses
`self.readiness()`; preflight provides `initial_prerequisites_ready()` for a truthful
import predicate. The all-role atomic receipt check is also local to the copied
engine. Original source hashes and local changes are recorded in `UPSTREAM.json`;
the outer generic engine was not changed by this assignment. Repeated identical
agent definitions are cached by content digest per process for validation efficiency;
registry/event hashes and changed definitions remain checked.

## Existing-project import and remaining gates

Created immutable `management/agentic/imported-project.json` from the parent's
actual user-authorization transcript and exact commit-2a86ca5 historical snapshots.
Hash: `cf12434737bb7519349565abfe1d415aee97dda8cbf3fc71af22394b79b3a9b1`.
Import is scoped history, not newly conducted research or new gameplay acceptance.

It cannot enable production until a real accepted setup publication explicitly
publishes `project.import` with the matching `import_hash` and record evidence.
Historical question/research/baseline booleans stay truthful. New unanswered
questions, pending research and confirmed findings still block production; later
research requests stop the requesting worker and survive interruption. The original
strict fresh-product preflight remains available separately.

For this slice, DOC must publish `task.agentic-loop-setup` with
`value.task_id = agentic-loop-setup`; the active task is bound to turn.002. Root owns
actual MCP submission, WIK owns final wiki/docs, and terminal roles own their actual
collection/review/publication. Those future outcomes are not claimed here.

## Reviewed bootstrap contributions

Inspected/adopted the parent's provisional `configure_agentic_client.py`,
`install_agentic_dependencies.py` and `call_agentic_mcp.py`, plus configuration tests
and scoped role definitions. They were not all authored by this engineer.
Configuration preserves unrelated settings, rejects conflicts/escapes and reports
CONFIGURED_UNVERIFIED. Installation uses a temporary alias for this colon-containing
workspace but retains the real game root; pinned dependencies are local. The RPC
helper uses a real stdio client, not fabricated tool results; callers should bound
its process (the parent's actual calls use an external timeout). No credentials
are stored in these scripts or the evidence reviewed for this assignment.

## Actual final verification

SWE-owned logs in this directory:

- Agentic policy suite: **29 tests PASS, 23.300 s**, `agentic-tests-final.log`.
- Historical software-adapter suite: **13 tests PASS, 0.472 s**,
  `legacy-management-tests.log`.
- Client configuration suite: **6 tests PASS, 0.005 s**, `client-setup-tests.log`.
- Actual local stdio protocol: **PASS**, protocol 2025-11-25, 22 tools, 23 roles,
  `mcp-probe.log`. Sandbox IPC blocked an earlier probe; its incomplete turn.001
  log is retained and that process was interrupted, not called a success. Final
  successful probe used actual execution outside the sandbox with the bounded tool.
- Python compile: **10 files PASS**, `compile.log`.
- Actual process structure: **PASS**, `final-structure-check.log`: exact 23-role
  selection/defaults, turn.001 interrupted, turn.002 active, import not yet accepted,
  production false and historical survey/research flags still false at inspection.
- `final-inventory.json` and `task-status.json` are observed pre-terminal snapshots.

Separately, QA-16 confirms **35 agentic/client tests plus 13 legacy tests PASS**
after the final source freeze, with its own logs, source snapshot and actual MCP
probe. These independent QA results are evidence, not this engineer claiming to
have been its own independent reviewer. The 48-test totals overlap the suites above;
do not add them as distinct coverage. Provisional earlier failure/pass logs remain
historical rather than being erased or counted as final qualification.

## Limits

CLI readiness is scoped to the local registry/runtime/Python prerequisites; external
host worker availability, specialist capabilities and authenticity require real
receipts and review. The protocol probe starts its own server. Codex host tools have
not been observed loaded; configuration/protocol success remains explicitly separate.
Python hashes and logs are neither a sandbox nor tamper-proof chain of custody.

No game mechanics, art, canon archive, native scene, gameplay milestone or remote
publication was changed/qualified by this role. No final wiki rebuild or semantic
acceptance is claimed before WIK and IA actually perform it. New research-resolution
and every possible IA-resolution combination are not exhaustively tested. No known
blocking implementation defect remains in the tested setup scope; terminal acceptance
and documentation consistency are still pending at this handoff.
