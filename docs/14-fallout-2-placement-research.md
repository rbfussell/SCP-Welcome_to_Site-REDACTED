# Fallout 2: grids, placement and sprite projection

Research date: October 22, 2026. This investigates the user's visual reference. Subsequent decision: the user clarified hex actor movement with smooth transitions; the delegated Falltergeist comparison retained Godot with our own simulation. Historical findings below do not require reproducing the reference engine's animation-driven movement authority.

## Evidence and limits

The original [Fallout 2 manual](https://cdn.akamai.steamstatic.com/steam/apps/38410/manuals/Fallout2_manual_EN.pdf), “Movement Cursor” on printed page 34 and combat movement on pages 119/122, establishes hex-based movement/distance and six character facings.

Implementation details below were inspected in [Fallout 2 Community Edition](https://github.com/alexbatalov/fallout2-ce), a working reimplementation. It is evidence of that implementation and its representation of the original game's behavior/data; it is not Interplay's original source tree. No game binaries, proprietary sprites or repository source files were imported into this project.

## Findings

| Concern | Representation found | Evidence |
|---|---|---|
| Actor movement | Hex locations, paths and six facing directions | Original manual; CE `Rotation` and movement routines |
| Floor/roof placement | Separate square-tile layer and projection | CE square-grid definitions and floor/roof conversion/render functions |
| World object location | Hex tile plus elevation; additional image/screen offsets | CE `Object` record and location validator |
| Sprite image | Width/height, per-direction offsets and frame offsets | CE `Art` / `ArtFrame` records |
| Visual motion | Pixel displacement between hex-location updates | CE `_object_move` |

The CE [map definitions](https://github.com/alexbatalov/fallout2-ce/blob/main/src/map_defs.h) distinguish a 100×100 square layer from a 200×200 hex layer, with three elevations. These are reference implementation dimensions, not proposed limits for our procedural domains.

The [tile implementation](https://github.com/alexbatalov/fallout2-ce/blob/main/src/tile.cc) has separate hex/square screen conversions and floor/roof passes. Its square projection advances internal axes by screen vectors `(48, -12)` and `(32, 24)`: a skewed parallelogram rather than a symmetric 2:1 diamond. This observation follows from the conversion arithmetic; it does not determine our camera angle or world scale. Matching the visual reference requires evaluating the projection and asset perspective together.

The [object record](https://github.com/alexbatalov/fallout2-ce/blob/main/src/obj_types.h) separates tile, elevation, facing, frame and image-position fields. [Placement calculations](https://github.com/alexbatalov/fallout2-ce/blob/main/src/object.cc) project the hex location, add offsets and place the image around a bottom-center anchor. An image can extend well beyond its location cell; visible image bounds are not the movement grid.

The [art records](https://github.com/alexbatalov/fallout2-ce/blob/main/src/art.h) contain six-direction offsets plus frame dimensions/offsets. The [movement routine](https://github.com/alexbatalov/fallout2-ce/blob/main/src/animation.cc) applies frame offsets while walking and advances the object's hex when the displacement reaches the next step. Smooth-looking movement therefore does not establish freely continuous world positioning. It combines discrete path locations with image displacement.

“Vector positioning” is not an exclusive alternative to a grid: screen coordinates and offsets can be vectors while navigation still uses cells. The practical questions are what the authoritative location stores, which positions are legal and what determines collision, distance and observation.

## Application to this game

Preserve the useful separation between construction, actor location and sprite presentation. Square room/building tiles remain confirmed. The user now accepts hex actor navigation alongside them; the finished sprite-facing count remains a separate art decision.

The user clarified that apparent smoothness over a hex grid is appropriate. Use a separate hex actor/navigation grid with smooth real-time movement between path locations, while retaining square construction/floor tiles and independent facing. The earlier recommendation of freely positioned actors within construction cells was not retained. [ADR-0005](adr/0005-retain-godot-after-falltergeist-review.md) records the retained Godot route after inspecting Falltergeist. This design has not been implemented or benchmarked.

Actor hex/path identity, movement-segment progress, floor and footprint belong to the simulation. Derive current world position from that progress for collision and observation; a presentation adapter projects it and applies sprite anchors/cosmetic offsets. Any display interpolation must agree with committed movement and observation boundaries. The [first spatial prototype](15-spatial-prototype.md) now exercises the reference floor/hex registration, footprints, movement and input picking; broader scale, precision and production collision remain M0 qualification work. Smooth movement does not require making engine physics authoritative.

Do not reproduce animation-driven location updates merely to imitate the reference. Our existing contract makes semantic movement/action progress authoritative and sprite frames a representation of it. Animation replacement must not change speed or the instant an SCP loses observation.

Smooth movement applies both during real-time exploration and when a tactical action resolves. The adopted time split holds all local simulation during turn selection while the world calendar and other areas continue; [ADR-0006](adr/0006-real-time-exploration-and-tactical-turns.md) records that game-specific departure from a global wait.

The next bounded presentation proof should compare projected floor/wall geometry, a tall sprite's anchor, walk-behind order, click-to-world conversion and the facing diagnostic. Projection and actor-movement choices must remain explicit; the research does not silently supersede [the art contract](06-sprites-and-placeholders.md) or [simulation contract](02-simulation-and-world.md).
