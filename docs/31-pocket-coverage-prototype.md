# Pocket access from body-surface overlap

Current follow-up: [body-fit cleanup](38-body-fit-cleanup.md) regenerates paired mouths with the symmetric body and projects adjustable outer straps using their resolved span. Access behavior below remains; new immutable geometry requires a fresh field-kit scene.

Status: October 26, 2026. The native inventory now uses authored body-local overlap to decide access to mapped compartments. The first fixture is a zipped jacket over hip pockets, with exposed lower cargo pockets. This extends the [body-fit slice](30-body-fit-prototype.md) and [gear metadata contract](29-gear-data-standard.md).

## Try it

Press **I**. The utility trousers now have four real compartments: left/right hip pockets at the top and left/right cargo pouches lower on the legs. Double-click the trousers to inspect their illustration; hover or double-click the actual pouch/opening.

Select the coat and use its normal **Zip selected garment** control. While zipper work is still resolving, access stays as it was. Once closed:

- Hip pockets highlight red on hover and explain that the field coat covers them. Their contents and nested contents cannot be accessed.
- Lower cargo pockets remain reachable. They can be opened, secured, loaded and emptied through the normal handling service.
- A cargo contents view already open stays open. A hip contents view closes when its access becomes blocked, retaining the accessible trouser gear view.

Unzip the coat through its paid action to expose the front hip openings again. This changes coverage, not the pockets' own flap/button states or securing preferences. The jacket's back and retained front/sleeve cover do not disappear merely because its front opens.

In **Body fit · hex view**, select the worn trousers to see current mouth cells. Red marks identify the actually obstructed cells; cyan marks are clear. The summary counts covered versus clear pockets. Any required mouth-cell obstruction denies that pocket's access, even if another part of the mouth remains clear. These marks describe actual worn placement; moving an uncommitted fit draft does not alter them or reveal contents.

## Native authoring fields

| Field | Meaning |
|---|---|
| `StorageRegion.AccessFootprint` | One or more surface masks locating this compartment's required access cells on its owner's worn shape. Each mask has a `surface` and local axial `cells`. |
| `BodyFitShape.Coverage.Closed` | Cover masks active while the garment is fastened. |
| `BodyFitShape.Coverage.Open` | Cover masks active while the garment is open. This fixture permits authored subsets of the closed masks. An empty array explicitly clears all cover. |
| `WearMount.BodySurface` | Resolves a `mounted` mask to `front` or `back`, such as one backpack being worn on either side. |
| `WearProfile.CoversAccess` | Whether this outer item can obstruct access. A mapped cover does not override a false flag. |
| `WearProfile.BlocksRemoval` | Independent removal restraint; opening or clearing a pocket does not remove this dependency. |

Cells are local to the owning shape's default anchor. Transform both the mouth and every relevant outer cover by their own actual `BodyPose`, including their sixth-turn rotation. Only cells on the same body surface can intersect. Front/back masks are distinct even where their two-dimensional coordinates coincide. The shared shape geometry is not tied to map hex scale, inventory square-cell scale or image pixels.

Each illustrated hotspot still targets its real compartment ID. The hip seams now bind `left` and `right`; the lower pouch contours bind `cargo-left` and `cargo-right`. Their [source-image polygons](../game/Assets/inventory-objects/hotspots.json) control pointer picking/highlighting, while the physical access masks live in [item metadata](../src/SiteSimulation/Inventory/Content/field-kit.json). The [body/garment catalog](../src/SiteSimulation/Inventory/Content/body-fit.json) supplies cover masks. Image opacity alone decides neither physical coverage nor storage capacity.

Validation rejects unknown surfaces, repeated mask surfaces/cells, masks outside the owner's worn footprint, empty pocket footprints, malformed open-cover relationships and `mounted` masks whose wear mounts do not identify a surface. Native masks currently support `front`, `back` and mount-resolved `mounted`. More surface kinds, open panels moving into new cover positions and linked seams require an explicit extension rather than a guessed coordinate mapping.

## One access rule shared by every consumer

[InventoryCoverage](../src/SiteSimulation/Inventory/Equipment/BodyFit/InventoryCoverage.cs) projects masks and reports pocket mouths, exact blocked cells, actual blocker identities and whether a coarse compatibility fallback was needed. Its player-facing diagnostic query is restricted to the player's own worn items and returns no contents.

[InventoryWear.Compartment](../src/SiteSimulation/Inventory/Equipment/InventoryWear.cs) uses this result inside the existing ancestor access traversal. Therefore the same geometry gates:

- Opening/securing a pocket and reading its contents.
- Manual transfers and deliberate container-target packing/Arrange.
- Nested cases/packets inside the pocket.
- Readiness of actual phones/radios and other equipment services.
- Automatic securing and final revalidation of paid handling.

Only worn gear on the same person, at a strictly higher layer, with `CoversAccess=true` participates. The item never covers its own compartment through this outer-layer test. The blocked set combines all matching outer layers in deterministic layer/ID order.

A mapped pocket uses exact intersection against mapped outer covers. It does not reapply coarse whole-region blocking after a geometric non-overlap. An outer item without mapped cover geometry conservatively retains its existing `AccessBody`/coverage behavior; that fallback is identified without inventing blocked hexes. A pocket without an access footprint continues to use the legacy coarse rule. This allows incremental content conversion while avoiding silent access through unmapped equipment.

A back-worn pack does not cover a front chest pocket. The same pack moved to a front mount can cover that pocket where its authored mask overlaps. An item held in the hand or packed away does not project a worn cover.

Paid handling retains the source until completion and checks current access again before commitment. An unexpected cover change can block an already started transfer without duplicating/moving the item. Geometry queries, hover updates and body markers are read-only and consume no simulation RNG.

## Content and persistence

The native catalog remains **15 portable definitions and 31 instances**. Three existing trouser instances each gain two cargo compartments; region-state entries increase from 19 to 25. Compartments remain regions of those items, not extra portable objects. Existing hip contents keep their identities and original `left`/`right` parent IDs. Cargo capacities/access costs are bounded fixture values, not finalized balance.

Outer schema **12** already persists the required garment state, compartment state and body pose, so no new mutable save field is needed. The additional compartments and static geometry alter content identity: use fresh saves for the changed field-kit fixture. Matching saves restore all actual closure/placement state and recompute the same overlap. SCP-650 module schema remains 2.

## Bounds and following slices

This qualifies geometric compartment access. Whole-garment visibility, layer conflicts and removal restraints still use the existing coarse regions; opening a pocket does not certify arbitrary geometric undressing. The single front-projected diagnostic body is not a finished front/back clothing compositor. Shape symmetry, final cloth/fastening animation and six-direction world outfit art remain separate work.

The following [belt/pouch slice](32-belt-and-attachment-prototype.md) now qualifies one exterior support, compatibility and loaded parent movement. Drag-and-drop fitting remains the intended next body-control interaction; it must reuse the same fit/coverage/handling services rather than applying free placement on release. Spillage remains a separate inventory behavior.

## Verification

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/inventory-coverage.png
```

[Coverage domain checks](../tests/SpatialChecks/CoverageChecks.cs) cover partial mouth intersection, hip/cargo distinction, independent surfaces and rotation, open-front/back retention, nested/service access, paid closures, continuation, post-start cover changes, unmapped compatibility, front-mounted packs, independent removal flags and invalid author metadata. Existing inventory/body/closure tests remain required.

[Native coverage checks](../game/Presentation/SpatialScene.CoverageSmoke.cs) use all four illustrated segments and actual coat controls, validate blocked hip clicks and retained cargo views, inspect body marks, and restore the resulting state. Preview screenshots include covered hip, usable cargo, exact overlap and exposed hip states. They are ignored inspection output, not new production art.

## Hip-pocket fastening correction

These trousers’ `left`/`right` hip openings are **unsealable and snug**: `Sealable=false`, `Snug=true`, `SecureOnClose=false`. Inspecting an exposed opening reveals its grid without fastening work; jackets can still cover the mouth. Menus offer no Button/Unbutton or securing preference, and dismissing the view queues no hip closure. Cargo pockets retain their separate buttoned flaps. The snug flag declares retention for future spillage; this change does not implement the spillage scheduler. Invalid saved sealed states or closure overrides on an unsealable opening reject atomically. Schema 13 is unchanged; static field-kit content identity changes.
