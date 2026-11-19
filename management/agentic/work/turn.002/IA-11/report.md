# Independent Internal Affairs review — turn.002 / IA-11

Disposition: **REJECTED for setup acceptance**. One independently confirmed MAJOR implementation defect remains. The actual source-owner, Wiki, Collector and independent-review loop ran and preserved the defect; that honest process does not make the failing entry point complete. The overall `agentic-loop-setup` task must remain active. No import, task or gameplay acceptance is granted here.

## Scope and independent work

Read game AGENTS, management README/profile/board, actual authored wiki development/status/project-management/agentic-loop pages, wiki authoring/agent-entry guidance, docs59 and linked development/history evidence. Inspected actual GameWorkflow, copied handoff/collection/audit/publication/preflight code, CLI/MCP dispatch, task completion guards, wrapper, scoped roles, recorded prompts/events/handoffs, tested-source fingerprints, import provenance and real test/wiki/protocol logs. This is a distinct `/root/ia_setup_review` host worker; it did not author the reviewed implementation or silently repair it.

Own output is restricted to this IA directory. `verify_audit.py` reconstructs the genuine turns and checks current hashes, exact prompts, recorded authorization order, dependencies, collection head, task check inputs and imported commit snapshots. It changes no source, wiki, task, registry, or process disposition. The parent must record this raw handoff and audit through the actual runtime serially.

## Confirmed finding IA-002-001

**MAJOR; responsible owner SWE-13; CONFIRMED and independently verified.**

The installed outer `scripts/manage_game.py agents readiness` fails with exit1 and `ModuleNotFoundError: No module named 'game_workflow'`. Independently reproduced from both the outer workspace and unrelated `/tmp`, using the wrapper's absolute path. The direct game-root `.venv-harness/bin/python tools/agentic.py readiness` passes; the outer legacy `status` route also passes. Thus this is the agents shortcut's import integration, not a missing game runtime or a general board failure.

The versioned wrapper calls `runpy.run_path` for `tools/agentic.py`, whose `from game_workflow import GameWorkflow` needs the game tools import path. The wrapper never establishes it. Exact failing source bytes, WIK failure and Collector adverse snapshots are preserved. WIK correctly found this after source freeze and documented it instead of making an unreviewed repair. Earlier SWE/QA no-known-defect statements are historical pre-WIK observations and are explicitly superseded by this concrete finding.

Required next authorized-turn action: SWE fixes the versioned wrapper's import setup, reinstalls the outer managed copy, and supplies a regression covering agents dispatch from both the outer root and unrelated cwd while retaining the legacy board route and direct CLI. QA independently reruns those checks; WIK refreshes affected current documentation and real wiki build/check after the fix; Collector/IA/DOC perform the ordered terminal review again. Include `IA-002-001` in the responsible selection's corrective cases. Do not close the software task, accept the import, or claim the shortcut works before the correction passes. No source repair is permitted in this frozen turn.

## Role selection, dispatch and ownership

The actual frozen registry has 23 roles, exactly 8 selected and 15 individually excluded. All roles are accounted once, with substantive distinct task-specific NO rationales. Media creation, asset integration, web presentation, native UI/UX, lore, research and simulation have separate software/game scopes; no manufacturing crew is imported by default. Exclusions match this tooling-only scope, including retained Vince and prototype roles. This is not a declared gameplay/product completion milestone, so no fictitious Designer/Builder attempt is required or claimed.

Actual recorded authorization/invocation sequences are ENG5/6, KRA8/9, SWE11/13, QA12/14, WIK17/18, EVC21/22 and IA25/26. Tickets and exact saved prompts/hashes match, receipts differ by role, and dependency handoffs precede dispatch authorization. Source-owner handoffs finish before WIK; WIK depends on all four and no new execution specialist starts after it. EVC handoff precedes IA. DOC is intentionally still pending; DOC must publish the rejected result last, not turn this review into acceptance.

These are independently checked recorded-order facts. Present host inventory corroborates root, QA, EVC and this IA; earlier completed ENG/KRA/SWE/WIK workers are not all retained in that current inventory. Parent context and role reports retain actual dispatch assertions, but Python cannot authenticate external receipt strings or prove exact host spawn timing from records alone. No cryptographic host-authentication claim is made.

## Evidence completeness and historical differences

The genuine Collector package indexes 71 current references and has no missing execution invocation/handoff, indexed integrity issue or recorded turn.002 worker failure. Its collected head matches actual event22. Its supplementary inventory includes adverse wrapper snapshots, real logs and earlier failures. Independent verification checked 219 current reference assertions (including EVC's own handoff), all matching, QA's final source hashes, and all four task check records with 39 unchanged declared inputs. Both turns reconstruct through actual hash-chain validation.

Six differing hashes are the task's explicitly historical 10:29:49 `wiki_read`/`docs_read` attestations, preceding the current source/wiki changes. They are preserved separately from final check inputs. They do not claim current source freshness or fresh tests; no mismatch is excused in the current evidence. Collector retained its initial too-broad hash-verification FAIL and classified those six transparently. IA accepts that historical interpretation rather than rewriting reading history.

The immutable import's four delimiter-named snapshots exactly match `git show 2a86ca5` for their actual source paths; its local README is a provenance note, not a snapshot of the game README. Import hash is `cf12434737bb7519349565abfe1d415aee97dda8cbf3fc71af22394b79b3a9b1`. Its authorization excerpt matches the actual human instruction in this conversation; it expressly labels the existing-project import as scoped coordinator policy, not separately answered human preflight. This migration is reasonable for the already developed game and does not grant current acceptance. Historical question/research/baseline flags remain false, import acceptance false and production false. New questions/research/findings remain gates; M0 stays open.

Turn.001 is preserved INTERRUPTED with SWE/QA failure events and no accepted publication. Its quota attribution is parent-recorded, not independently service-authenticated. Failed first WIK RPC logs remain alongside corrected v2 calls; their generic TaskGroup text alone does not prove the parent-described envelope cause. They did not produce a false successful handoff event. Tracked diffs show no changed native source/assets, canon or GitHub workflow.

## Actual checks and game-wiki agreement

Independent reruns in this IA invocation:

- `python3 -m unittest discover -s tests -p 'test_agentic*.py' -v`: **PASS, 35 tests, 23.914s**.
- `python3 -m unittest discover -s tests -p test_project_management.py -v`: **PASS, 13 tests, 0.442s**.
- `python3 tools/check_management_docs.py`: **PASS**, five Markdown sources and task-record integrity.
- `python3 tools/build_game_wiki.py --check`: **PASS**, 2247 HTML pages, local links/anchors and source/evidence hashes; 141 C# files. IA did not perform a new build; WIK's actual prior build/check logs and stable final inputs were independently checked.
- WIK's read-only generated-page inspection rerun: **PASS**. Manual review compared current authored/generated loop descriptions, reciprocal relationships, roles, source ownership and limitations against actual source/events/results.
- Owned read-only `verify_audit.py`: **PASS**, 219 current hashes, 23-role coverage, exact prompt/ticket/recorded order, 39 stable check inputs, imported commit snapshots and truthful readiness history.
- Outer agents shortcut: **FAIL** twice, as the confirmed finding. Direct agent readiness and legacy board: **PASS**.

The first IA verifier attempt mistakenly compared the local import provenance README against the game's historical README. Its actual FAIL log is retained as `initial-verifier-scope-error.log`; the owned checker was corrected to compare only the four explicitly named snapshots while independently hashing the provenance note. This was an IA-checker scope mistake, not a repaired production defect or fabricated successful first run.

QA/SWE logs also show the genuine local stdio probe initialized protocol2025-11-25, listed22 tools and invoked inventory for23 roles. Actual parent RPC receipts support local initialization/logging/handoffs. IA inspected this source/provenance but did not rerun a separate MCP server. No actual host-exposed `workflow_*` service appears in this IA tool inventory. Local protocol PASS plus installed configuration therefore remains **CONFIGURED_UNVERIFIED** for the Codex host; that stated external loading gap is not silently upgraded.

Actual WIK authored content, docs59, superseding historical-doc links, generation, semantic and 39-input check evidence are substantial and accurate about the current failure. WIK's COMPLETE means its assigned documentation work is complete, not that the whole setup is accepted. Process JSON remains separate from the real game wiki. Successful hashes/tests/document checks cannot erase the failing runtime entry point.

## Required terminal outcome and limits

DOC must publish **REJECTED**, preserve `IA-002-001` for SWE-13 in the next authorized setup/corrective turn, and leave task/import acceptance incomplete. No worker may run after DOC within this turn. Source/wiki fixes, fresh acceptance and eventual completion belong to that next reviewed turn.

No fresh gameplay, native rendering, SCP canon, art, full research-resolution cycle or exhaustive finding-resolution qualification is asserted. The 48 passing harness tests overlap earlier QA/WIK suites and are not additive gameplay coverage. Hashes detect ordinary modification, not deliberate rewrite, receipt authenticity or semantic correctness. Declared-input checks cover their stated files, not every environmental dependency. No new material design ambiguity needs a human answer for the bounded correction.
