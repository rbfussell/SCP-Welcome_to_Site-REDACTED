# Player role, field controls, time, and continuation

Status: Revision 0.8, October 22, 2026. Confirmed: director/later employee starts, Fallout-like controls/animation, hex navigation, real-time exploration/operations and turn-based tactical combat. All local simulation waits for turn input, including movement, hazards and timers; the world calendar, granular world simulation and other areas continue. This replaces the earlier real-time-combat assumption and global-wait proposal. Bindings, AP/time mapping, boundary behavior and milestone limits remain proposed.

## 1. One character, scenario-defined authority

The initial campaign starts as a newly appointed regional operations director with one functioning Foundation site. Responsibilities include personnel, equipment, finances, research, security and containment. Foundation command supplies objectives, restrictions and delegated authority. Network expansion follows through the same simulation.

Starting as a director is scenario data. Later starts can use security, research, maintenance, medical or cafeteria employment, in the spirit of the user's SS13 reference. Reuse character, employment, clearance, knowledge, inventory, organization and authority records. Do not branch the engine into separate employee and management games.

The controlled actor has a role and credentials; the human player's input does not itself confer authority. Terminal services, doors, equipment, orders and reports validate the acting character's current permissions. An employee can use authorized local services while network management remains unavailable unless granted through the world. Scenario UI can hide or explain unavailable controls without creating a separate authority system.

Direct control attaches to one character at a time. Orders influence autonomous colleagues; selecting a team does not possess its members. Employee starts expose shift duties and workplace obligations while direct input remains with the player. Whether optional automatic duty assistance is offered is a later control decision; the NPC work handler must not silently take over the avatar.

## 2. Field commands and equipment abilities

Use the confirmed fixed isometric/three-quarter sprite presentation with Fallout-like point-and-click movement, interaction and animation feel. The user identifies Fallout 2/Falltergeist as the blueprint to implement in Godot, including real-time exploration and turn-based tactical combat. Equipment-based functions, Ctrl-click intent, physical access and character authority apply in both modes.

Actor destinations and routes use a separate hex navigation grid with smooth transitions in exploration and during committed tactical moves. Construction/floor cells remain square. The input adapter resolves clicks to valid reachable hex destinations while the simulation owns movement progress and independent facing. Grid scale/registration and near-target interaction placement are M0 qualification choices.

Prototype click-to-move, walk/run presentation, visible turning, contextual interaction and hover/target feedback as one coherent Fallout-like interaction layer. Exact mouse bindings, cursor-mode cycling versus contextual selection, run modifier and interaction-menu behavior remain explicit design decisions before their affected controls are fixed. Research the reference behavior, show a concrete prototype and ask about unresolved choices. Replacing the movement animation must not change travel or interaction timing.

Proposed command defaults:

| Input/context | Intent |
|---|---|
| Click reachable ground | Request movement to a position |
| Click a visible object/person | Request the displayed contextual interaction, or inspect/approach if needed |
| Select an equipment ability | Choose a function and targeting mode supplied by a physical source |
| Ctrl-click with an appropriate selected function | Explicitly designate an entity/position or force an otherwise non-default action |
| Issue a team/personnel order | Send an acknowledged order through the character's applicable authority |

[Shared awareness](18-awareness-and-encounter-entry.md) interrupts click-to-move on a newly sensed anomaly when the relevant setting is enabled. Both self sightings and actually delivered reports default on. Clear the path and stop at the exact fractional position; world/local time keep running. This is independent of an SCP's fear/startle behavior and does not automatically start combat. A deliberate new movement command remains possible while the same encounter stays visible.

Ctrl-click does not bypass collision, range, inventory access, clearance, equipment readiness or anomaly rules. Show the pending action and failure reason. Its exact mouse-button/modifier bindings remain remappable implementation details. SCP-650 is not a hostile auto-attack target; its default contextual action should be inspection/observation or a valid containment interaction. Explicit weapon targeting can still have ordinary consequences.

Abilities primarily come from carried, worn or otherwise physically accessible equipment: weapon fire, flashlight, radio, medical kit, tool, terminal, or anomalous item. Bind an ability to its actual item instance/source and declared function. Skills can modify performance and prerequisites; they do not create unowned equipment. A module may explicitly grant another kind of ability through a published capability.

The equipment interface labels source location and readiness: hand/belt, holstered and requires drawing, backpack and requires retrieval, buried/locked and blocked, unavailable, or out of resources. A quick slot is a reference, not extra storage. Using it may create draw/retrieve/equip actions before use. Dropping, transferring, exhausting, locking or losing that item invalidates the shortcut immediately; revalidate at action commitment.

Separate destination, action target, body orientation and perception-facing. Looking down a corridor, walking backward, opening equipment, and using a terminal can impose different attention/facing changes. Action definitions specify those changes; the green direction marker and sight display expose them. Exact turning speed and equipment timings are tuning values to measure, not implicit consequences of sprite frames.

## 3. Sight and shared information

The ordinary field view uses the avatar's senses plus information actually delivered by friendly staff, surveillance and the operational network. Track source, timestamp, uncertainty and context. Shared knowledge can mark a reported location without making the avatar directly see it or track its current position through a wall.

Direct observation, remote live observation, recorded footage, remembered location and a radio report are different evidence types. The module decides which can qualify for a particular anomaly. A camera feed requires the relevant device, power, line of sight, connection and qualified attention state. The overhead renderer never becomes another observer.

Commands referring to shared or distorted information resolve through the actor's targeting context. If the target moves or information is stale, use the action's declared last-known-position/reacquisition/failure policy. Knowledge of a target does not grant a weapon a clear physical shot. UI feedback must not reveal hidden true identities or positions to explain a failed action.

## 4. Time controls

The user resolved “same time solution” as the Fallout-like exploration/combat split plus animation feel, then confirmed complete local tactical waiting while the wider world and other areas continue. [ADR-0006](adr/0006-real-time-exploration-and-tactical-turns.md) records the final zone policy and remaining implementation/balance choices.

One authoritative kernel maintains the world calendar and declared local execution/progress states. Outside combat, ordinary life, work and interaction run in real time. Opening inventory or a terminal does not implicitly pause this mode. A ruleset controls explicit manual pause, acceleration and event-driven normal speed/optional auto-pause. Default development/M1 play permits manual pause.

In tactical combat, actors take sequential turns with action-point budgets, visible costs and explicit end-turn control. Input waiting holds movement/action progression for everybody in the engaged local area, including noncombat local pawns. The world calendar continues; the wider world updates at its normal granular/event cadence, and other areas continue their own simulation. There is no automatic global pause caused by one area's player decision.

The timing profile must specify AP grants/costs, initiative, local action/turn progress, timer clock bases and waiting/end-turn behavior. The original game's numerical values are reference points rather than unreviewed constants. The continuing world scheduler already accounts for elapsed decision/execution time; do not add that time a second time at round end or combat exit. Do not bulk-replay all missed local actions when releasing a hold. Exact budgets, costs, durations, local-zone membership and grid/time registration remain to be qualified.

All local simulation waits, as explicitly confirmed: bleeding, blink cycles, local SCP countdowns, work and other conditions stop along with local movement/actions. Store their remaining durations/progress. On release, resume from those values; the continuing world calendar does not make them immediately expire or trigger a bulk catch-up. Declare timer ownership and clock bases. World-scoped events continue, with genuinely external local effects handled at a qualified encounter boundary rather than slipping through as local timers.

Local input waiting is distinct from explicit manual pause. Inspecting a UI or previewing a grid placement cannot transfer items, retrieve equipment, turn an observer or complete a held local order for free. Committed changes use the same validators and their declared action/AP/progress semantics. Other zones' valid actions continue normally. Acceleration cannot skip required player input or critical observation events; animation frames/cosmetic playback cannot grant extra AP or secretly advance held movement.

The player controls the avatar's turn; friendly personnel and opponents remain autonomous under their duties, orders and permitted actions. Encounter entry, participants, zone hold membership and exit must be explicit and cannot refill AP, reset a hazard or duplicate a committed action. External physical interactions require a qualified boundary/interrupt or encounter-merge policy; actors in another area cannot attack held characters through an undefined timing gap. Preserve critical incoming transitions until they can be resolved safely. SCP-650's nonaggressive startle is not automatically combat. Qualified ordinary hostile engagements/attacks automatically request tactical entry. Hidden anomalous movement and lethal effects can resolve without warning or a player turn; acquired evidence and actual remaining reaction time govern entry, rather than a universal SCP proximity trigger. Combat mode cannot invent observation, reset an attack deadline or guarantee a response. After committed avatar death, reveal the scene as presentation only; this grants no knowledge to living actors. Use world-space engagement distance with a buffer and actual attack reach; camera zoom/window dimensions cannot change whether combat starts. Contact direction/age/uncertainty comes from sight, delivered reports or qualified sound evidence, never an unseen enemy's live position. Exact trigger tuning, initiative and boundary rules remain a required prototype pass; see [awareness and entry](18-awareness-and-encounter-entry.md).

Persist the world calendar, per-zone modes/hold flags, encounter/participants, active turn, AP state, timing-profile/clock-basis versions, action IDs, local progress and queued cross-zone events. Restore held/running areas independently without duplicate elapsed time, AP or damage. Replay includes recorded command timestamps and external world events: different thinking durations can produce different background-world outcomes. Rendering/frame-count changes must not alter resolution for the same recorded timeline.

Multiplayer remains later. Preserve the architectural ability for an unrelated second player's area to continue. The proposed configurable decision timeout (for example 30 seconds) is a later option; timeout duration/action and disconnect handling are not fixed or implemented by this decision.

On region departure, the user proposes a contextual summary such as securing the area, packing gear or writing reports to explain elapsed world time. Record encounter entry/exit timestamps and local progress; do not add the already elapsed interval to the world calendar again or rewind other areas to match. The example of 1.2 hours is illustrative. Text alone does not perform recovery, treatment, inventory transfers or report submission. Additional timed departure work versus a summary-only transition is awaiting clarification; the current proposal is summary-only with separately validated real work when requested. [ADR-0006](adr/0006-real-time-exploration-and-tactical-turns.md) records the distinction and persistence requirements.

During explicit manual pause, UI may preview layouts and queue permitted commands. Movement, drawing equipment, transfers, work, treatment and module timers do not complete until the participating simulation resumes. A local tactical hold applies that gate only to its area; other areas remain active. On resume, validate commands against current authoritative state. The developer inspector can have separate explicit editing tools; these are not campaign actions.

Significant containment changes, injury, hostile contact, a requested acknowledgement or a critical staffing failure can emit interruption events. Apply configured speed/pause policy at the committed event boundary. Pausing does not undo an already resolved consequence. Acceleration must process observation and anomaly events at required fidelity; it cannot skip a brief loss of observation.

Unattended locations use certified detail levels and continue according to their own area/world execution policy. A local combat hold does not stop another site's employment or the wider event planner. Difficulty remains independent of character role; a cafeteria start does not imply different physics or a global pause.

For later travel between world locations, the user references a Fallout/Wasteland-like world-map presentation. Treat this as an interface direction over actual travel and continuing world events, not an unpriced instant transfer. Exact route selection, travel duration, resource use, interruption/encounter rules and availability remain open design questions for the world/network stage. M1 remains inside the starting facility.

## 5. Failure policies

Persistent losses include casualties, injury, abandoned equipment, escaped anomalies, damaged/destroyed sites, budget changes and failed obligations. Distinguish avatar death, incapacitation, organizational collapse and a scenario's explicit ending conditions.

Provide a versioned ruleset with allowed continuation options rather than hardcoding one death screen:

- Reload a permitted save/checkpoint.
- End the campaign permanently.
- Continue the existing world through an eligible living character or an authored replacement scenario.
- Invoke an available anomalous continuation mechanism under its module/ruleset conditions.

M1 proves ordinary save/reload and permanent-ending policies. Successor control and anomaly-triggered day restart are staged architecture proofs. They are core supported policy types, not mandatory in every scenario. Exact difficulty presets and their permitted combinations remain to be selected.

Succession transfers direct input after an explicit continuation event. The new character keeps their own body, location, knowledge, inventory, rank and permissions. The dead character and possessions remain in the continuing world. Do not silently promote a cafeteria worker to director because the player controls them. Unavailable candidates and organizational collapse use the configured fallback.

## 6. Bounded anomalous restart

Support a host-managed checkpoint/branch capability. A module requests an authorized restart; it cannot read arbitrary files or selectively rewrite the past. The host validates ruleset permission, checkpoint compatibility, activation/cost conditions and any explicitly retained information.

A restart restores the whole participating world snapshot, including organizations, NPCs, inventory, topology, generated modifications, module state, random streams, timers, reservations and external decision records. Start a new branch/epoch; simulation time is ordered within a branch. Cancel pending commands, callbacks and LLM results from the abandoned branch. Preserve the old save/history rather than mutating it in place.

An authored retention policy may carry over selected memory or a limited restart charge in campaign-level state. Apply that policy once with recorded provenance and validate the new state. Retained inventory cannot coexist with its restored copy; retaining arbitrary objects is not part of the initial proof. A cost must not be refunded automatically by restoring the checkpoint that predates payment.

This supports a deliberate “restart the day” mechanic without promising arbitrary temporal paradoxes, local partial rewinds or compatibility with every future mod. M3 proves one synthetic checkpoint-based anomaly and one successor handover before a specific production SCP or difficulty preset uses them.

Related requirements: CTL-01 through CTL-04, TIME-01 through TIME-02, TAC-01 through TAC-03, ROLE-01 through ROLE-02 and CONT-01 through CONT-03 in [scope and acceptance](04-scope-and-acceptance.md).
