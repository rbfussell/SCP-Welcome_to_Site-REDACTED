# Simulation code map

This is the engine-independent C# prototype. Godot input, drawing and diagnostics UI live in [game/Presentation](../../game/Presentation). The public script host, inventory, employment and complete combat are not implemented yet.

| Directory/file | Owns | Does not own |
|---|---|---|
| [SpatialSimulation.cs](SpatialSimulation.cs) | Tick ordering and validated prototype command entry points | Sprite frames, SCP-specific selection policy or inventory rules |
| [World/](World) | Map definition/geometry, doors, lighting, physical state and dynamic blockers | Whether an SCP is allowed to self-relocate |
| [Actors/](Actors) | Actor state, perceived player-route planning, movement progression, reservations and bounded patrol | Perception qualification or anomaly RNG |
| [Perception/](Perception) | Facing/lighting/occlusion/attention facts, eligibility and shared player evidence/interruption settings | SCP-specific reactions, relocation selection or hidden-entity tracking |
| [Anomalies/AnomalySystem.cs](Anomalies/AnomalySystem.cs) | Native module registry, scheduling and persistence binding | Mission-triggered teleports |
| [Anomalies/AnomalyWorldAccess.cs](Anomalies/AnomalyWorldAccess.cs) | Bounded read queries and commit-time spatial validation | The module's policy for when to request a move |
| [Anomalies/Scp650/](Anomalies/Scp650) | SCP-650 observation rule, placement query, poses, RNG, local retry and per-witness encounters | Directly setting actor/world positions or playing sound |
| [Operations/](Operations) | Data-defined duties/orders, bounded voice/radio delivery, observed physical transport, received reports and recovery conditions | SCP relocation policy, full employment, inventory-backed devices or public scripting |
| [Feedback/](Feedback) | Bounded acquired player messages, visibility episodes and delivery feedback | Copying hidden diagnostic state or implementing conversations |
| [Combat/](Combat) | Local turn/AP scheduler, training equipment/points, action continuations, perception-based autonomous decisions and save validation | Full injuries, public weapon definitions or ordinary hostility policy |
| [Diagnostics/](Diagnostics) | Bounded causal trace with world/local time, cause, subject and reason | Advancing simulation when inspected |
| [Persistence/](Persistence) | Versioned snapshot and detached validation before replacing live state | Replaying creation rewards or rerolling modules |

The confirmed production presentation is a Fallout 2-style bottom HUD with a Pip-Boy-like OSCP interface. Current engineering panels are replaceable adapters: they read acquired feedback/query state and submit validated commands through `SpatialSimulation`. Keep screen arrangement and interaction bindings in Godot presentation; the simulation owns authority, knowledge, AP, equipment rules and consequences. See the [presentation target](../../docs/20-event-log-and-tactical-training.md#confirmed-presentation-target).

Typical path:

```text
mouse/key intent
  -> SpatialSimulation command validation
  -> world/perception facts change
  -> anomaly module evaluates the facts
  -> module proposes a relocation
  -> host rechecks observation + destination + execution gate
  -> one physical body/pose/collision transaction
  -> diagnostic trace and later witness reaction
  -> Godot draws the committed state
```

The native module interface is an implementation seam, not a sandbox security guarantee or a promised mod API. SCP-650 owns per-person `complacency` state and selects threat/relaxed placement and pose pools. Its queries request explicit near-actor/radius, optional rear placement, clear-line geometry and unobserved cells. The shared host does not interpret complacency: its generic flag dispatcher forwards a validated target to the module, which accepts/rejects the named flag and actor kind. The host validates those declared parameters within the instance's granted scope. A future module can need another qualified capability; avoid adding its special rule to the movement/rendering code.

Snapshots use outer schema 5 for journal/tactical continuations and SCP-650 module schema 2. Matching schema-4 operation saves and schema-2/3 spatial saves remain supported. The optional operation snapshot persists order/report/rig state and is validated separately. Content identity includes the operation definition. Matching spatial-only schema-2/3 saves remain supported; schema 2 migrates with empty awareness history and enabled safety settings; older implicit-familiarity module saves are rejected rather than assigned invented flags. PlayerNavigation plans against known layout/perceived occupants and approaches blocked goals; actual steps retain full collision validation. Requested goals and route-local encountered cells preserve continuation/rerouting without revealing hidden occupants. A stopped actor retains a fractional edge separately from its cleared route, and new paths can continue or reverse without snapping. Shared awareness accepts acquired contacts and never pauses a clock or delegates player safety to the SCP module. Module state stores complacency separately from witness history. No automatic complacency metric exists. A failed restore builds/discards a detached candidate and leaves the current world intact. Do not mutate live state incrementally while validating a save. Read-only diagnostics return current facts without consuming RNG or adding events.

Run [the spatial checks](../../tests/SpatialChecks/Program.cs) and [SCP-650 checks](../../tests/SpatialChecks/Scp650Checks.cs) with `python3 tools/run_spatial_scene.py --check` from the workspace root. The [prototype guide](../../docs/15-spatial-prototype.md) covers the Godot smoke run and the [composition contract](../../docs/16-item-composition-and-crafting.md) records future inventory/crafting boundaries without adding empty implementations now.

The [recovery prototype guide](../../docs/19-personnel-orders-and-recovery-prototype.md) describes the command → delivered order → real work → received report flow and the explicit limits of radio/device and physical transport fixtures.

The [event log and tactical training guide](../../docs/20-event-log-and-tactical-training.md) explains evidence-gated player messages, local AP resolution, controls, provisional tuning and pending-action persistence. The unrestricted debug trace never supplies journal entries.
