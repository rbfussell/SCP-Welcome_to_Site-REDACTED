# Simulation code map

This is the engine-independent C# prototype. Godot input, drawing and diagnostics UI live in [game/Presentation](../../game/Presentation). The public script host, inventory, employment and complete combat are not implemented yet.

| Directory/file | Owns | Does not own |
|---|---|---|
| [SpatialSimulation.cs](SpatialSimulation.cs) | Tick ordering and validated prototype command entry points | Sprite frames, SCP-specific selection policy or inventory rules |
| [World/](World) | Map definition/geometry, doors, lighting, physical state and dynamic blockers | Whether an SCP is allowed to self-relocate |
| [Actors/](Actors) | Actor state, movement progression, reservations and bounded patrol | Perception qualification or anomaly RNG |
| [Perception/](Perception) | Facing/lighting/occlusion/attention facts and human/AI/any eligibility | Relocation selection, rendering or omniscient player knowledge |
| [Anomalies/AnomalySystem.cs](Anomalies/AnomalySystem.cs) | Native module registry, scheduling and persistence binding | Mission-triggered teleports |
| [Anomalies/AnomalyWorldAccess.cs](Anomalies/AnomalyWorldAccess.cs) | Bounded read queries and commit-time spatial validation | The module's policy for when to request a move |
| [Anomalies/Scp650/](Anomalies/Scp650) | SCP-650 observation rule, placement query, poses, RNG, local retry and per-witness encounters | Directly setting actor/world positions or playing sound |
| [Diagnostics/](Diagnostics) | Bounded causal trace with world/local time, cause, subject and reason | Advancing simulation when inspected |
| [Persistence/](Persistence) | Versioned snapshot and detached validation before replacing live state | Replaying creation rewards or rerolling modules |

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

Snapshots use outer schema 2 and SCP-650 module schema 2; older implicit-familiarity module saves are rejected rather than assigned invented flags. Module state stores complacency separately from witness history. No automatic complacency metric exists. A failed restore builds/discards a detached candidate and leaves the current world intact. Do not mutate live state incrementally while validating a save. Read-only diagnostics return current facts without consuming RNG or adding events.

Run [the spatial checks](../../tests/SpatialChecks/Program.cs) and [SCP-650 checks](../../tests/SpatialChecks/Scp650Checks.cs) with `python3 tools/run_spatial_scene.py --check` from the workspace root. The [prototype guide](../../docs/15-spatial-prototype.md) covers the Godot smoke run and the [composition contract](../../docs/16-item-composition-and-crafting.md) records future inventory/crafting boundaries without adding empty implementations now.
