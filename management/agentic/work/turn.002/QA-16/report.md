# Independent test-engineering report — turn.002 / QA-16

Assigned result: **COMPLETE** for the bounded harness verification scope. This is
specialist QA evidence, not Internal Affairs acceptance, process publication,
complete game qualification or confirmation that the desktop loaded an MCP.

## Reading and scope

Read game `AGENTS.md`, management README, actual game-wiki authored development,
status and project-management pages, integration contract `docs/58-software-project-management.md`,
and turn.001 plus recovery turn.002 ENG-00/KRA-14 reports. Inspected copied
workflow/preflight sources, game policy/CLI/MCP code, software board completion
code, role definitions and client setup tests. The later user request supersedes
the older single-developer default; the actual wiki workflow remains mandatory.

Own changes are `tests/test_agentic_workflow.py` and this QA report/evidence folder.
No game implementation, wiki content, canonical process events, registry, task
record, Git history or canon archive was changed by QA. Temporary-root fixtures
exercise the real policy methods with explicitly synthetic worker receipts and
logs. Those receipts are never production dispatch, real user-message or
independent production-review evidence.

The parent recorded turn.001 interrupted after quota failures. Earlier partial
work and failed/intermediate test logs are preserved as provisional history;
this report uses fresh recovery verification and does not accept that turn.

## Actual frozen-source results

- `python3 -m unittest discover -s tests -p 'test_agentic*.py' -v`:
  **PASS, 35 tests, 23.181 seconds**. This includes 29 game-policy tests and the
  parent's six client-configuration tests. Full output: `agentic-tests.log`.
- `python3 -m unittest discover -s tests -p test_project_management.py -v`:
  **PASS, 13 tests, 0.432 seconds**. Full output: `legacy-adapter-tests.log`.
- `.venv-harness/bin/python tools/check_agentic_mcp.py`:
  **PASS**, real local subprocess protocol initialization, tool listing and
  inventory call; **22 tools, 23 registered roles**, CLI readiness `READY`.
  Full output: `mcp-protocol.log`. The subprocess IPC required execution outside
  the sandbox, as the parent independently observed with its earlier probe.
- Read the actual setup task: `status=active`, `execution=agentic`,
  `agentic_turn=turn.002`. This is an observed binding, not task completion.

Total: **48 automated tests passed**, plus the actual stdio protocol probe.
The final test run followed the engineer's source freeze, including the last
atomic receipt check, duplicate-JSON-key rejection and active-profile completion
guard. `tested-source-snapshot.json` hashes the tested implementation/tests.
No later implementation revision is silently covered by these results.

## Coverage and discovered corrections

The independent tests exercise the genuine game adapter over the copied runtime:

- All registered roles start YES; omitted/extra roles and duplicate JSON role
  keys are rejected. Each unused role, including Vince, needs its own substantive
  reason; mandatory core and actual game-wiki role cannot be negated.
- Exact approved prompts, selected status, authorization tickets, complete upstream
  handoffs, acyclic dependencies and distinct external worker receipts are enforced.
- Collection, IA and Document Chief phases cannot be skipped. Missing/blocked Wiki
  work cannot become accepted work. Changed owned artifacts appear in collection;
  published turns cannot start later workers or publish twice.
- Wiki handoffs require existing authored game-wiki Markdown, semantic review and
  build/check evidence; process-publication paths do not satisfy that responsibility.
- Accepted setup alone does not invent historical question/research/baseline rounds.
  An explicit accepted imported-history record can unlock work while leaving those
  historical booleans false. Unreviewed imports and interrupted setup do not unlock
  production; changed historical references are detected.
- An imported existing project still blocks unanswered questions and permits a
  later research request. Its requesting worker must stop, and interruption retains
  the queued research and blocks later production.
- New software slices cannot finish before publication, from unrelated accepted
  publications, or with an unrelated/fake IA review receipt. A matching accepted
  publication, matching Wiki/all-role handoffs and actual recorded IA receipt allow
  the real completion method. Old `single-developer` metadata cannot bypass the
  currently active agentic profile. Historical legacy completion remains supported
  in its explicit legacy fixture profile.
- Workspace MCP setup preserves unrelated settings and refuses conflicting custom
  server entries/path escapes. Configured status remains `CONFIGURED_UNVERIFIED`.

QA reported three concrete defects to SWE rather than editing production code:
fresh initialization needed parent directories; mapped collector package paths
needed canonicalization before comparison; Wiki handoffs needed real authored-page
existence checks. SWE corrected them. Passing regression tests exercise the final
fixes. Earlier failure logs also contain fixture/API alignment errors while the
implementation was in progress; they are not unresolved final production findings.

## Limits and handoff

The actual probe starts its own local server and reports
`host_registration=CONFIGURED_UNVERIFIED`, `host_tool_observed=false`. It does not
prove the Codex desktop loaded its configuration. The parent must retain that
limitation until an actual host-exposed workflow tool succeeds.

Python validates recorded workflow, hashes and distinct receipt strings; it cannot
authenticate a host worker or detect every invented nonempty receipt. IA must inspect
real dispatch evidence. The tests demonstrate recorded constraints, not an OS
sandbox, tamper-proof chain of custody or exhaustive research/IA resolution proof.
Research-request blocking/persistence is tested here; a complete later researched
response/acceptance cycle and all possible finding-resolution combinations are
not exhaustively qualified by this suite.

No gameplay, native renderer, new canon/adaptation, game milestone, full historical
strict-harness suite or final wiki rebuild was run by QA. WIK-30 must maintain and
validate the actual game documentation after specialist freeze; Collector, independent
IA and Document Chief must then perform their real ordered responsibilities.
There are no remaining observed blocking QA defects in the tested scope.
