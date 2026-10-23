# Personnel orders, OSCP records and observed recovery

Status: Bounded native prototype, October 23, 2026. This adds a playable coordination/recovery proof to the spatial scene. It does not certify full M1, employment, inventory, radio infrastructure or the public mod API.

## Playing the operation

The normal launcher now loads [the operation definition](../game/Content/scp650-operation.json) alongside the existing facility geometry. The director, Chen and Ortiz start in actual direct observation. Chen and Ortiz have standing observation duties, which displace their old discretionary patrol. After three local seconds, the lit receiving footprint with closed access and three eligible humans is procedurally compliant. One observer still suffices to suppress SCP-650's self-relocation; understaffing alone cannot release it.

The field panel provides local voice orders. Select Chen or Ortiz, then maintain observation, follow the director, take a corridor post, pick a post on the map, or recover SCP-650 using the trolley. A picked post uses the next field click; RMB cancels picking. At a post, personnel face the target point communicated with the order. Observation and following remain distinct tasks.

Open the upper room's door with **E** while near it. **Approach OSCP terminal** requests a normal walking route to the marked corridor console. **T** opens/closes OSCP while physically in reach and with service available. The command/document panel shows submitted intents and received records with their author and local time. Opening it does not pause. Walk away or lose terminal service and the session closes. The terminal uses the same order service as local voice; its fixture transport is site radio.

To create an incident through ordinary play, relieve the NPC watches with post orders, open their route and turn/leave with the director. Other valid observers continue to hold the statue until their sight actually ends. No mission command forces a relocation. Locate the statue through actual sight or a delivered report, establish observation, and assign one employee to trolley recovery while the director and the other employee cover it. Open necessary doors and use posts to clear routes or reposition observers. Reissue an observation order to share a newer acquired location when an old point is empty.

The handler fetches the real trolley, rolls it to the acquired location, loads under observation, pushes to the receiving footprint, unloads and parks the empty trolley. Loading, each push and unloading require 600 local milliseconds of work. The current proof commits a validated one-hex transfer at completion; it does **not** implement a continuously moving trolley or swept trolley animation. Actors walking to equipment still use normal smooth movement.

Reestablish three eligible humans, light and closed receiving access for three local seconds. A recovery is recorded only after the subject actually left the footprint and those conditions are restored. Completion records the historical recovery; subsequent coverage loss or another breach remains possible. Conditions, equipment, orders, acquired contacts and reports persist through F5/F9.

## System boundaries

| Component | Responsibility |
|---|---|
| [OperationState](../src/SiteSimulation/Operations/OperationState.cs) | Scenario data, immutable order/report records, staff contacts, terminal/radio fixture state, trolley and recovery history |
| [PersonnelOrderSystem](../src/SiteSimulation/Operations/PersonnelOrderSystem.cs) | Commander validation, submission, delivery/acknowledgement, relief, duties, travel and blocked outcomes |
| [FacilityCommunications](../src/SiteSimulation/Operations/FacilityCommunications.cs) | Direct sensing, local voice and explicit powered-radio/terminal delivery |
| [ObservedTransportSystem](../src/SiteSimulation/Operations/ObservedTransportSystem.cs) | Handler access, timed physical work, path/reservation validation, loading/pushing/unloading and cargo detachment |
| [OperationLifecycle](../src/SiteSimulation/Operations/OperationLifecycle.cs) | Authored initial assignments and continuously evaluated receiving conditions; evidence-backed assessments |
| [OperationSaveCodec](../src/SiteSimulation/Operations/OperationSaveCodec.cs) | Detached validation of orders, evidence, rig occupancy and work continuation |
| [OperationsPanel](../game/Presentation/OperationsPanel.cs) | Voice controls and OSCP command/document projection; no hidden live-location or execution-status lookup |

The operation chooses a subject and receiving conditions through data. The SCP module explicitly permits observed external transport as an adaptation. The host validates physical transfers; the operation cannot override self-relocation or choose its destination. Physical transport preserves pose and anomalous appearance/encounter identity. It does not create a new startle episode at every pushed hex.

## Orders and information

Orders pass through pending, acknowledged active, blocked, complete or cancelled states. A replacement relieves an existing duty only after actual delivery; an offline pending message cannot remove an observer. Blocking does not teleport a worker or substitute a success percentage. The completed transport procedure assigns observation until relief. Arbitrary employee roles, shifts, fear/authority arbitration and team agreements are not implemented by this fixture.

Local voice requires proximity and clear communication geometry. The explicit site fixture supplies issued radios to the two employees, a physical console, terminal power and network availability. Radio power/link loss prevents delivery. These are persisted bounded device states, not inventory-backed radios, modeled RF propagation, an electrical circuit, SCiPnet synchronization or resident AIC behavior.

A worker uses their own acquired contact or the point delivered with an order. Hidden subject movement does not update that point. Finding the old point empty produces a blocked report. A follow order remembers the last actually seen leader position. Reports received by the terminal can remain available later, with their original observation time and source. Reading a historical radio sighting feeds shared player awareness as a report, never as direct sight. Movement-safety settings and encounter deduplication remain unchanged.

OSCP displays submission intent and **received reports**, not an omniscient projection of current worker state. The field summary uses delivered local assessments; OSCP can show received radio assessments too. No report of a hidden relocation is generated simply because the simulation knows it happened. An observer may legitimately report an empty receiving footprint without knowing the new location. Diagnostics remain deliberately unrestricted.

## Physical interruption and persistence

The trolley blocks a hex and reserves the next hex during push preparation. A handler needs adjacent clear access; paths, occupied/reserved endpoints, walls/doors, module permission and actual observation are checked again before commitment. Another eligible observer is required for transport even when the handler's own sight suppresses self-relocation. Losing external coverage stops work; restoring coverage can recover it. A blocked path waits for an actual change or another order.

If SCP-650 legally relocates from a loaded trolley, cargo association and pending work/reservations detach atomically. The one authoritative statue moves under its module; the empty trolley remains a physical blocker at the old position. No second statue remains aboard and no automatic mission failure neutralizes the module. Searching an empty last-known location waits for new evidence.

Outer save schema **4** includes the operation snapshot. Validation preserves loading/pushing/unloading progress, pending versus acknowledged orders, radios, received evidence and historical recovery without replaying actions. Invalid restores leave the live simulation intact. Scenario/content identity includes the operation JSON. Earlier spatial-only saves are therefore not a matching operation campaign; they remain readable under the matching spatial-only schema-2/3 compatibility fixture. SCP-650 module schema remains 2.

## Verification and remaining limits

[Operation checks](../tests/SpatialChecks/OperationChecks.cs) cover initial real coverage, delivery versus acknowledgement, command authority, blocked access, undelivered relief, radio/terminal outages, loading/push/unload saves, destination reservation, invalid restore isolation, recovery, later coverage loss and relocation leaving equipment behind. `python3 tools/run_spatial_scene.py --check` runs them with the existing spatial/module checks.

`python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/scp650-operations.png` exercises initial coverage, E door use, physical console approach, T access, order acknowledgement, continuing local time and save round-trip in Godot. `--smoke` retains the original isolated observation/movement fixture.

The three-person operation requires the director to participate in coverage. Unattended full-team recovery, player-as-handler controls, autonomous door use, full transport formations, multiple rigs, continuous trolley motion, room-scale containment validation, injury/panic/employment relief, inventory-backed equipment, cameras/AIC, public scripting, full combat and general world/network operations remain later qualification. The current equipment uses diagnostic geometry/reused stand-ins. This is a focused incident proof over the small scene, not the complete staffed facility milestone.
