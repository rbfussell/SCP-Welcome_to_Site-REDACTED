# First Godot spatial scene

Status: Runnable M0 spatial/observation subset, updated October 23, 2026. A native SCP-650 observation/relocation module now runs in the scene. A bounded orders/OSCP/trolley recovery proof is now available; complete facility operations, procedures, script hosting, combat and the full M0 gate remain open.

## Run and verify

From the repository root:

```bash
python3 tools/run_spatial_scene.py
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --smoke work/preview/spatial-scene/scene.png
```

The [launcher](../tools/run_spatial_scene.py) uses Godot .NET 4.7.1 and the .NET 8 SDK selected by [global.json](../global.json). It finds `godot-dotnet` on PATH; alternatively pass `--godot /path/to/the/mono/executable`. The regular non-.NET `godot` cannot run this scene. First restore requires the Godot SDK packages from NuGet unless already cached. The asset generator uses Python/Pillow.

This workspace's colon/brackets caused direct Linux MSBuild import failures and a .NET app-host initialization failure even through a symlink. The launcher copies only `game`, `src`, `tests` and `global.json` to a temporary directory, builds/runs there and removes the copy on exit. It never copies the wiki archive or Git history. Edit this repository's source; temporary runtime files are disposable. Persistent prototype saves remain under ignored `user-data/`, and import/tool logs under ignored `work/preview/spatial-scene/`. The launcher installs no engine or global SDK configuration.

Tested: Godot `4.7.1.stable.mono.official.a13da4feb`, .NET SDK `8.0.128`, runtime `8.0.28`, Linux, compatibility renderer. Other platforms, exports and performance budgets remain M0 work. The visual smoke run uses `xvfb-run` when available, otherwise a desktop display; this sandbox requires access to display sockets outside its default restrictions.

## Controls

| Input | Prototype behavior |
|---|---|
| Left click floor | Commit a reachable hex route; finish/cancel before replacing a moving route |
| Right click / Ctrl-click | Stop current travel exactly in place, then face the target; Shift-click explicitly moves while keeping facing |
| Shift-click floor | Move while maintaining facing |
| E | Open/close a nearby door; occupied/crossing doors cannot close |
| O / K / F8 | Change Chen's attention / blink the avatar / toggle prototype lighting |
| F2 | Switch direct-sight field presentation / unrestricted diagnostic view |
| F4 | Switch textured statue atlas / original diagnostic pose sprites |
| J | Toggle the avatar's module-owned complacency flag; future automatic metric remains undecided |
| M / R | Mute presentation audio / reduce discovery border effect; caption remains available |
| C / G / B | Toggle white collision boundaries / full hex grid / square construction grid in diagnostics |
| L / H | Toggle directional sight overlay / feathered player wall cutaway (full-height mode) |
| W | Toggle all ordinary walls between full height and short dark bases; also available through the field-panel wall button |
| F6 | Hold all local progress while world/off-site counters continue |
| Space | Global manual pause for this single-player proof |
| 1 / 2 / 3 | Set 1× / 2× / 4× speed |
| F5 / F9 | Save / restore prototype state |
| Middle drag / wheel / Home | Pan / zoom / reset camera |
| Esc | Cancel remaining movement at a safe hex boundary |

Two independent field-view checkboxes, **Stop on my anomaly sightings** and **Stop on communicated sightings**, default on. A new qualified anomalous encounter stops the avatar exactly in place and clears the path; the world continues. New clicks can continue or reverse the fractional step without snapping. Nearby unobstructed human voice reports remain available. The optional operation now supplies powered issued-radio/physical-terminal fixtures with received records; general radio hardware and remote SCiPnet delivery remain unimplemented. Contact arrows show recorded last-known/reported bearings with age, without following hidden subjects. [The awareness contract](18-awareness-and-encounter-entry.md) separates this movement safety from future automatic hostile combat entry.

The default field view follows the avatar at a closer scale and shows people/SCP-650 only under actual direct character perception, including facing, attention, closed eyes, light and obstruction. Known static facility geometry remains visible in subdued colors. F2 exposes the labeled full-map diagnostic view, where the green sight overlay differs from render visibility. This is a bounded presentation filter, not a complete fog-of-war, memory, surveillance or information-sharing system; navigation and debug fixture controls still use authoritative prototype state. The off-site area is a coarse counter, not a second finished facility. F6 is an execution-hold fixture, not AP combat. Door use is an immediate validated prototype interaction; timed equipment/actions remain future work. Chen starts observing SCP-650; O releases that attention so the module can relocate. Turn the avatar toward a discovered statue to establish direct observation. The **View**, **SCP-650** and **Trace** tabs separate presentation controls, rule explanations and causal events. Trace export writes readable JSON under the prototype's user-data directory.

## Encounter presentation

[GroundView](../game/Presentation/GroundView.cs) renders the existing floor textures through subdivided meshes. Shared world-space shading samples and interpolation soften the cosmetic cone across tile boundaries, avoiding whole-tile brightness changes from sprite anchors. Cone/range fade uses the shared sight constants, but only exact perception decides entity visibility and anomalous observation. Shading rebuilds only when relevant avatar, eye, lighting, door or view-mode state changes; rendering an unchanged held scene reuses it. This is presentation shading, not a flashlight, new sensor, or electrical/HVAC system.

[FieldPresentation](../game/Presentation/FieldPresentation.cs) filters dynamic bodies and consumes only fresh avatar `startled` events. It never draws RNG, mutates the world, counts a viewport as observation or cues a hidden relocation/remote witness. Restore resets the consumer baseline so historical events do not replay. Local-clock caption/border progress waits during a local hold.

[EncounterAudio](../game/Presentation/EncounterAudio.cs) generates quiet room ambience and a short low discovery accent as presentation-only audio, with mute support and no NPC hearing event. [EncounterOverlay](../game/Presentation/EncounterOverlay.cs) provides a readable discovery caption and subtle border, without flashing or camera shake. R disables the border; sound/captions remain independently useful. Preferences currently last for the running scene, not across launches. The atlas is fixed-view first-pass art, not a finished six-direction character set or qualified furniture-pose attachment system.

## Fallout 2 mapping comparison

Rechecked the primary [CE tile implementation](https://github.com/alexbatalov/fallout2-ce/blob/main/src/tile.cc), [map definitions](https://github.com/alexbatalov/fallout2-ce/blob/main/src/map_defs.h), [object record](https://github.com/alexbatalov/fallout2-ce/blob/main/src/obj_types.h) and [object rendering/blocking](https://github.com/alexbatalov/fallout2-ce/blob/main/src/object.cc). This is a reimplementation reference, not original Interplay source. No game assets or repository source code were imported.

| Concern | Reference finding | This scene |
|---|---|---|
| Ground | Square floor/roof layer separate from objects | Square construction-plane cells beneath walls/objects; roof rendering not implemented |
| Floor projection | Basis `(48,-12)` and `(32,24)` | Same basis at reference scale; camera zoom applied afterward |
| Hex envelope | Diagnostic 32×16 image with corners near `(16,0)`, `(31,4)`, `(31,12)`, `(16,15)`, `(0,12)`, `(0,4)` | Continuous centered envelope `(0,-8)`, `(16,-4)`, `(16,4)`, `(0,8)`, `(-16,4)`, `(-16,-4)`; raster endpoints differ by one pixel |
| Hex spacing | Six neighbors including `(32,0)` and `(16,12)` | Axial bases `(32,0)` and `(16,12)`, four hex areas per floor-cell area |
| Walls | Separate hex-located objects with elevation and image offsets | Short U/V wall sprites with hex-registered endpoints, separate anchors and optional visual cutaway |
| Blocking | Blocking critter/scenery/wall at a hex, flags and neighboring multihex checks | Blocked cells from physical footprints, plus wall-segment crossing rejection between free cells |
| Sprites | Project hex, add offsets, place image at bottom-center ground anchor | Generated anchor manifest; short wall segments/objects/actors sorted by projected ground depth |

Our registration is `u=q/2`, `v=r/2+q/4`. Projection gives `x=32q+16r`, `y=12r`. Floor corners are `(0,0)`, `(48,-12)`, `(80,12)`, `(32,24)`. Wall artwork follows the projected construction axes; the ground art is not the movement grid. The white overlay shows actual blocked hexes and solid wall segments, not sprite silhouettes. Opening a door updates path and sight obstruction together.

Explicit wall-segment collision is our extension. This is not a bit-exact recreation of Fallout's mapper, collision or formats, and it adopts no fixed 200×200 map limit. See [the original research](14-fallout-2-placement-research.md).

## Implementation and evidence

The [plain C# core](../src/SiteSimulation/SpatialSimulation.cs) owns 50 ms steps, committed movement, independent facing, occupancy reservations, door validation, a bounded NPC patrol, local/world progress and a countdown fixture. The [map](../game/Content/spatial-facility.json) defines two rooms/corridor, three doors, cabinet, crate, actors and a native SCP-650 instance. [Godot input/UI](../game/Presentation/SpatialScene.cs) and [rendering](../game/Presentation/WorldView.cs) consume this state without controlling movement through animation callbacks.

The [asset generator](../tools/generate_spatial_assets.py) makes deterministic original raster stand-ins and frame/anchor metadata, including six discrete SCP-650 poses. A separate [appearance manifest](../game/Assets/scp650-appearance.json) binds the six textured atlas regions/scales/anchors without changing pose identity or gameplay. F4 retains the diagnostic-art path; regeneration of stand-ins does not overwrite the new atlas. [Asset provenance](../game/Assets/scp650-art.md) records its source and limitations. `--walk-frames N` changes presentation without changing movement duration. This is diagnostic artwork, not final production art. Schema-4 saves preserve optional operation state and awareness settings, acquired evidence/acknowledgements and stopped fractional edges, while retaining content identity, actor routes/segment progress, facing/attention/blinking/reactions, patrol continuation, doors/lighting, separate clocks/holds, anomaly bodies, module RNG/deadlines and trace. Invalid incompatible state is rejected before commitment; general content migrations and branch restart are not implemented. Earlier outer-schema-1 or SCP-650-module-schema-1 prototype saves are rejected with an explanation and remain untouched unless the user explicitly overwrites them with a new save. Matching spatial-only outer-schema-2/3 saves with the current module schema upgrade to outer schema 4; schema 2 receives an empty awareness ledger and both safety settings enabled. The operation profile has a distinct content identity and requires its operation snapshot. The SCP-650 module schema remains 2.

Engine-independent checks cover source-derived projection vectors, positive/negative hex picking and polygon interiors, door path/sight changes, permitted-step wall crossings, mid-segment persistence/replay, held local timers with continuing world counters, global manual pause, attention-preserving movement and invalid-save rejection. Dedicated [SCP-650 checks](../tests/SpatialChecks/Scp650Checks.cs) cover direct observer profiles, commit-time validation, destination occupancy/visibility, cross-door relocation, no-candidate retry, unchanged observed poses, one reaction per witness/encounter and module save/hold behavior. Additional [complacency checks](../tests/SpatialChecks/ComplacencyChecks.cs) qualify per-person flags, threat/relaxed geometry across fixed seeds, host rejection and persistence. [Field presentation checks](../tests/SpatialChecks/FieldPresentationChecks.cs) verify direct sight and one-shot avatar feedback without world mutation. Godot smoke input checks the exact clicked destination, O attention loss and relocation, F6 hold, continuing world and module restoration, then a reproducible avatar look-away/RMB discovery fixture, one cue, J flag change while observed and F2/M/R controls before capturing field and diagnostic viewports.

Visual inspection confirmed the skewed perspective, direction/state labels, collision overlay, cutaway and visible controls. A click-coordinate mismatch was caught and fixed by transforming the input event's own viewport position. Mesa software rendering was used; this is not a performance qualification.

The [SCP-650 verification record](reviews/scp650-prototype-checks.md) records the current module/input checks, observed results and their limits.

F8 currently changes a shared lighting boolean. Electrical circuits, backup supplies, room temperature, HVAC and qualified utility maintenance are not implemented. Their staged contract is [facility infrastructure](17-facility-infrastructure.md).

Remaining work includes generalized multi-floor/split-sprite occlusion, production art scale, critical events under detail changes, script isolation/quotas, domain transfers, AP/initiative and incoming cross-zone arbitration. The [bounded operation proof](19-personnel-orders-and-recovery-prototype.md) now supplies received OSCP records, delivered personnel orders and timed observed-trolley recovery. Full SCP containment/transport procedures, human-attended surveillance, employment/logistics/general OSCP integration and the complete player knowledge/sharing model are not implemented. The [code map](../src/SiteSimulation/README.md) documents the actual system boundaries, and [item composition](16-item-composition-and-crafting.md) records future inventory/crafting architecture. This proof does not certify the full rows in [scope and acceptance](04-scope-and-acceptance.md).

The normal launcher loads `scp650-operation.json`, starts three actual observers and adds physical terminal/trolley fixtures and schema-4 operation saves. The original `--smoke` still runs the spatial-only fixture. Use `--operation-smoke PNG` for the added operation controls; T opens OSCP only while in reach. See the recovery guide for play instructions and save compatibility.

## Click-to-move obstruction handling — October 23

A valid in-map click keeps its requested destination. Use a complete route when one exists; otherwise approach the closest reachable boundary instead of rejecting a distant click on an occupied or inaccessible point. The field preview uses the known fixed facility layout and directly perceived actors/anomaly bodies/movable equipment. Unseen occupants and anonymous reservations cannot silently veto the click or alter its preview. The field hover readout says `NO KNOWN BLOCK` rather than exposing hidden collision state; diagnostics retain authoritative geometry.

Actual movement still revalidates collision and occupancy before every step. An encountered obstruction can supply an identity-free cell to avoid for this movement command. Try an alternate route to the same destination; if none gets closer, stop before the obstruction with ordinary blocked-path feedback. Do not overlap the body, disclose its identity or pause either clock. An occupied goal itself remains unreachable until it clears. If access opens during an approach, retain the original destination and continue through a newly valid route. Explicit cancellation and anomaly-awareness stopping clear that continuation.

[PlayerNavigation](../src/SiteSimulation/Actors/PlayerNavigation.cs) separates this planning view from [MovementSystem](../src/SiteSimulation/Actors/MovementSystem.cs)'s physical checks. NPC work and rig plans retain their existing validated routing; this change applies to player click travel. Fixed walls/doors/props still use the prototype's known-plan model, not a finished exploration/door-memory system. Route-local encountered cells are cleared on completion/cancellation/stopping rather than acting as a permanent obstacle map.

Optional actor fields `MoveGoal` and `BumpedCells` are saved/validated within outer schema 4. Older matching snapshots without them retain their committed route endpoint; new commands supply the explicit destination. Unknown future route occupants remain legal in a save, while physical step execution checks them on arrival. [Pathfinding checks](../tests/SpatialChecks/PathfindingChecks.cs) cover dark/visible occupied goals, actual approaches/stops, vacated destinations, walls/opening access, contact-driven detours, cancellation and saved intent. The operation render fixture additionally submits a real Shift-click into an unseen occupied corridor hex and verifies travel followed by a safe stop.

## October 24 journal and tactical training

The current scene adds the [player journal and tactical training proof](20-event-log-and-tactical-training.md). Outer schema 5 supersedes the schema-4 references above while retaining compatible migration; SCP-650 module schema stays 2. The lower panel keeps 100 acquired messages with five visible rows. The training guide records AP/input, independent local/world scheduling and the explicit limits of temporary sidearms and nonlethal points. This does not complete M0/M1 acceptance.

## October 24 default facility art

Default scenes now render [the generated facility stand-in set](../game/Assets/facility-standins/README.md): Chen, Ortiz and the director have separate six-facing idle/walk sheets, surfaces map onto the existing projected geometry, and crate/cabinet/CRT/trolley have registered sprites. F4 restores diagnostic art for the whole scene. Character appearance/ordered-layer selection lives in the Godot presentation catalog, with no simulation or save-schema change. These are baked development outfits, with aligned body/head/clothing composition and additional action animations still to qualify.

The [employment prototype](22-employment-prototype.md) now replaces normal-scene synthetic standing observation orders with scheduled posts and relief. Outer schema 6 adds employment continuation. Updated operation content has a new identity; matching legacy fixtures retain their previous migration support.

## Lowered-wall view

**W**, or the wall-view button near the top of the field panel, switches between full-height walls with the existing local cutaway and short bases across the entire map. The View tab also provides a Lower all walls toggle. The base is approximately 31% of full height, cropped at the dark lower panel of the current wall texture rather than squashing the full image. A small cap follows the lowered top edge. Diagnostic artwork supports the same mode. Doors, open door frames, props and people retain their normal appearance.

The local feather is dormant while every wall is lowered; returning to full height restores the current H preference. This is presentation state only: collision, pathfinding, observation, concealment, anomaly rules and save data do not change. Lowered walls make already-permitted characters and objects easier to read without revealing unseen entities. The setting currently lasts for the scene session.

The native spatial smoke captures full, lowered, restored and diagnostic-lowered views, checks W and the actual field button, verifies all ordinary walls are lowered while doors retain full height, and compares unchanged world/save state, sight and walkability.
