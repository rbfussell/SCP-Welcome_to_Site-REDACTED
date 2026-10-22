# Sprite presentation and diagnostic placeholders

Status: Revision 0.7. Godot, a fixed isometric/three-quarter field view, square logical tiles and detailed Fallout 2-inspired sprite artwork are confirmed direction. Generated visibly labeled stand-ins and distinct module-selected poses remain confirmed. Colors, dimensions, exact projection angle, direction count, production method and initial animation counts below are proposals.

## 1. Art and development pipeline

The user's visual reference is Fallout 2: detailed sprite characters, substantial environmental detail and a grounded, textured appearance. “Retro” here refers to that classic PC RPG sensibility. The target is not a low-color NES/RPG Maker-style treatment. Use enough color, shading and resolution to distinguish bodies, equipment and surface materials at the chosen gameplay zoom. Do not interpret the earlier “modern sprites” wording as rejecting the now-confirmed Fallout 2 reference.

The field camera is fixed isometric/three-quarter. Square logical tiles underpin terrain, room construction and procedural placement; their screen projection and the visible sprite canvas are separate from simulation geometry. This choice does not adopt Fallout's game rules or grid model. Exact projection ratio/angle, world-unit scale, sprite dimensions, native display scale and zoom limits must be qualified together before producing the finished asset set.

The desired appearance can be reached through painted sprites, model-to-sprite rendering or a controlled combination; the production method is not selected. A rendered-looking sprite aesthetic does not require a real-time 3D character renderer. Character silhouettes, material shading, perspective, light direction and scale need a shared art specification so modular rooms, procedural equipment and separately authored SCP assets look coherent. Avoid copying proprietary artwork; the games are visual references.

Square asset cells do not require every image to fill its cell or every entity to occupy exactly one tile. Transparent padding, tall sprites and multi-tile objects need explicit anchors and bounds. Retain separate logical occupancy/collision, render bounds and interaction geometry. Walls, people, trees and furniture may visually overlap adjacent projected tiles without changing world ownership or physical reach.

Diagnostic blocks demonstrate state and integration, not the final character design. Their six category colors are development information, not the finished scene's palette.

Each visual definition declares illustration/animation states before finished art exists. Generate stand-ins for every declared state, facing and frame, so missing production art never hides a missing state binding. A finished animation replaces the corresponding visual asset binding without changing an entity definition, behavior script or gameplay timing.

Use logical keys such as `character.human / walk / north / frame-02`. Keep the state name, facing, frame count, durations/phase mapping, loop behavior, anchor, draw bounds, scale and attachment points in versioned presentation metadata. Atlas coordinates and filenames are implementation details behind that key. Changing a frame count requires an explicit metadata update and regenerated placeholders, not guessing from filenames.

## 2. Category palette and labels

| World category | Proposed stand-in color | Visible category label |
|---|---|---|
| Landscape / map base | Earth brown `#6B5D3E` | LANDSCAPE |
| Map objects: trees, rocks, immovable vehicle scenery | Slate `#61717C` | MAP OBJECT |
| Building tiles | Purple `#9B7BA6` | BUILDING |
| Objects: items, furniture, usable fixtures, movable vehicle visuals | Amber `#C88E41` | OBJECT |
| NPC | Teal `#3D8FA3` | NPC |
| Player | Magenta `#C74D79` | PLAYER |

Use a bright green facing bar `#61FF70`, a dark outline, and readable text. The category remains recognizable alongside the marker. Category text and a cardinal letter supplement color.

Character diagnostics show the instance's name and `state:frame/total`, for example `Mara Chen`, `walk:2/3`, `N`. Object diagnostics show a definition/instance label and state such as `door:opening:2/4` or `rack:occupied:1/1`. Static landscape needs no invented facing; label its variant/illustration instead. Reserve room for text without changing collision or occupied inventory cells.

Generate reusable category/state images. Render instance names and diagnostic text as overlays in the eventual engine; do not require a new artwork file for every procedural person's name. The accompanying reference generator precomposes sample labels for inspection only.

## 3. Direction is authoritative information

Facing comes from the simulation's authoritative orientation/attention record. The existing engine-independent north-up reference places north on the top edge, east on the right, south on the bottom and west on the left. It is a diagnostic sample, not the confirmed game's camera projection. In the fixed isometric view, project the world-facing vector into screen space before positioning the green marker; retain the explicit world-direction label. World north is not automatically screen up.

Standing still retains facing. A pawn can walk backward or strafe while looking somewhere else; the bar must not silently follow velocity. Looking and sprite animation remain separate inputs. If attention/eye orientation differs from body facing, the diagnostic inspector exposes both and identifies which supplies the marker; the initial character convention uses the perception-facing orientation. Eligibility for SCP observation also depends on eyes, light, attention and obstruction; a facing bar alone does not prove eye contact.

Propose four world-facing directions for the first diagnostic integration. The finished direction count remains open; a later eight-direction manifest can use explicit vectors and appropriate markers. Fixed isometric projection must be tested from the first Godot harness. Camera rotation is not part of the confirmed field-view design. Turning the character, changing body facing and moving diagonally must not make the UI mislabel its observation direction.

## 4. Animation behavior

Propose `idle` with one frame and `walk` with three frames per direction for the first reference. Production counts are data, and different classes can use different totals. The label advances `walk:1/3`, `walk:2/3`, `walk:3/3` along with the selected sprite frame.

Walking phase derives from committed locomotion/stride progress. Pausing freezes it; stopping selects idle; failed movement does not show successful walking. State metadata specifies how a non-looping action maps to semantic action progress and how a blocked/held state is represented. Preview playback can use a separate presentation clock and must be labeled preview.

Animation is a view of the simulation. Changing sprite frame counts must not change movement speed, attack timing, door collision, pickup completion, employment progress, or containment. Gameplay events come from actions and their semantic checkpoints. Keep anchors and attachment points consistent when substituting artwork. A missing production frame uses its labeled placeholder; an undeclared state emits a conspicuous `MISSING STATE` fallback and diagnostic rather than silently displaying idle.

An illustration/pose is distinct from a temporal frame. SCP-650 initially has approximately six module-selected pose keys, each with its own placeholder; it must not cycle through those poses as a walk animation. A changed pose footprint is validated by the module's placement rules. A module such as SCP-1499 can supply its own environmental/entity sprite set while using the same diagnostic bindings.

Clothing/gear can later use attachment/layer composition. Initial stand-ins show equipment as diagnostic information until layer art is available. Art variety must not multiply the NPC decision state machine.

### Isometric composition and art qualification

Render order derives from world position, ground anchors, height/floor and declared layers, rather than an entity's entire image rectangle. Tall or multi-tile assets may need authored split layers/occlusion masks; qualify the chosen approach in the presentation harness. Visual shadows and decorative pixels do not become authoritative collision or line-of-sight geometry.

Picking and placement resolve screen input through the selected projection to real world targets/footprints. A wall cutaway or other visibility aid must respect the player's permitted information. A character passing behind furniture remains at its actual position; sprite overlap cannot invent observation or allow an attack through an obstruction.

Before committing production scale, propose one small art qualification scene: a floor/wall corner, doorway, tall storage fixture, character and SCP-650 pose. Check relative size, walk-behind order, selection, facing, text readability and a swap between labeled stand-ins and a representative finished sprite. This is a bounded art/presentation check, not an extra playable map or a requirement to finish all art before M0. Keep production resolution, frame totals and equipment-layer budget open until that evidence exists.

## 5. Development controls and deliverables

Provide a sprite catalog/preview showing all declared illustrations and animation frames, category colors, state keys, facing and anchor. Provide in-world diagnostic overlays even after finished art is installed. Allow label scale/visibility controls for crowded scenes; the developer can always inspect full state. Debug names and hidden state are development tools and do not grant the release player information their character lacks.

The engine-independent [reference generator](../tools/generate_diagnostic_sprites.py) creates a contact sheet, labeled sample PNG frames and a JSON manifest under `assets/development/diagnostic-sprites/`. It is a deterministic asset sample, not an implemented animation controller, runtime overlay system, atlas importer or game scene. It uses Pillow already available in the development environment.

Run from the workspace root:

```bash
python3 tools/generate_diagnostic_sprites.py
```

M0 qualifies the presentation binding and facing contract; M1 qualifies its use in the running world. See SPR-01 through SPR-06 in [scope and acceptance](04-scope-and-acceptance.md).
