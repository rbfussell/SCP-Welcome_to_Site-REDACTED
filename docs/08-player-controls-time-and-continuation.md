# Player role, field controls, time, and continuation

Status: Revision 0.5, October 22, 2026. The starting director role, later employee starts, click-based equipment controls, real time with pause/acceleration, and configurable continuation are confirmed direction. Specific input defaults and milestone limits below are proposed.

## 1. One character, scenario-defined authority

The initial campaign starts as a newly appointed regional operations director with one functioning Foundation site. Responsibilities include personnel, equipment, finances, research, security and containment. Foundation command supplies objectives, restrictions and delegated authority. Network expansion follows through the same simulation.

Starting as a director is scenario data. Later starts can use security, research, maintenance, medical or cafeteria employment, in the spirit of the user's SS13 reference. Reuse character, employment, clearance, knowledge, inventory, organization and authority records. Do not branch the engine into separate employee and management games.

The controlled actor has a role and credentials; the human player's input does not itself confer authority. Terminal services, doors, equipment, orders and reports validate the acting character's current permissions. An employee can use authorized local services while network management remains unavailable unless granted through the world. Scenario UI can hide or explain unavailable controls without creating a separate authority system.

Direct control attaches to one character at a time. Orders influence autonomous colleagues; selecting a team does not possess its members. Employee starts expose shift duties and workplace obligations while direct input remains with the player. Whether optional automatic duty assistance is offered is a later control decision; the NPC work handler must not silently take over the avatar.

## 2. Field commands and equipment abilities

Use the confirmed fixed isometric/three-quarter sprite presentation with Fallout-like point-and-click movement and interaction feel. The user identifies Fallout 2/Falltergeist as the visual/control blueprint to implement in Godot. Preserve the already confirmed real-time action timing, equipment-based functions and Ctrl-click intent; square construction tiles and hex navigation do not introduce turn-based combat.

Actor destinations and routes use a separate hex navigation grid with smooth real-time transitions, as clarified by the user. Construction/floor cells remain square. The input adapter resolves clicks to valid reachable hex destinations while the simulation owns movement progress and independent facing. Grid scale/registration and near-target interaction placement are M0 qualification choices.

Prototype click-to-move, walk/run presentation, visible turning, contextual interaction and hover/target feedback as one coherent Fallout-like interaction layer. Exact mouse bindings, cursor-mode cycling versus contextual selection, run modifier and interaction-menu behavior remain explicit design decisions before their affected controls are fixed. Research the reference behavior, show a concrete prototype and ask about unresolved choices. Replacing the movement animation must not change travel or interaction timing.

Proposed command defaults:

| Input/context | Intent |
|---|---|
| Click reachable ground | Request movement to a position |
| Click a visible object/person | Request the displayed contextual interaction, or inspect/approach if needed |
| Select an equipment ability | Choose a function and targeting mode supplied by a physical source |
| Ctrl-click with an appropriate selected function | Explicitly designate an entity/position or force an otherwise non-default action |
| Issue a team/personnel order | Send an acknowledged order through the character's applicable authority |

Ctrl-click does not bypass collision, range, inventory access, clearance, equipment readiness or anomaly rules. Show the pending action and failure reason. Its exact mouse-button/modifier bindings remain remappable implementation details. SCP-650 is not a hostile auto-attack target; its default contextual action should be inspection/observation or a valid containment interaction. Explicit weapon targeting can still have ordinary consequences.

Abilities primarily come from carried, worn or otherwise physically accessible equipment: weapon fire, flashlight, radio, medical kit, tool, terminal, or anomalous item. Bind an ability to its actual item instance/source and declared function. Skills can modify performance and prerequisites; they do not create unowned equipment. A module may explicitly grant another kind of ability through a published capability.

The equipment interface labels source location and readiness: hand/belt, holstered and requires drawing, backpack and requires retrieval, buried/locked and blocked, unavailable, or out of resources. A quick slot is a reference, not extra storage. Using it may create draw/retrieve/equip actions before use. Dropping, transferring, exhausting, locking or losing that item invalidates the shortcut immediately; revalidate at action commitment.

Separate destination, action target, body orientation and perception-facing. Looking down a corridor, walking backward, opening equipment, and using a terminal can impose different attention/facing changes. Action definitions specify those changes; the green direction marker and sight display expose them. Exact turning speed and equipment timings are tuning values to measure, not implicit consequences of sprite frames.

## 3. Sight and shared information

The ordinary field view uses the avatar's senses plus information actually delivered by friendly staff, surveillance and the operational network. Track source, timestamp, uncertainty and context. Shared knowledge can mark a reported location without making the avatar directly see it or track its current position through a wall.

Direct observation, remote live observation, recorded footage, remembered location and a radio report are different evidence types. The module decides which can qualify for a particular anomaly. A camera feed requires the relevant device, power, line of sight, connection and qualified attention state. The overhead renderer never becomes another observer.

Commands referring to shared or distorted information resolve through the actor's targeting context. If the target moves or information is stale, use the action's declared last-known-position/reacquisition/failure policy. Knowledge of a target does not grant a weapon a clear physical shot. UI feedback must not reveal hidden true identities or positions to explain a failed action.

## 4. Time controls

Clarification pending: the user's latest “same time solution” request may mean Fallout-like animation timing, Fallout's exploration/combat time split, or a correction to “tile solution.” A question has been sent. The previously confirmed real-time/pause/acceleration baseline below remains provisionally recorded; do not infer a combat-clock change or fix the affected implementation until the intended meaning is answered.

All gameplay uses simulation time. Opening inventory or a terminal does not implicitly pause it. A ruleset controls whether manual pause is permitted, acceleration limits, and event-driven return to normal speed or optional auto-pause. Default development/M1 play permits manual pause.

Paused UI may preview layouts and queue permitted commands. Movement, drawing equipment, transfers, work, treatment and module timers do not complete until simulation runs. On resume, validate commands against current authoritative state. The developer inspector can have separate explicit editing tools; these are not campaign actions.

Significant containment changes, injury, hostile contact, a requested acknowledgement or a critical staffing failure can emit interruption events. Apply configured speed/pause policy at the committed event boundary. Pausing does not undo an already resolved consequence. Acceleration must process observation and anomaly events at required fidelity; it cannot skip a brief loss of observation.

The simulation continues in unattended locations using certified detail levels. Difficulty is a ruleset independent of the chosen character role; a cafeteria start does not imply different physics or automatic pause behavior.

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

Related requirements: CTL-01 through CTL-04, TIME-01, ROLE-01 through ROLE-02 and CONT-01 through CONT-03 in [scope and acceptance](04-scope-and-acceptance.md).
