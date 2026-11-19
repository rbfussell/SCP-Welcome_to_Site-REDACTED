# Evidence collection — turn.002 / EVC-28

**COMPLETE collection; no IA acceptance or task completion asserted.** The genuine runtime package has 71 indexed references, no missing execution invocations/handoffs, no recorded turn.002 invocation failures and no indexed integrity issues. The functional outer-wrapper failure remains an explicit adverse result. Independent IA and Document Chief have not run at this collection snapshot.

## Scope, reading and actual collection

Read game AGENTS, management README/profile, actual authored wiki development/status/project-management/agentic-loop pages, linked docs58/59 and wiki authoring guide. Inspected the game policy/CLI, genuine copied runtime collection/handoff/hash-chain behavior, frozen registry/selection, source-owner reports, nested source snapshots/import, actual test/document/wiki logs and both turns. The task board was read through the outer wrapper from the game root; the setup task remains active.

Ran `.venv-harness/bin/python tools/agentic.py collect --input management/agentic/work/turn.002/EVC-28/collect-input.json --output management/agentic/work/turn.002/EVC-28/collect-result.json`. This invoked the real `GameWorkflow.build_evidence_package`, not a hand-authored replacement or a host MCP call. The package is `evidence-package.json`, SHA-256 `7cd8e9a6863c269aa296f9d46be4d9ad94460e2ad3bdcaa53393b48154ed89b7`. Its collected event head matches the actual turn.002 head at sequence 22 (`b382665e4267f989200909f817fdea3c65a6ec2729787a9cd83210d8bd45e393`). Runtime turn reconstruction verified the ordinary hash chains for both turns.

The owned `verify_collection.py` is a read-only supplemental inventory. It inspected 676 nested/direct hash assertions over 92 unique paths, plus 159 observed supporting files. 670 assertions matched. Six differing assertions are explicitly historical task reading-context hashes, described below. There are no missing/current indexed references or missing current nested snapshots/logs. The exact checked references, origins, actual hashes, role assignments and observed files are in `inventory.json`; repeated receipts are not counted as separate coverage.

## Every frozen role accounted exactly once

Frozen registry revision 19 contains 23 roles: 8 YES and 15 NO, with no omitted/extra role and 15 distinct exclusion reasons. Selected roles are DOC-10, ENG-00, EVC-28, IA-11, KRA-14, QA-16, SWE-13, WIK-30. Their exact prompts, prompt hashes, dependency lists and individual rationales are retained in selection/events and copied into `inventory.json`. Setup declares no gameplay milestone, so no prototype plan/attempt is represented as performed.

| Individually excluded role | Exact task-specific reason |
|---|---|
| AST-32 | No sprite atlas, mask, art binding, import settings or asset provenance changes are made by this workflow recovery. |
| AUD-25 | No speech, sound effects, music or audio pipeline changes are requested by this workflow initialization. |
| BLD-27 | No product milestone or approved Designer prototype plan is declared; software and QA qualify the harness tools directly. |
| DEM-08 | No animation sequence or digital product demonstration is authored during the agentic workflow setup. |
| LOR-33 | No SCP canon claim or in-game lore adaptation is introduced in this agent-loop setup; prior game lore remains unchanged. |
| MED-07 | This initialization changes no sprite illustrations, textures or graphic artwork, so there is no illustration deliverable. |
| OPS-17 | No game packaging, release pipeline or operational deployment changes are included; SWE owns this bounded harness CLI/MCP setup. |
| PRT-05 | No game completion milestone is declared; this is setup qualification of workflow services, not a new product prototype plan. |
| RES-09 | No unfamiliar product/material choice or game-canon decision is made; this setup reuses inspected local harness sources and pinned dependencies. |
| SEC-18 | No new game sandbox, credential handling or public service is introduced; IA reviews the local subprocess and evidence boundaries for this setup. |
| SHO-12 | No sales or milestone presentation is requested in this harness initialization; truthful setup evidence is the deliverable. |
| SIM-34 | No authoritative game kernel, SCP module, time/perception rule or save contract changes are requested by the harness recovery. |
| UIX-31 | No player-facing Godot screen, control flow or interaction usability is changed during harness initialization, so native UI/UX design has no deliverable. |
| WEB-06 | The recovery changes no wiki HTML/CSS/search interface; WIK-30 maintains technical content and existing generation, so no web-interface implementation is assigned. |
| WRI-22 | No character dialogue, narrative or canon adaptation is authored; WIK-30 owns technical game documentation. |

## Actual dispatch and ownership inventory

| Role | Authorization event | Invocation event | Handoff event | Recorded host receipt |
|---|---:|---:|---:|---|
| ENG-00 | 5 | 6 | 7 | `/root/coordinator_setup` |
| KRA-14 | 8 | 9 | 10 | `/root/architecture_setup` |
| SWE-13 | 11 | 13 | 16 | `/root/software_harness` |
| QA-16 | 12 | 14 | 15 | `/root/qa_harness` |
| WIK-30 | 17 | 18 | 19 | `/root/wiki_agentic_setup` |
| EVC-28 | 21 | 22 | pending | `/root/collector_agentic_setup` |

Each recorded authorization precedes its invocation, tickets match, exact saved prompts match selected text and hashes, dependencies have earlier complete handoffs, and recorded receipt strings are distinct. ENG/KRA/SWE/QA completed before WIK authorization; WIK has all four dependencies and its complete handoff precedes collection. No selected execution worker was invoked after WIK. EVC's own handoff is pending submission at this snapshot; IA and DOC are intentionally pending terminal roles, not silently omitted execution work.

The five execution roles have 39 indexed owned artifacts and 42 actual owned files. The extra three are QA/SWE/WIK raw handoff JSON files; they are independently hashed in the supplemental inventory, while canonical handoff values already exist in actual turn events. All submitted artifact paths lie inside their declared owner directories. The package includes actual code/test/doc/wiki source evidence and all test references; nested QA tested-source snapshots, WIK 39-input check snapshots/logs, imported historical documents and actual authorization references were additionally traversed and verified.

Current `collaboration.list_agents` corroborated root, this collector, completed QA and completed WIK. ENG/KRA/SWE are no longer present in that live inventory, so their recorded receipts are retained as externally reviewable assertions. A runtime log and distinct nonempty strings cannot authenticate the earlier host spawns. The parent retains the actual dispatch context; IA must assess it independently. `host-agents-observation.json` records the status-only observation.

## Actual results inventoried

QA's real logs show 35 agentic/client tests plus 13 adapter tests passing (48 total), plus local stdio protocol initialization/tool listing/inventory for 22 tools and 23 roles. WIK's later overlapping 35-test PASS is fresh documentation-stage evidence, not 35 additional distinct tests. WIK logs show documentation PASS (five managed Markdown sources; supplementary 78 documents / 104 acceptance IDs), wiki build/check PASS (2247 HTML pages, 141 C# files, 13 SCP entries, 15 systems, 1535 members and 235 types), semantic/reciprocal-link inspection PASS and template install/parity PASS. ENG/KRA accurately mark tests NOT_RUN. Collector inspected these logs and hashes; it did not rerun gameplay or replace specialist test meaning with an exit code.

The outer `python3 ../scripts/manage_game.py agents readiness` is a real FAIL: `ModuleNotFoundError: game_workflow`. This functional failure differs from turn.002's empty invocation-failure map. WIK's failure log and `bootstrap/root-wrapper-defect.json` remain evidence. Exact current adverse wrapper bytes are preserved as `broken-wrapper-template.py.snapshot` and `broken-wrapper-installed.py.snapshot`; both match their live files at collection, SHA-256 `6e70dcdfd821a0bf17fa68f9c6425c8afcb7e6fe208dd0e6492c33bf8ad13b3a`. No source repair was made. IA must disposition the defect; SWE-13 owns the proposed new authorized correction/regression turn.

## Explicit differences and remaining evidence limits

- Six task context hashes recorded at 10:29:49 differ from current WIK-edited files: status, development, engine, wiki README, AGENTS and docs 12. Their context explicitly says reading attestation; it predates source changes. Current WIK handoff/check inputs and QA's tested-source snapshot match. `initial-verification.log` and `initial-inventory.json` preserve the broad verifier's actual FAIL when it initially required every historical reading hash to match current files. The final verifier classifies but retains all six differences, performs current-reference checks and passes. Collector did not rewrite reading history; IA must assess the interpretation.
- Turn.001 reconstructs as INTERRUPTED with actual recorded SWE/QA quota failures, 18 events and no accepted publication. Partial artifacts/adverse logs remain in the supplemental inventory. The quota assertion is the parent's recorded host outcome; no raw quota-service error response is attached, so stronger service-level proof is not claimed.
- First WIK handoff and collection-phase RPC attempts retain generic TaskGroup FAIL logs; corrected v2 calls succeeded and actual events show no failed first handoff state mutation. The detailed double-envelope cause is parent explanation, not independently established by the generic error string.
- Local direct CLI and real stdio probes are evidenced, while desktop host workflow tools remain unobserved: CONFIGURED_UNVERIFIED. Parent local IPC failure/interruption and later successful out-of-sandbox protocol evidence remain visible. This collector used direct CLI and did not claim a new host connection.
- The existing-project import hash is `cf12434737bb7519349565abfe1d415aee97dda8cbf3fc71af22394b79b3a9b1`; its nested sources/authorization match, but acceptance remains pending. Historical question/research/baseline flags remain false, production remains blocked and M0 remains open. No new game/canon/art/UI/native-runtime qualification is asserted.
- Python hashes detect ordinary changes, not deliberate evidence rewriting or host authenticity. Historical upstream pre-patch hashes retain their explicit provenance meaning. Collection does not decide IA acceptance, publish process outcomes, close tasks, repair code/wiki, commit or deploy.

Independent IA now owns acceptance/findings. DOC must publish the actual process outcome last, with no later worker in this turn. The known wrapper correction belongs to a separate authorized turn.
