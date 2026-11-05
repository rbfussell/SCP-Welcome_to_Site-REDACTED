# Paperdoll pocket segments: jacket, trousers and backpack

Implemented November 5, 2026. **I → Sprites** now exposes the jacket chest pocket, all four existing front trouser compartments and the backpack main/side compartments. The viewer-left jacket chest segment follows both open and closed jacket artwork. It extends [worn container drops](52-worn-container-drops.md), [sprite equipment drops](51-sprite-equipment-drops.md), [physical pocket coverage](31-pocket-coverage-prototype.md) and [three-view navigation](27-three-view-inventory-and-body-fit.md). See the [wiki mechanic](../source_material/game-wiki/paperdoll-pocket-segments.html).

## Illustrated parts and real compartments

`PaperdollPart.pockets` is an optional array of `{ region, side, polygon }` source-local registrations. The native open/closed coat frames each register a front polygon for the existing `chest` region. The other drawn pockets/badge are not automatically storage. The back has no chest registration. Whole-part `dropRegion` remains available for backpack/pouch material.

Polygons are normalized to their own part's source rectangle, with bounded point/count validation, a valid side, nonempty region and triangulatable nonzero-area geometry. They do not add a pocket, capacity, fastening or physical mouth mask. The existing region ID supplies all gameplay facts. Region validity is checked against the real item definition when the target is used.

Picking first requires the topmost actual worn alpha, then tests the matching source-local segment. Tint uses that same texture and polygon within the normal layer order; transparent pixels stay empty and later clothing/straps still occlude it. A front-mounted pack hides the covered chest segment. Visual exposure never overrides physical body-surface coverage. Current outlines are manually authored stand-in registration, not automatic segmentation or a production clothing generator. Source PNGs remain unchanged.

## Pointer actions and physical state

Hover highlights the pocket rather than the entire jacket and labels its real compartment/secured state. Double-click inspects the owning jacket and opens the named pocket through the normal browser/closure service. If already open and accessible, this is ordinary inspection. If closed, normal time/AP is required before contents appear. Manual pause refuses opening; saved paid opening restores exactly.

Right-click targets the pocket's **Button/Unbutton** action and its own securing preference, rather than the jacket's Zip/Unzip action. Holding/dragging still moves the owning worn garment through normal access/removal rules. The pocket is a compartment, not a separate movable item.

Dropping onto the segment uses the existing scoped packing path. Closed/inaccessible/full pockets refuse, without automatic opening, fitting clothing instead, or finding another pocket. An open pocket receives normal paid AutoPlace with source retention until commitment. Existing mount → storage → body-fit arbitration remains as documented in the earlier slice.

Zipping/unzipping the jacket front does not change the pocket's independent fastening. Closing views and Esc retain the existing per-fastening/browser rules. Flap animation and an explicitly traced zipper segment are not added here; existing jacket controls remain available.

## Trouser extension

The trouser frame registers `left`, `right`, `cargo-left` and `cargo-right` on its front source crop. These reuse the same segment renderer, alpha picking and real compartment handlers as the jacket. Existing item IDs, physical mouth masks and source PNGs are unchanged. Left/right labels retain the existing gear-view region convention; no coordinate-based renaming occurs.

Hip openings remain **unsealable and snug**. Double-clicking an accessible hip shows its grid without opening work; its menu has no fastening action or securing preference. Cargo flaps remain buttoned and use normal paid Open/Close with their own Button/Unbutton menu. Hover instructions say “right-click actions” for both, avoiding an invented hip fastening.

A zipped coat still covers hip mouths while lower cargo pockets remain accessible. Opaque coat pixels prevent clicking a hidden trouser segment at all; any visible but physically obstructed segment still refuses through normal coverage/access validation. Unzipping restores authored physical hip access but does not make opaque side panels transparent: direct targeting requires visible material. Remove the coat to inspect all four front contours. The enlarged trouser view remains available for explicitly choosing pockets when appropriate. Rear artwork remains decorative: no rear-access segment or new rear compartment is inferred from painted pockets.

[Native trouser input checks](../game/Presentation/SpatialScene.PaperdollTrouserPocketSmoke.cs) stage coat/pack through real ground handling; qualify all four contour targets and read-only hover, both hip grids/menus, paused/paid cargo opening, correct cargo menus, direct source-grid storage, covered hip refusal versus visible cargo, unzip access and exact continuation. They run inside the existing `--pocket-smoke` fixture and full inventory regression.

## Backpack extension

Both bag frames register source-local `main` and `side` contours on their mounted side: `back` in frame 8 and `front` in frame 9. The main contour follows the upper zipped panel; the side contour follows the visible buckled side pocket. Harness parts have no pocket/storage registration. The painted front pouch is not a third compartment. Outside explicit contours, the established bag-material drop fallback still targets `main`; clicking that material remains ordinary pack inspection. This fallback does not turn decorative pouches into separate inventories.

The segment takes precedence over the part's `dropRegion`: a closed/full side pocket refuses without trying the main section. Double-clicking a segment opens that named compartment via normal paid handling; right-click uses its Zip/Unzip or Buckle/Unbuckle action and independent preference. Hover/drop tint stays on the chosen contour, even where the enclosing bag part has a default drop region. Holding/dragging moves the owning loaded backpack normally. Front/back registration and actual item IDs keep two simultaneously worn packs distinct.

[Native backpack pocket checks](../game/Presentation/SpatialScene.PaperdollPackPocketSmoke.cs) exercise read-only contour hover, both fastening menus, paused/paid opening, exact pending restore, closed-side drop refusal while main is open, paid side storage with source retention, harness exclusion, both mounting sides and two pack identities. They run alone with `--pack-pocket-smoke`, within `--pocket-smoke`, and in full inventory checks. No extra compartments, source-image edits, fastening animation, simulation rules, content identity or save fields are introduced.

## Evidence and scope

[Native pocket checks](../game/Presentation/SpatialScene.PaperdollPocketSmoke.cs) qualify hover, paused/paid double-click opening, exact pending restore, the correct context fastening, direct compartment storage, both jacket visual states, rear exclusion and front-pack occlusion. They run alone with `--pocket-smoke` and inside paperdoll/full inventory regressions. The [runtime review](reviews/inventory-runtime-checks.md) records actual results and warnings.

The simulation-domain pocket/access/closure/packing services, native definitions, physical masks, kit identity and save schema **15** are unchanged. UI segments/highlights are reconstructed from presentation registration. World characters still use baked art.

Qualified registrations are the front jacket chest pocket, front trouser hip/cargo pockets and backpack main/side on the actual bag side. Shirt pockets, rear trouser pockets, additional backpack compartments beyond main/side, arbitrary body variants, animated flaps, automatic source segmentation and world wardrobe composition remain future work. Each later segment must link to an actual region and respect the same access/closure/handling rules.

```sh
python3 tools/run_spatial_scene.py --pack-pocket-smoke work/preview/spatial-scene/pack-pocket.png
python3 tools/run_spatial_scene.py --pocket-smoke work/preview/spatial-scene/paperdoll-pocket.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/paperdoll-pocket-full.png
python3 -m unittest discover -s tests -p test_wiki_items.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```
