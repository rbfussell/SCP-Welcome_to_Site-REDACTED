# Sprite presentation and diagnostic placeholders

Status: Proposed contract, revision 0.5. Modern sprite presentation, generated visibly labeled stand-ins, and distinct module-selected poses are confirmed direction. Colors, dimensions, direction count and initial animation counts below are proposals.

## 1. Art and development pipeline

Use modern, readable sprite artwork with sufficient resolution for the chosen camera and zoom. A deliberately retro/pixel-art treatment is not the intended art direction. Exact finished style and projection remain to be selected. Diagnostic blocks demonstrate state and integration, not the final character design.

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

Facing comes from the simulation's authoritative orientation/attention record. North places the bar on the top edge, east on the right, south on the bottom, west on the left in the initial north-up top-down reference. The label states the direction explicitly.

Standing still retains facing. A pawn can walk backward or strafe while looking somewhere else; the bar must not silently follow velocity. Looking and sprite animation remain separate inputs. If attention/eye orientation differs from body facing, the diagnostic inspector exposes both and identifies which supplies the marker; the initial character convention uses the perception-facing orientation. Eligibility for SCP observation also depends on eyes, light, attention and obstruction; a facing bar alone does not prove eye contact.

Propose four directions initially. A later eight-direction manifest uses corner markers for diagonal facings and explicit vectors. Isometric projection or camera rotation must project the marker from world facing into screen coordinates and keep the world-direction letter correct. Do not promise those variants until their presentation tests exist.

## 4. Animation behavior

Propose `idle` with one frame and `walk` with three frames per direction for the first reference. Production counts are data, and different classes can use different totals. The label advances `walk:1/3`, `walk:2/3`, `walk:3/3` along with the selected sprite frame.

Walking phase derives from committed locomotion/stride progress. Pausing freezes it; stopping selects idle; failed movement does not show successful walking. State metadata specifies how a non-looping action maps to semantic action progress and how a blocked/held state is represented. Preview playback can use a separate presentation clock and must be labeled preview.

Animation is a view of the simulation. Changing sprite frame counts must not change movement speed, attack timing, door collision, pickup completion, employment progress, or containment. Gameplay events come from actions and their semantic checkpoints. Keep anchors and attachment points consistent when substituting artwork. A missing production frame uses its labeled placeholder; an undeclared state emits a conspicuous `MISSING STATE` fallback and diagnostic rather than silently displaying idle.

An illustration/pose is distinct from a temporal frame. SCP-650 initially has approximately six module-selected pose keys, each with its own placeholder; it must not cycle through those poses as a walk animation. A changed pose footprint is validated by the module's placement rules. A module such as SCP-1499 can supply its own environmental/entity sprite set while using the same diagnostic bindings.

Clothing/gear can later use attachment/layer composition. Initial stand-ins show equipment as diagnostic information until layer art is available. Art variety must not multiply the NPC decision state machine.

## 5. Development controls and deliverables

Provide a sprite catalog/preview showing all declared illustrations and animation frames, category colors, state keys, facing and anchor. Provide in-world diagnostic overlays even after finished art is installed. Allow label scale/visibility controls for crowded scenes; the developer can always inspect full state. Debug names and hidden state are development tools and do not grant the release player information their character lacks.

The engine-independent [reference generator](../tools/generate_diagnostic_sprites.py) creates a contact sheet, labeled sample PNG frames and a JSON manifest under `assets/development/diagnostic-sprites/`. It is a deterministic asset sample, not an implemented animation controller, runtime overlay system, atlas importer or game scene. It uses Pillow already available in the development environment.

Run from the workspace root:

```bash
python3 tools/generate_diagnostic_sprites.py
```

M0 qualifies the presentation binding and facing contract; M1 qualifies its use in the running world. See SPR-01 through SPR-06 in [scope and acceptance](04-scope-and-acceptance.md).
