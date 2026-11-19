# Architecture recovery review — turn.002 / KRA-14

Assigned status: **COMPLETE** for this bounded setup architecture review. This is
not IA acceptance, a completed product baseline, or a gameplay milestone. This
role writes only this report, with no runtime or canonical-doc edits.

## Inspected context and actual recovery

Re-read game `AGENTS.md`, management README/profile, actual game-wiki sources
`content/project-management.md`, `content/development.md` and `content/status.md`,
the turn.002 ENG-00 report, setup assessment/selection context, and current
`tools/game_workflow.py`, `tools/agentic.py`, `tools/check_agentic_mcp.py`, role
definitions and the copied runtime's relevant workflow/preflight sections. The
linked integration/development/wiki authoring documents and full initial
architecture report read during turn.001 remain relevant local sources; this
focused recovery does not pretend to perform a new product-research round.

Actual persisted phase inspection shows turn.001 **INTERRUPTED**, with 18 roles in
its snapshot; turn.002 is **RESEARCH_AND_EXECUTION**, with 23 roles. Retain the
earlier quota failures and partial work. No accepted setup/import may be inferred
from the earlier report, source files, installed dependencies or interrupted turn.

The parent reports actual quota errors in engineering/QA workers. This report
does not independently diagnose the service quota or claim those workers finished.
New authorized invocations and final checks must qualify the resumed files.

## Architecture confirmed

The turn.001 recommendation still applies: reuse the genuine copied runtime's
registry, exact all-role selection, authorize-before-invocation receipts,
hash-linked events, structured handoffs, phase order and immutable publications.
Keep one shared API behind CLI/MCP and preserve the game's independent Git root,
wiki, source, tests, accepted history and excluded canon archive.

Retain distinct authorities: task board for bounded slice scope/acceptance;
`management/agentic/` for execution/review history; the **actual authored game
wiki** for mechanics/implementation/lore explanations; applicable `docs/` for
contracts and historical decisions. Process publication JSON never replaces the
game knowledge base or implies in-world character/AIC knowledge.

The current adapter already implements several of the requested boundaries:

- Mandatory ENG-00, EVC-28, IA-11, DOC-10 and WIK-30; all registered roles must be
  explicitly selected or individually negated through the inherited validator.
- WIK-30 depends on every selected execution role, and later specialist dispatch
  is refused once Wiki starts. Wiki has real authored-page/build/check/semantic
  evidence requirements before collection.
- Each role needs a distinct externally supplied host receipt. Receipts and
  reading attestations remain externally reviewable claims, not host-authenticated
  guarantees produced by Python.
- Imports retain actual sources/authorization hashes and require an accepted
  setup publication naming that exact import. Historical question/research/
  baseline booleans remain truthful. Pending questions, research and IA cases
  still block production readiness.
- CLI readiness is explicitly separate from host MCP loading. The real local
  stdio probe tests initialize/list-tools/inventory and labels live host
  registration CONFIGURED_UNVERIFIED until a host call is observed.

The earlier two concrete architecture hazards are addressed in the inspected
source: `Workflow.select` now uses `self.readiness`, and inherited later research
eligibility calls `initial_prerequisites_ready`. GameWorkflow can therefore honor
an accepted import without fabricating `initial_research_complete`. QA must still
exercise the actual positive and rejected import/research cases.

## Concrete remaining integration defect at inspection

Collector package creation returns an alias path such as
`work/turn.002/EVC-28/evidence-package.json`. The game path mapper resolves this
under `management/agentic/work/`. `references` records the canonical game-relative
path, but `_validate_collection` currently compares `r['path'] == path` literally.
Those two spellings identify the same file but fail string equality.

SWE should fix this narrowly through canonical game-relative return paths or
resolved-path comparison, retaining the owned-directory containment check and
recorded artifact hash. QA should run the genuine collect → handoff sequence
through GameWorkflow, not only unadapted generic runtime fixtures. Do not bypass
ownership, omit the package hash or accept arbitrary unrelated evidence to make
the comparison pass. Parent was notified of the exact mismatch.

## Software-role scope and negation

The current registry includes 23 roles. Every unused one requires its own
substantive task-specific NO; all start YES for consideration. Retain registered
roles across turns even when this setup does not need their work.

The newly explicit software/game scopes are sensible:

- MED-07 creates approved sprite/texture illustration; DEM-08 covers authored
  animation/demonstrations; AUD-25 covers sound/music; AST-32 owns atlas/mask/import/
  placement/provenance integration. Asset authoring and integration are distinct.
- WEB-06 owns actual HTML/CSS/search/accessibility/web interaction, while WIK-30
  owns technical game-wiki content, source relationships and routine generation.
- UIX-31 owns native Godot interaction, feedback and accessibility, rather than
  treating every native game view as a website.
- LOR-33 examines SCP canon/cross-archive contradictions and explicit adaptation
  proposals; RES-09 researches unfamiliar external dependencies/approaches and
  preserves source receipts. They do not silently make disputed human decisions.
- SIM-34 owns authoritative kernel/module/time/perception/inventory/employment/
  persistence contracts, separate from general tooling implementation.

These roles need no fictional deliverables this setup. ENG/KRA/SWE/QA/WIK and the
terminal crew cover the current task. Vince, unused software/media/lore specialists
and prototype roles remain individually negated. Setup is not a declared gameplay
milestone; ordinary milestone Designer/Builder requirements remain for later
actual milestones.

## Finalization conditions and documentation ownership

Canonical docs still describe single-developer execution at inspection. Treat
that as incomplete integration requiring WIK-30's superseding current-policy
update, preserving the old accepted self-review records as historical evidence.
Do not let stale current-policy text justify skipping the user's agentic loop.

After SWE and QA final handoffs, WIK must maintain the actual wiki/linked Markdown,
rebuild/check and inspect generated semantic agreement. Then EVC inventories and
hashes actual logs/outputs/receipts/gaps, independent IA reviews the final evidence,
and DOC publishes the accepted/rejected/blocked process outcome last. No source
or game-wiki repair should be hidden after review. Findings require responsible
owner correction records; interruptions remain incomplete.

Accepting imported history requires actual setup acceptance/publication, not a
synthetic architecture.baseline or backdated questions/research. Keep later
questions, queued research, findings and readiness gates operational. The game
ledger's existing open decisions and M0 limitations remain unchanged.

## Explicit disposition and verification limits

`unresolved_questions: []`

Coverage: recovery/quota interruption, current all-role registry and negation,
software asset/media/web/native-UI/lore/research ownership, game/wiki/process
authority, accepted imports versus historical booleans, later research eligibility,
collection path containment, actual receipts and terminal phase order. No new
user-design answer is necessary for bounded setup. The collector mismatch is an
implementation defect for SWE/QA, not an invented user ambiguity.

`baseline_artifacts: []`

Only actual local read-only source/JSON inspection was performed. This role did
not run gameplay tests, a new wiki build/check, protocol probe or acceptance, and
does not reuse turn.001's freshness pass as fresh recovery verification. WIK/QA/IA
must record final real results after the resumed implementation. This report's
COMPLETE status describes its assigned review only.
