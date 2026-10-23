# ADR-0005: Retain Godot after reviewing Falltergeist

**Status:** Accepted engineering decision under the user's delegated engine-choice authority

**Date:** October 22, 2026

## Context

The user proposed [Falltergeist](https://github.com/falltergeist/falltergeist) as a possible engine base, explicitly delegating the choice between extending it and recreating the desired world presentation in Godot. They clarified that hex-based actor movement with smooth transitions is appropriate. The game needs square construction tiles, a fixed angled sprite view, detailed pixel artwork and a persistent simulation spanning staff, sites, physical logistics, SCP modules and OSCP.

The user then explicitly clarified that Godot is suitable and Fallout 2/Falltergeist should be the blueprint for visual representation, movement and controls. The chosen implementation therefore targets that visible interaction feel inside Godot; it does not pursue compatibility with the original game's executable/content formats.

## Decision

Continue with Godot. Use the Godot 4 .NET distribution for presentation and a plain C# simulation core as the implementation architecture, with exact versions/platform integration to qualify in M0. Treat Falltergeist and Fallout 2 Community Edition as research references for projection, ground anchors, sprite offsets, layering and motion appearance. Do not fork Falltergeist or introduce it as a runtime dependency.

Use a hex actor/navigation grid with smooth real-time transitions, separately registered to the square construction/floor grid. Actors retain domain/floor, hex/path identity, physical footprints and authoritative progress through a movement segment; facing/attention remain independent. Sprite frames and cosmetic offsets represent committed progress rather than controlling its timing. Hex scale/orientation, grid registration, numeric representation, occupancy handoffs, collision and native pixel scale still require M0 evidence. This does not adopt Falltergeist's fixed map dimensions.

## Inspected evidence

Reviewed the repository's default `develop` branch through its primary documentation and source on the research date. This was a source/fit review, not a build, benchmark or complete feature audit.

- [README](https://github.com/falltergeist/falltergeist/blob/develop/README.md): C++/SDL Fallout 2 engine, documented startup using original game data and a roadmap toward full gameplay. This does not make a standalone original game impossible, but it demonstrates the current content-compatibility focus.
- [Build configuration](https://github.com/falltergeist/falltergeist/blob/develop/CMakeLists.txt): C++17, SDL/OpenGL dependencies and Fallout DAT virtual-filesystem components. These are coherent choices for that project, not defects.
- [HexagonGrid header](https://github.com/falltergeist/falltergeist/blob/develop/src/PathFinding/HexagonGrid.h) and [implementation](https://github.com/falltergeist/falltergeist/blob/develop/src/PathFinding/HexagonGrid.cpp): fixed 200×200 hex representation and cell-path routines.
- [Critter movement](https://github.com/falltergeist/falltergeist/blob/develop/src/Game/CritterObject.cpp): queued hex locations and animation-frame callbacks which move the actor to the next hex after offset thresholds are reached.
- [Location loading](https://github.com/falltergeist/falltergeist/blob/develop/src/Game/Location.cpp): Fallout map objects, floor/roof arrays and script resources. [Location state](https://github.com/falltergeist/falltergeist/blob/develop/src/State/Location.cpp) combines active-location simulation, scripts and rendering coordination.
- [Changelog](https://github.com/falltergeist/falltergeist/blob/develop/CHANGELOG.md): records implemented world-map/location and elevator work despite these also appearing in the README roadmap. Do not use the roadmap alone to claim those features are absent, or infer abandonment from old file dates.

Godot supplies maintained project/editor workflows, [tile layers and sorting controls](https://docs.godotengine.org/en/stable/tutorials/2d/using_tilemaps.html), [sprite animation](https://docs.godotengine.org/en/stable/tutorials/2d/2d_sprite_animation.html) and [UI controls/layout/theming](https://docs.godotengine.org/en/stable/tutorials/ui/index.html). Their suitability for this game's world/OSCP presentation is an engineering assessment. They do not supply our persistent domain simulation or certify deterministic gameplay.

## Tradeoffs and rationale

| Route | Useful starting point | Work remaining for this project |
|---|---|---|
| Extend Falltergeist | Existing Fallout-oriented renderer, formats, hex paths, animation and game UI | Adapt the content model, actor movement and animation authority; add/qualify procedural domains, persistent background simulation, employment, logistics, OSCP and the SCP module contract; maintain the fork |
| Godot presentation with custom simulation | General sprite/tile/UI tooling, scenes, editor and exports | Build the isometric adapter and all game-domain rules; qualify multi-floor composition, deterministic core, save/load and module isolation |

The decisive cost is adapting a Fallout recreation to this game's different simulation contract. Hex navigation now aligns with the user's chosen movement topology, making that part of Falltergeist more directly relevant. We still require substantially more than Fallout-style map traversal. A familiar first screenshot would not demonstrate autonomous site operations or anomaly interactions. In Godot, domain work remains explicit while general presentation/editor tooling is reused. This is a fit/maintenance judgment; no comparative development-time or performance result has been measured.

Our metered capability sandbox, branch-aware persistence and offscreen simulation semantics need separate qualification on either route. The inspected code and documented capabilities are sufficient for this architectural fit decision; implementation/performance evidence remains M0 work.

## Consequences and next proof

Keep original game content and source code out of the runtime dependency set. Learn from observed behavior and implement our own presentation metadata and simulation services. This review imported no Falltergeist code/assets and did not install or build either engine.

Begin M0 with the bounded two-room/corridor harness: square construction geometry, registered hex paths with smooth click movement, independent facing, ground anchors/layering, directional sight and door occlusion. Then qualify the observation-dependent SCP-650 fixture, pause/acceleration and save/resume using the same authority boundary. Sight/collision use the actor's committed movement progress, rather than remaining at an old hex center while only its image moves. Pixel-preserving display rounding must not change that progress or the timing of visibility loss. These are implementation targets, not completed checks.

Reconsider the engine only if measured M0 evidence identifies a material limitation and a demonstrably better alternative. [ADR-0004](0004-godot-and-simulation-core.md) records the original choice and limitations; this decision resolves the subsequent Falltergeist alternative and selects the .NET/C# integration direction. See [the sprite contract](../06-sprites-and-placeholders.md), [simulation contract](../02-simulation-and-world.md) and [Fallout placement research](../14-fallout-2-placement-research.md).

Subsequent time clarification: [ADR-0006](0006-real-time-exploration-and-tactical-turns.md) adopts real-time exploration and turn-based local combat, with complete local input waiting over a continuing world calendar and other areas. Smooth hex movement and independent animation authority remain applicable in both modes.
