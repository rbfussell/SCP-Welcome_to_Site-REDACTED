# Architecture setup review — turn.001 / KRA-14

Status: COMPLETE for this bounded architecture review. This is a specialist
recommendation, not independent IA acceptance, a frozen game baseline, or a claim
that the desktop has connected an MCP server. No runtime or canonical documentation
was edited by this assignment.

## Scope and reading

The user's latest request explicitly authorizes an actual agentic loop, individual
negation of unused registered agents every turn, and a specialist for the existing
game wiki. It supersedes the earlier single-developer execution default without
discarding that adapter's accepted historical records or the game-wiki workflow.

Read `Project/AGENTS.md`, `management/README.md`, `management/profile.json`, the
actual setup assessment and selection, ENG-00's report, game-wiki authored pages
`content/development.md`, `content/status.md`, `content/project-management.md`, the
wiki authoring README, `docs/12-development-workflow.md`, and
`docs/58-software-project-management.md`. Inspected the actual copied
`management/harness/runtime/workflow.py`, `preflight.py`, the outer
`docs/preflight.md`, and the initial path-only `tools/game_workflow.py` adapter.

Current gameplay remains an existing, incompletely qualified prototype. The ledger
leaves M0 and wider simulation features open. This setup neither reopens settled
game requirements nor qualifies any new game behavior.

## Recommended architecture

Keep the copied genuine harness as the event, registry, dispatch, evidence and
publication engine. Put game-specific policy and path mappings in `GameWorkflow`,
with thin CLI and stdio-MCP entry points using the same API and state. Do not
implement separate CLI/MCP state machines, pretend a role record starts a worker,
or replace the independent game repository or actual knowledge base.

Authority remains divided by purpose:

- `management/tasks/`: bounded slice scope, dependencies, acceptance and real checks.
- `management/agentic/`: actual agent selection, host dispatch receipts, handoffs,
  hashes, review outcomes and immutable process publications.
- `source_material/game-wiki/`: authored game mechanics, implementation limits,
  lore/adaptation provenance and connected source/test evidence.
- Existing `docs/`, source/assets/tests and game Git: contracts, decisions,
  implementation and history. The bulk canon archive remains untouched.

All registered roles must appear exactly once in each frozen selection. Default
YES means every NO needs an individual substantive task-specific reason; it does
not require registering unrelated catalog roles or inventing busywork. Required
ENG-00/EVC-28/IA-11/DOC-10 and WIK-30 cannot be excluded in a completed normal turn.
Keep Vince registered with an explicit reason when unused. Existing registered
specialists cannot disappear from the next turn's selection.

The inspected runtime already validates exact selection key coverage, unique
substantive exclusion strings, complete exact prompts, valid acyclic dependencies,
authorization before recorded invocation, handoff prompt hashes, IA receipt
separation, phase order, evidence hashes and immutable publication. Reuse these
checks, strengthening gaps with small game-policy overrides and focused tests.
These are recorded-workflow checks, not host authentication or a sandbox.

Use actual host workers after authorization. Each selected role owns its report
directory; source owners may change their explicitly assigned code. Distinct
independent IA and question-survey receipts are necessary. Never use dummy test
receipts in production. A host receipt remains external evidence that IA must
inspect, not a guarantee provided by an arbitrary nonempty string.

## Required ordering and documentation boundary

For this turn: ENG-00 → KRA-14 → SWE-13 and QA-16 → WIK-30 → EVC-28 → independent
IA-11 → DOC-10. SWE/QA may collaborate within separate ownership; Wiki waits for
their final source/tests/evidence. WIK-30 reads the actual final work, maintains
authored wiki and applicable Markdown, then rebuilds/checks and semantically
inspects generated pages before collection.

The Collector inventories and hashes specialists' outputs, exclusions, dispatches,
handoffs, real logs and missing evidence; it does not repair them. IA reviews the
frozen package and records findings for responsible owners. Document Chief runs
last and publishes the accepted/rejected/blocked process outcome. DOC must not
silently rebuild or rewrite the game wiki after IA, because that would change the
reviewed evidence. No specialist invocation follows publication in the same turn.
Preserve interruptions, failures and findings; corrections use the next recorded
turn rather than rewriting completed handoffs.

Avoid self-referential wiki hashes: evolving turn/publication/log records are
process evidence, not a generated mechanics-page input. The stable policy guide,
runtime source and relevant tests may be normal wiki evidence. A process
publication can reference the final wiki check and its hash without putting its
own later-changing record into that wiki build's source inputs.

## Explicit existing-project migration policy

The generic engine's fresh-product preflight insists on accepted setup, independent
questions, research and a published architecture baseline before work. The accepted
game already has design/history, and the user requested restoring the execution
loop rather than restarting product discovery. A narrow, documented migration
policy is appropriate, with all of the following conditions:

1. Record the existing baseline commit, historical decision/evidence locations,
   user authorization and applicable imported scope. Describe it as imported
   history, not newly qualified software or independently surveyed/researched work.
2. Allow imported-project readiness only after an actual accepted setup publication,
   usable selected tools and no outstanding questions, research requests or
   confirmed IA findings. A merely configured policy or active setup cannot enable
   production. Keep the underlying historical `question_round_complete`,
   `initial_research_complete` and `baseline_complete` booleans truthful.
3. Expose imported readiness separately in the preflight report, including its
   basis and limits. Never insert synthetic historical events or publish
   `architecture.baseline` during setup as a shortcut.
4. Preserve the question mechanism and the later research loop. New material
   ambiguity requires actual user answers; new research requests stop the
   requesting specialist, persist across interruption, require a later Researcher
   invocation and accepted source-backed response before later resumption.
5. Keep normal milestone triggers: an actual declared work milestone requires
   Prototype Designer and Builder. This bounded SETUP declares no game milestone.

Concrete integration hazard: inherited `request_research` separately tests
`initial_research_complete`, in addition to `preflight_status` deciding work
readiness. Overriding only `production_allowed` would allow work but prevent new
research requests. Implement a common explicit "initial prerequisites or accepted
import" predicate for these gates, keeping historical booleans unchanged and
never bypassing pending research/questions/findings. Also inspect direct readiness
imports in `select`: the selected-role readiness check must use the game root's
mapped config/state, not accidentally the absent generic root stores.

## Setup and MCP truthfulness

The same methods should be reachable through CLI and the actual stdio MCP app.
Probe initialization, tool listing and an inventory call against the real
subprocess/interpreter. Retain protocol logs without credentials. A successful
local protocol test proves that server instance works; configuration written for
the Codex host is only CONFIGURED_UNVERIFIED until the host actually loads it and
a host tool call succeeds. Unavailable live host loading must remain an explicit
limitation, not become an invented successful initialization receipt. The CLI can
execute the genuine loop while the host connection is pending if the documented
readiness scope makes that distinction explicit.

Use portable interpreter commands or a documented local environment, not committed
absolute developer executable paths or credentials. Keep temporary path aliases
out of public configuration. No paid service/model API or hardware tool is needed
for this bounded software-process setup.

## Minimal change checklist for owners and IA

- SWE: narrow path/policy adapter; shared CLI/MCP API; migration readiness and later
  research compatibility; managed-root containment; actual receipt/phase checks.
- SWE/board owner: require a matching accepted agentic publication, WIK handoff
  and real slice linkage for new agentic completions, retaining old self-review
  records as history. An unrelated accepted turn cannot close arbitrary slices.
- QA: negative tests for omitted/extra roles, duplicate NO reasons, mandatory Wiki,
  unauthorized or changed-prompt dispatch, dependency and phase violations,
  invalid independent IA receipts, incomplete work, and post-publication mutation.
- QA: test unaccepted/blocked/interrupted import setup, pending question/research/IA
  blockers, truthful historic booleans, a real later research-request lifecycle,
  and wrong-turn/wrong-slice software completion.
- Wiki: supersede current single-developer claims in rules/profile/guide/wiki and
  preserve historical evidence. Separate actual game wiki from process JSON;
  retain the full reading/build/semantic gate and reciprocal links.
- Collector/IA/DOC: inspect final actual assignments, workers, logs, readiness and
  semantic descriptions; publish only their observed dispositions and limits.

## Explicit architecture disposition

`unresolved_questions: []`

Coverage: software-vs-manufacturing scope, registered-role negation, phase/ownership
boundaries, real wiki responsibility, host execution versus record authentication,
MCP protocol versus host connection, existing-project import, later questions and
research, milestone scope, task linkage and immutable evidence. No unresolved
user-design choice is necessary for this bounded setup. The checks above are
implementation/review obligations rather than invented user questions. This empty
list does not resolve any open gameplay decisions in the ledger or authorize
unbounded future scope.

`baseline_artifacts: []`

No product baseline is drafted or frozen during this setup. The game requirements
and historical accepted decisions remain their own authorities.

## Actual verification and limits

This agent inspected the actual local source and setup assessment/selection. The
assessment is `kind=setup`, `architecture_change=true`, `research_required=false`,
and has no milestone. Its required set includes ENG-00, KRA-14, WIK-30, EVC-28,
IA-11 and DOC-10. Source inspection, rather than a runtime test, supports the
descriptions of inherited validation above.

Ran `python3 tools/build_game_wiki.py --check` from the game root: exit 0,
2246 HTML pages, all local links/anchors, 141 C# files and current source/evidence
hashes at this pre-edit snapshot. This is wiki freshness validation, not gameplay
testing or verification of later specialist edits; output was observed through
the host command receipt rather than saved as a separate local log by this role.

No game runtime, MCP protocol, acceptance, dispatch mutation, test suite or Git
publication was performed by this assignment. Later implementation can change
the inspected files; QA and IA must check the actual final code and evidence.
The assigned report is the only file this specialist writes.
