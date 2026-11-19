# Agentic software-development loop

Status: implemented tooling and active policy following the user's November 11, 2026 request. This supersedes the single-developer execution default in [the earlier integration](58-software-project-management.md). The game remains its own Git repository in `Project/`; no gameplay or milestone gate is changed. Setup/recovery acceptance is a process outcome recorded by independent IA and Document Chief, not inferred from these documents.

## Authorities and source ownership

- [Management tasks](../management/README.md) own bounded scope, dependencies, acceptance, context reading and command evidence.
- `management/agentic/` owns append-only registry revisions, exact role selection, dispatch authorizations, actual external receipts, handoffs, interruption, evidence packages, findings and immutable process publications.
- [The actual game wiki](../source_material/game-wiki/agentic-loop.html) owns connected explanations of mechanics, adopted SCP lore, implementation and limits. It is not the harness's JSON process publication store.
- Applicable `docs/`, ADRs, source/data/assets and tests retain their existing authority. The excluded bulk canon archive remains an independent reference. Process knowledge is never automatically character, OSCP or AIC knowledge.

Source changes belong to explicitly assigned paths; role-owned handoff artifacts and logs belong to `management/agentic/work/<turn>/<role>/`. Cross-role implementation changes require an explicit owner handoff. The runtime records workflow but cannot authenticate a host worker, force correct source ownership or replace human review.

## Every registered role is considered every turn

The versioned seed `management/harness/runtime/config/game_roles.json` registers 23 roles. A turn freezes its actual registry; do not assume future registry revisions have exactly 23. Start every registered role at YES. Selection must account for **every frozen registered role exactly once**. Each NO requires its own substantive, unique, task-specific reason, including Vince and every unused software/media role. Omission, blanket exclusion, duplicate keys and deleting unused roles are not negation. Register newly needed specialties before selection.

Mandatory roles are ENG-00, WIK-30, EVC-28, independent IA-11 and DOC-10. A registered role does not mean it runs every turn. Other roles are selected for the actual scope. Every selected role needs an individual rationale, exact prompt, dependency list, owned directory, prior authorization and distinct actual host receipt. Runtime receipt strings are externally reviewable assertions, not authenticated host identities.

| Role | Software/game responsibility |
|---|---|
| ENG-00 — Coordinator | Scope, acceptance, role decisions, exact assignments and interfaces |
| KRA-14 — Architecture | Architecture boundaries, ambiguity and provisional contracts |
| SWE-13 — Software | Application, automation, tooling and local APIs |
| SIM-34 — Simulation/modules | Authoritative kernel, SCP capabilities, time, perception, inventory and saves |
| QA-16 — Test engineer | Independent appropriate positive/negative acceptance checks |
| OPS-17 — Build/delivery | Builds, packaging and authorized deployment procedures |
| SEC-18 — Security | Assigned permissions, data, sandbox and privacy boundaries |
| RES-09 — Researcher | Source-backed unfamiliar technical choices, alternatives and uncertainty |
| LOR-33 — SCP lore/adaptation | Canon and cross-article evidence, contradictions and explicit adaptations |
| UIX-31 — Native UI/UX | Godot controls, information, feedback, accessibility and interaction flows |
| WEB-06 — Web interface | Wiki/web renderer, navigation, search, layout and accessibility implementation |
| MED-07 — Illustration | Approved sprite/texture/icon and other 2D source artwork |
| AST-32 — Asset pipeline | Sprite states, atlases, masks, bindings, imports and provenance integration |
| DEM-08 — Animation/demonstration | Approved sprite motion and truthful runnable demonstrations |
| AUD-25 — Audio | Approved speech, sound and music assets |
| WRI-22 — Narrative/editorial | Assigned dialogue, narrative and story continuity |
| PRT-05 — Prototype designer | Concrete available-artifact plan and checks at a declared milestone |
| BLD-27 — Prototype builder | Attempt that exact approved milestone plan and record actual blockers/results |
| SHO-12 — Vince Offer | Truthful milestone presentation; requests specialist media without inventing success |
| WIK-30 — Game wiki maintainer | Actual authored game wiki, related technical docs, generation and semantic consistency |
| EVC-28 — Evidence collector | Inventory selected work, nested references, hashes, gaps and actual logs |
| IA-11 — Internal Affairs | Independent audit of assignments, order, exclusions, evidence and claims |
| DOC-10 — Document Chief | Publish the reviewed process outcome last; no post-IA game/wiki rewriting |

Lore and research can collaborate: RES owns queued unfamiliar external-choice research; LOR owns SCP evidence/adaptation analysis. MED authors art; AST integrates approved assets. WEB implements web presentation; UIX designs native gameplay interfaces; WIK explains and verifies the actual game knowledge base. Select overlapping specialties only with distinct owned scopes and handoffs.

## Ordered execution

1. Read game agent rules, board, implementation ledger and specific relevant wiki/source/design evidence. Check freshness; define a small task and assessment. Record actual questions or an explicit scoped empty list. A setup task is not a gameplay milestone.
2. Initialize/read the genuine registry and readiness, draft all-YES selection, then record every selected role and every individual NO. Freeze exact prompts/dependencies before dispatch.
3. **Authorize before spawning** each independent host worker. Record its actual host receipt after spawning with the exact approved prompt. The local CLI/MCP neither spawns workers nor creates authentic host receipts. Never backdate or fabricate an invocation; a failed or unavailable worker is BLOCKED/FAILED, not a successful handoff.
4. Working owners implement and qualify their assigned scope, then submit structured handoffs with real tests, artifacts, limitations and status. Requests for unfamiliar research stop the requesting worker for the remainder of that turn; a later Researcher invocation returns sources for acceptance before the waiting work resumes. Questions, research and confirmed findings survive interruptions.
5. **WIK-30 follows every selected execution owner.** It updates actual authored game-wiki Markdown plus affected technical documentation, builds/checks the wiki, inspects pages and reciprocal links, and reviews semantic agreement with implementation and actual tests. It supplies existing authored page paths, real build/check evidence and `semantic_review=true`. Specialist dispatch stops once Wiki begins. Do not hide a discovered late defect with an unreviewed source repair; carry corrections through a new authorized turn.
6. **EVC-28 → independent IA-11 → DOC-10**, in that order. Collector inventories the complete package and omissions without repairing it. IA audits genuine dispatch/evidence/order and records findings without silently fixing reviewed work. DOC publishes accepted, rejected or blocked process outcomes last. No agent runs after DOC in that turn. Rejected work cannot be promoted by documentation prose.
7. Complete the software task only with its matching accepted immutable turn publication, matching task entity, all selected task handoffs, actual Wiki handoff, independent IA receipt, actual tests/docs/wiki checks and final semantic review. Review the Git diff and preserve unrelated work. CLI commands do not commit, push or deploy.

Declared completion milestones require Designer and Builder; an ordinary tooling setup is not such a milestone. A build plan alone is not a completed runnable product. The inherited strict fresh-project setup/questions/research gates remain available for new projects; this game's explicit import is described below.

## Existing history and quota recovery

`management/agentic/imported-project.json` pins immutable copies of accepted commit `2a86ca5` documents and actual user authorization. It imports existing design/process history only. An import file alone does not unlock production: accepted setup publication must explicitly publish `project.import` with that exact hash and record evidence. Historical question-review, initial-research and baseline booleans remain truthful; no artificial historical worker rounds are invented. New unanswered questions, pending research and confirmed IA findings still block production.

Turn.001 stopped with actual quota failures and is recorded INTERRUPTED. Its partial outputs and adverse logs remain evidence, not accepted setup. Turn.002 is also INTERRUPTED: independent IA rejected setup with confirmed MAJOR SWE-13 case IA-002-001, and DOC then hit an actual usage limit before producing a handoff/publication. The subsequent real host failure call recorded the event but returned an MCP dictionary-schema error; read-back confirmed the append. Do not replay that production mutation.

Turn.003 owns the two bounded corrections and preserves the original logs, rejected audit and failed turns. Its independent QA evidence is current; collection, IA, DOC and task/import acceptance remain pending at this WIK handoff. Consult the immutable runtime publication for later disposition. M0 remains open, and historical gameplay tests are not fresh qualification of this tooling change.

## Local tools, initialization and connection limits

The copied genuine portable engine is versioned under `management/harness/runtime/`, with original hashes and scoped patches in `UPSTREAM.json`. `tools/game_workflow.py` maps its state into the game and adds real-Wiki/import/selection policy. `tools/agentic.py` and stdio `tools/agentic_mcp.py` share 22 operations, including init, inventory, status, plan/select, authorize/invoke, handoff, collection, audit, publication, interruption, questions and research. Local pinned dependencies are `mcp==1.30.0` and `jsonschema==4.26.0`; no model API, account purchase or network listener is needed.

From the game root:

```sh
python3 tools/install_agentic_dependencies.py
.venv-harness/bin/python tools/agentic.py init
.venv-harness/bin/python tools/agentic.py inventory
.venv-harness/bin/python tools/agentic.py status
.venv-harness/bin/python tools/check_agentic_mcp.py
python3 tools/configure_agentic_client.py --workspace ..
```

Initialization is idempotent; it records actual registry revisions rather than fabricating worker runs. Mutations take exact named arguments from `--input path.json`; retain exact prompts/arguments and actual outputs. Read `tools/agentic_mcp.py` descriptions and the runtime schema instead of guessing dispatch parameters. `tools/call_agentic_mcp.py` can perform genuine local stdio tool calls before the host reloads; it does not establish a live desktop connection.

The actual local stdio probe initialized protocol 2025-11-25, listed 22 tools and called inventory for 23 roles. Setup also recorded real initialization/logging/handoff calls through that protocol. A real Codex-exposed `workflow_readiness` call succeeded at 2026-11-11 16:42:06 UTC; `management/agentic/bootstrap/host-connection-observed.json` retains that external observation. Earlier reports of CONFIGURED_UNVERIFIED remain historical. The local runtime always returns generic `host_mcp_connection: NOT_OBSERVED` because it cannot inspect its external transport; client configuration checks also do not inspect host loading. Those fields cannot negate the observed host call or authenticate every worker. Local READY covers CLI prerequisites and can coexist with `stdio_mcp_dependency: false`; actual MCP execution requires the pinned harness interpreter.

The [installed-wrapper template](../management/harness/manage_game.py) now establishes the validated game tools import path before `runpy` dispatch. The [shared dispatcher](../tools/agentic.py), [local readiness policy](../tools/game_workflow.py) and [entrypoint/real-MCP regressions](../tests/test_agentic_entrypoints.py) provide the corresponding implementation and verification sources. From this game root, `python3 ../scripts/manage_game.py agents readiness` and legacy `status` work; the wrapper is also qualified from the outer root and unrelated cwd. Shared `tools/agentic.py` failed dispatch returns a dictionary with original arguments, `status: FAILED` and `recorded: true` only after the genuine runtime append succeeds. Invalid, duplicate and sealed-turn operations remain errors without an extra FAILED event. Fresh real-MCP fixture checks verify that acknowledgement using explicitly synthetic records, without replaying the production mutation. They do not demonstrate resident host hot reload or a corrected host-exposed failure reply.

## Verification and honest limits

Turn.003 independent QA recorded 40 agentic/client/entrypoint tests plus 13 historical-adapter tests (53 distinct total); the five new entrypoint regressions are included in the 40, not added again. Earlier 48-test reports and the initial real local MCP probe remain historical. They exercise all-role accounting/negation, authorization, exact prompts, distinct receipts, dependencies, Wiki/terminal ordering, interrupted work, truthful import and matching accepted-publication completion. Fixtures label synthetic receipts explicitly; they are never production dispatch evidence. Evidence hashes detect ordinary changes, not deliberate rewrite or correct test meaning. Reading receipts attest reading, not cognition.

The setup does not exhaustively qualify later research-resolution/IA-resolution combinations, native gameplay, art or canon changes. WIK supplies fresh wiki evidence separately. Consult the selected turn's owned reports and immutable publication for actual outcome; no process guide by itself means acceptance. See [the management guide](../management/README.md), [wiki loop page](../source_material/game-wiki/agentic-loop.html), [wiki maintenance](../source_material/game-wiki/development.html) and [Git workflow](12-development-workflow.md).

