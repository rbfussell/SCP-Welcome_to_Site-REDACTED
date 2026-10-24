# Player event log and tactical training

Status: Runnable bounded proof, October 24, 2026. The user confirms Fallout-style feedback/history alongside the field view and authorizes the first tactical combat work. A full combat, injury or conversation system is not implied by this fixture. Existing [time rules](adr/0006-real-time-exploration-and-tactical-turns.md) and [awareness rules](18-awareness-and-encounter-entry.md) continue to apply.

## Confirmed presentation target

The user confirms on October 24 that the finished field interface should place the event log and related play controls in a Fallout 2-style bottom HUD. The Pip-Boy comparison concerns opening a dedicated interface; OSCP is not an arm-mounted device. Full terminals provide the strategic construction, mission planning, SCP investigation, intelligence and equipment/material/network management workspace. Full OSCP uses a visible faux CRT monitor frame and is accessible from fixed facility workstations. A secure Foundation phone supplies a limited local-map, mission-information, communications and authorized mission/team-command profile, extensible through registered applications. The current engineering panels can remain during development. Final layout, monitor/phone art, input bindings and app navigation will be designed later; a shared phone/inventory menu with inventory on the right is planned, while automatic pause behavior awaits clarification.

Keep that replacement within presentation adapters: the HUD reads acquired journal entries and combat/query state, and submits validated simulation commands. OSCP uses the same service endpoints and device/session access rules from [the terminal contract](11-oscp-terminal.md). UI arrangement, scroll position and open panels belong to presentation; message history, action costs, knowledge, authority and world effects belong to the simulation. Future panels must preserve these boundaries so the debug layout can be rewired without rewriting gameplay rules.

## Player-facing log

The lower field panel keeps the newest 100 logical entries and displays five at a time. Wheel-scroll within the panel or use its scrollbar to read history. New messages preserve a history position; scrolling to the bottom resumes live following. Long entries use ellipsis and expose their complete text on hover. Entries carry world timestamps, category labels and distinct colors: observation, orders, combat, equipment, conversation, status and warning. Labels keep meaning independent of color. The future conversation view should publish its completed exchanges to this same journal; no conversation runtime or transcript producer exists yet.

The journal is separate from the unrestricted F2 diagnostic trace. Producers report what the character observes, does, receives or experiences. A visible character entering sight creates one entry per visibility episode. Ordinary movement while continuously visible creates none. Brief blinks retain the previous visibility episode. Relevant visible equipment actions and attacks have explicit producers; a hidden attacker affecting the player stays unidentified. Unseen third-party combat does not leak into the log. Sound propagation and general special-ability providers remain future work.

Accepted orders appear as submitted intent. Delivered acknowledgements/results come from the existing voice/radio service. After one second of local time without delivery, a pending voice order yields one tentative “I don't think they heard me” message. It neither reveals the cause nor cancels the order; later delivery can still succeed. The one-second delay is provisional tuning. Stored radio reports enter the journal only when the physical terminal is accessible. No omniscient debug messages are copied into player knowledge.

## Trying combat

In field view, select a staff opponent in the right panel and choose **Arm nonlethal training drill**. Direct player sight of that opponent within eight world hexes starts local turns automatically. An unseen nearby opponent does not create a telltale input hold. The radius is a test value independent of zoom/window size, not the complete ordinary-hostility policy.

All three existing characters receive training roles: the chosen opponent is on the opposing side; remaining staff are allies. Seeded initiative can give an autonomous participant the first turn. Standing duties and trolley work are suspended; pending trolley work reservations are released while actual cargo/cart association remains. Everyone's current fractional position is preserved. On finishing the drill, ordinary duties resume.

| Action | Control | Provisional cost | Local duration |
|---|---|---|---|
| Move | Click ground; Shift-click keeps facing | 1 AP per committed hex | 300 ms per full hex, retaining fractional progress |
| Face | RMB / Ctrl-click | 1 AP | 150 ms |
| Ready training sidearm | V or panel | 2 AP | 300 ms |
| Fire | Click visible opponent's sprite or ground position or select target and use panel | 4 AP | 600 ms |
| Reload | Y or panel | 3 AP | 900 ms |
| Use nearest door | E | 2 AP | 300 ms |
| End turn | Enter or panel | Forfeit remaining AP | 300 ms local wait |

Each turn grants 10 AP. Accepted actions reserve their cost exactly once; invalid, unaffordable, overlapping or out-of-turn commands change no authoritative action state. Facing and equipment shortcuts cannot bypass costs. A blocked paid route stops with generic feedback and refunds untravelled steps. A partially traversed hex consumes one step. Startle can delay physical movement; an action cannot end with unpaid movement still running. Equipment actions and attacks resolve at explicit checkpoints, independent of animation frames.

During player selection, all local simulation waits: personnel, movement, blinks, startle, anomaly deadlines and containment work. The calendar/offsite coarse counter continues. Only the active actor's purchased movement advances during an action; anomalies and observation still evaluate at action boundaries/ticks. Passing a turn includes a short local wait so local recovery timers cannot become permanently stuck. Explicit Space pause still stops both clocks; F6 is an additional diagnostic local hold.

Training sidearms are accessible equipment fixtures with six loaded rounds and twelve reserve rounds. Participants have six training points. A shot has a provisional 75% hit chance and removes two points; ammunition and RNG are persisted. Zero points removes the participant from the drill's turns, without inventing physical injuries or changing their ability to observe an SCP. The drill ends when the opposing side or player is out, or when explicitly ended at a player input boundary. Starting another drill intentionally resets its training equipment/points; this is not a qualified campaign resource/disengagement policy.

Autonomous participants use direct observations and stored last-seen points for aiming/travel/search. They cannot track an unseen moving target. Training direction cues use those acquired points, never hidden live transforms. Visible attacks, equipment use and outcomes appear in the journal; ordinary walking does not.

## Ownership and persistence

[The simulation code map](../src/SiteSimulation/README.md) identifies the boundaries. `Feedback` owns bounded player messages and evidence-gated producers. `Combat` owns local initiative/AP, action continuations, training decisions and validation. Door checks are shared with realtime play. Godot panels only submit commands and draw committed state. Rendering/log inspection consumes no RNG and cannot grant AP.

Outer save schema 5 adds journal, delivery/visibility deduplication, roster/initiative, AP, training equipment/points, contacts, pending actions and RNG. F5/F9 preserve an unfinished shot or movement without charging/replaying it. Restores validate a detached candidate and commit only on success. Matching schema-2/3 spatial saves and schema-4 spatial/operation saves migrate to an empty journal/inactive drill; already-received legacy reports are marked consumed. SCP-650 module schema remains 2. Content identity is unchanged; general content migrations remain out of scope.

## Verification and remaining qualification

Run from the workspace root:

```bash
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --combat-smoke work/preview/spatial-scene/tactical-training.png
python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/event-log-operations.png
python3 tools/run_spatial_scene.py --smoke work/preview/spatial-scene/event-log-spatial.png
```

The combat render fixture starts from a validated saved test arrangement with the player turn selected and Chen already out of training. It submits V, an actual map-click shot and Enter, checks the independent clocks, journal, ammunition/AP and save round-trip, then verifies autonomous turns return input. It is not evidence that initiative always favors the player. Engine-independent checks cover the default non-player opening initiative separately.

This proves subsets of TAC-01–TAC-03 and AWR-03. Full hostility/factions/ambush, configurable weapons and injury/death, completed-death scene reveal, inventory-backed access/transfer costs, general special abilities, combat disengagement, cross-zone interaction/multiplayer and real offsite simulation remain unqualified. SCP-650 remains nonaggressive and outside the training roster. No safety hold for an unseen lethal SCP, no complete M1 acceptance and no delivery date are asserted.
