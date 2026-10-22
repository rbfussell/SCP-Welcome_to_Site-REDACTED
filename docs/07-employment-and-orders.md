# Employment, optional actions, and command authority

Status: Proposed contract, revision 0.5. Autonomous pawns, shift-based employment above optional action preferences, role-specific behavior and higher-rank orders with meaningful overrides are confirmed direction. Later employee starts reuse these records. Exact schedules, staffing numbers and doctrines remain proposed.

## 1. Separate employment from optional tasks

An employee has an appointment to a role and workplace, scheduled shifts, qualifications, reporting line and explicit responsibilities. The **work handler** owns their scheduled duty. The **action-job handler** assigns discretionary work only when the work handler releases capacity. Both execute through the same low-level movement/interaction/action services; employment is a different assignment system, not duplicate game physics.

Later player starts can inhabit these same employee roles without gaining director permissions. Appointments still define obligations and access, but direct player control remains the action source for the avatar rather than the NPC work handler silently replacing input. Any optional auto-duty assistance needs an explicit control mode. See [player roles and controls](08-player-controls-time-and-continuation.md).

Turning off general hauling does not prevent a cafeteria worker from carrying ingredients as part of their job. Turning off general cleaning does not prevent a janitor from cleaning their assigned area. An employed worker must not abandon a duty because another optional action has a better utility score.

Distinguish role duties, optional action preferences, construction/haul work requests, and low-level actions. The word “job” on a work order does not by itself make it primary employment. The scheduler checks the worker's actual assignment and availability.

## 2. Employment records and work handling

Persist employee ID, role definition, workplace/area, post/slot, supervisor, shift calendar, breaks, qualifications, permitted duties, coverage requirements, current duty, relief requirements and employment status. Support vacancies and unfilled shifts; a room existing does not create employees.

The work handler progresses through off-shift → reporting/preparing → on duty → scheduled break/relief → handover → off-shift. It travels, checks equipment and access, claims the relevant post, and runs a role-specific duty graph. Clocking in remotely must not make someone physically present at a counter or guard post.

Blocked duty is an explicit state with reason and escalation. A cafeteria worker without ingredients requests supplies or performs another cafeteria duty, then waits/reports if nothing is possible. They do not become a general-purpose hauler merely because preparation is blocked. Temporary reassignment requires an authorized order or a declared role policy.

Shifts include breaks and coverage rather than assuming endless labor. Critical posts require explicit relief/overlap policies. If relief fails, report the uncovered post, apply a bounded overtime/fallback policy, and retain real consequences. A staffing guarantee must not create an invisible guard or an immortal employee. Incapacity, severe exhaustion, or panic can still make coverage fail.

## 3. Role-specific behavior

| Employment | Example primary duties |
|---|---|
| Security/guard | Equip, take post, check authorization, patrol route, communicate sightings, coordinate response, escort and hand over observation/coverage |
| Cafeteria worker | Receive/replenish role supplies, prepare food, serve, maintain the work area and close/handover |
| Janitor | Follow assigned cleaning rounds, restock equipment, handle permitted waste, report restricted/hazardous areas |
| Scientist | Execute authorized research plans, inspect evidence, request controlled tests and record findings |
| Lab technician | Prepare equipment/samples, support tests, calibrate/check supplies and follow cleanup/storage procedures |
| Logistics worker | Receive, reserve, haul, stage, consolidate and issue stock within storage policies |

These are reusable role graphs with parameters, not a single “protect” or “work” action. Qualifications and clearance gate substeps. Neither a scientist nor a janitor automatically understands an unknown SCP. The module's containment/testing rules still determine what is possible.

Additional roles such as medical, engineering and site management can follow the same contract. M1 proves the six roles above with bounded routines and shared primitives; it does not require a complete profession simulation for each.

## 4. Orders and interruption policy

An order records issuer, recipient/team, authority scope, objective, urgency, parameters, expiry, acknowledgement, assignment effects and completion/failure predicates. Higher rank must include applicable command authority: departmental scope, delegated command, clearance and current incident leadership determine whether an order is valid.

Proposed arbitration:

1. Resolve physical incapacity, mandatory anomaly constraints and other actions the pawn cannot perform.
2. Apply acute emergency/panic/critical-need responses under the pawn's role and current procedure. Mild discomfort or ordinary fear is not automatic abandonment.
3. Consider valid direct orders. An order can interrupt employment only when its issuer and order type authorize reassignment or emergency command. Otherwise queue it, reject it, or escalate the conflict explicitly.
4. Execute current shift duties and approved breaks/handoffs through the work handler.
5. Allow discretionary action jobs when eligible, or ordinary off-duty activity.

This is a policy boundary, not a rule that every frightened guard always flees. Training, doctrine, stress, injury, perceived danger and team support determine response through deterministic game logic. Strong panic may override an order; the resulting lost coverage or failed mission is real and reported. The optional LLM cannot bypass the arbiter or redefine qualifications/authority.

Long actions define safe interruption points and emergency interruption outcomes. An urgent order can stop preparation immediately where safe; interrupting a medical action, loaded-container carry or guarded transfer must retain its actual partial state. Cancelling a duty does not complete it or restore carried items automatically.

## 5. Security teamwork and acknowledgement

Team commands can allocate leader, observer, mover, escort, cover and relief roles. Agreement means participants acknowledge their role and reserve required resources/posts before a coordinated action begins. A guard reports ready, unable, delayed or refusing with a reason. Issuing a team order does not imply unanimous compliance.

Containment procedures define minimum coverage, blink/observation handoff where relevant, communications, deadlines, readiness and abort predicates. An observer stays assigned until actual relief is established, unless a documented override forces failure. Missing acknowledgement or a failed radio link blocks/aborts the dependent step; do not let the planner assume a watching guard exists.

In the SCP-650 scenario, procedural three-person staffing and current sight are separate facts. A delayed relief guard can cause an understaffing alert while one actual observer still prevents relocation. Its module can relocate only when all qualifying sight is lost. This distinction must remain visible in roster, procedure and observation diagnostics.

Ordinary response doctrine can select cover, hold, flank, escort, call support or withdraw using shared action primitives. The initial slice only needs hold/observe, cover/escort and withdraw with a coordinated readiness/handoff gate. Expanded tactics require separate content and acceptance.

## 6. Optional work and capacity

The action-job handler sees only unreserved availability. Off-shift availability depends on scenario policy, willingness, rest and authorization; it is not automatically unpaid unlimited labor. An employed logistics worker receives hauling as a primary role duty. A guard can move a supply item if ordered or if that move is part of a security procedure, without becoming generally available for every haul request.

Dispatching a team removes its members from on-site duty coverage for the operation interval. Warn about uncovered posts and allow an authorized manager to accept the consequence or assign relief. Do not double-book one person at a gate, cafeteria and field mission. Reservations cover actual staff capacity as well as equipment.

## 7. Visibility, persistence and scope

Local inspection and terminal rosters show employment, workplace, shift, current duty, next handoff, accepted order, coverage and any blocking/override reason. A useful explanation is “on cafeteria shift; optional hauling disabled; replenishing ingredients as a role duty,” or “guard post uncovered: assigned guard incapacitated; relief requested.”

Save appointments, clocks, current duty graph and continuation, orders/acknowledgements, role/post reservations, coverage, team agreements and override state. Offscreen work uses the same obligations and interruption rules. Resuming must not reset a shift, free a reserved employee, or assume an unperformed handover succeeded.

M1 exercises one scheduled shift with breaks/relief and six role definitions using the existing staff ceiling. Vacancies, blocked work and reassignment are test cases, not a promise that 16 staff fully cover every service at all hours. See EMP-01 through EMP-08 in [scope and acceptance](04-scope-and-acceptance.md).
