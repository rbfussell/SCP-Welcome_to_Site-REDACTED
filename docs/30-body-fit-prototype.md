# Body fitting: first playable slice

Status: October 26, 2026. The first body-local hex mask/contact fixture runs in the native simulation and Godot inventory. It is an engineering fit view alongside the existing sprite equipment view, not finished anatomical artwork or a complete replacement for named mounts.

## Try it

1. Press **I**, then **Body fit · hex view** in the existing left paperdoll panel.
2. Choose worn or held clothing from the dropdown. Retrieve stored clothing normally before fitting it. Selecting a worn/held item before switching carries that choice into the fit view.
3. Click a body hex to position the item's authored anchor. **R** or **Rotate 60°** cycles six body orientations. **Default** restores its authored fit.
4. Inspect the result. Green cells are on the body. Red cells fall outside it. White boundary edges meet required body contacts; red edges fail them. The gold marker is the placement anchor.
5. **Fit / equip** submits the existing timed equipment command if geometry, access, wearing order and destination all permit it. To try a complete handling cycle, take the shoes into hand through the equipment view, then fit them back at Default.

The reason text distinguishes shape failure from access/removal constraints. A geometrically valid shirt cannot bypass its outer coat. An already worn item at its effective default reports that it is already there; omitting an explicit default pose is equivalent to storing it. Invalid previews are read-only and disable Apply. Manual pause still denies physical work even when a draft is valid. Inspection, rotation and anchor movement neither pause time nor spend AP.

**Equipment slots** returns the left panel to the existing sprites, Layers selector and dressing controls. The middle gear and right contents trails retain their roles; this is a mode of the first view, not a fourth inventory window. Esc still closes the interface without altering physical closures. R in ordinary storage continues to mean two orthogonal orientations; R in body mode operates only the body draft.

## Geometry and content

[body-fit.json](../src/SiteSimulation/Inventory/Content/body-fit.json) defines one provisional human silhouette with nine shoulder cells and seven worn profiles: shirt, coat, trousers, backpack, socks, boots and chemical coverall. These remain diagnostic shapes to refine with real body/garment art. All existing employees use the same body family for this fixture; phenotype/body-size variation needs additional authored families.

`BodyMask.Rows` and `BodyFitShape.Rows` use a shared odd-row-offset canvas: `#` occupies a cell and `.` leaves it empty. They are deliberately readable as silhouettes in the JSON file. Rows convert to body-local axial coordinates with `q = column - floor(width/2) - floor(row/2)`, `r = row`. These units have no connection to map hex size, navigation coordinates, sprite pixels or pocket millimetres.

Each shape has an authored default anchor and local contact edges. Contacts use the shape's coordinates relative to that anchor. Unlisted boundary edges are explicitly open under this fixture's convention. Neck, sleeve and hem openings can permit the body to continue beyond the garment boundary; a contact edge cannot. Contact registration comes from authored geometry, not inferred transparency in a sprite.

The evaluator:

- Rotates occupied cells and contact directions by the same zero-to-five sixth-turn value, then translates them to the candidate anchor.
- Requires every occupied cell to lie on the target body mask.
- Requires each contact's cell to lie on the body while its outward neighbor lies outside the body. A closed shoulder edge moved into the torso therefore fails even if the entire garment footprint remains inside the body.
- Checks the shape's body family and permitted orientations. Large/out-of-range saved coordinates and unsupported rotations fail before transforming cells.

The native catalog validates row syntax/bounds, unique IDs, matching canvas/family, orientation declarations, contact uniqueness/directions, boundary ownership and a valid authored default. A contact cannot point through another occupied garment cell. The shape source is included in inventory content identity so changing a silhouette/contact cannot silently reinterpret an old save.

## Integration and source boundaries

| Component | Responsibility |
|---|---|
| [BodyFitDefinitions](../src/SiteSimulation/Inventory/Equipment/BodyFit/BodyFitDefinitions.cs) | Body-local coordinates, shapes, poses and diagnostic result records |
| [BodyFitGeometry](../src/SiteSimulation/Inventory/Equipment/BodyFit/BodyFitGeometry.cs) | Pure shape transforms and boundary tests; no item mutation or costs |
| [BodyFitCatalog](../src/SiteSimulation/Inventory/Equipment/BodyFit/BodyFitCatalog.cs) | Embedded, validated authored body/garment fixture |
| [InventoryBodyFit](../src/SiteSimulation/Inventory/Equipment/BodyFit/InventoryBodyFit.cs) | Worn-placement validation and player-bounded preview adapter |
| [InventoryPlacement](../src/SiteSimulation/Inventory/Placement/InventoryPlacement.cs) | Combines geometry with existing packing, category, hierarchy, layer and load validation |
| [InventoryBodyFitPanel](../game/Presentation/Inventory/InventoryBodyFitPanel.cs) | Draft selection, shared screen picking/drawing transform and normal Apply command |

`InventoryRoot.BodyFamily` chooses the body. `WearProfile.FitProfile` selects the garment shape. `ItemPlacement.BodyPose` stores optional body-local Q/R and rotation; null means that profile's authored default. A body pose is valid only for a fitted wearable at an actor's worn mount. It is rejected in square storage or the hand. Moving the item off the body clears that optional pose through the ordinary destination placement.

The source item remains at its real position while fitting takes time. Completion revalidates the entire candidate graph and commits once; taking off or refitting still requires clearing covers/removal blockers. A front-worn backpack keeps its front mount in the preview rather than switching to the back. Picking a geometric position does not imply a new physical parent, expose pocket contents or let the player manipulate another employee's equipment. The selector is restricted to the actor's worn/held items; being near an accessible crate or leaving a bag open does not create a quick-scan list of stored clothing. Known covered own clothing can be inspected as shape data, but the usual action restrictions remain.

Outer save schema **12** retains explicit worn coordinates and pending equipment destinations. Save/restore uses matching body/item content and the existing detached graph validation. Earlier matching spatial-only migrations remain supported; the newly authored field-kit body metadata changes content identity and requires fresh saves for previous inventory builds. SCP-650 module schema stays 2. Runtime body definitions are a bounded native fixture; the [proposed general gear schema](29-gear-data-standard.md) is still a separate authoring contract.

## What this slice does and what follows

This slice qualifies mask/contact fitting, six orientations, inspection feedback and paid placement. Named equipment mounts still restrict eligible locations and occupancy. Existing coarse regions still decide layer overlap and removal obstruction. The subsequent [pocket coverage slice](31-pocket-coverage-prototype.md) now uses geometric masks for mapped compartment access, with a conservative fallback for unmapped gear. A front-projected silhouette alone does not qualify full front/back/seam geometry, clearance, stretch or body proportions.

Next bounded slices:

1. **Geometric coverage:** the [compartment access proof](31-pocket-coverage-prototype.md) now qualifies hip/cargo mouths and open/closed covers. General geometric removal and visibility remain later.
2. **Attachment support:** one belt/strap provider and one child attachment, including rotation of support/contact geometry, overlap, clearance and load. Multiple angled belts require loosening legacy mount occupancy through an explicit equipment-graph change.
3. **Appearance alignment:** map fitted gear/front-back surfaces to the doll and world pose bindings. Reuse the fit coordinates without allowing sprite frames to change physical state. Generated or composited final art is a separate proof.

Spillage remains the next inventory behavior after the selected body/attachment slice. No cloth simulation, arbitrary item auto-rigging, ammunition behavior or revised combat system is introduced here.

## Verification

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/inventory-body-fit.png
```

[Domain checks](../tests/SpatialChecks/BodyFitChecks.cs) cover authored defaults, shoulder count, independent rotation vectors, rotating contacts, contact-vs-outside failures, open edges, forbidden orientations/families, body/storage separation, hidden-equipment boundaries, normal paid work/AP, pause, no-op defaults, front packs, exact continuation and forged-save/catalog rejection. [Native checks](../game/Presentation/SpatialScene.BodyFitSmoke.cs) use actual item selection, mode button, body-cell click, R, Default and Apply; the existing packing/closure/navigation tests remain in the same run. Valid, invalid and applied-body screenshots are retained under ignored preview output for visual inspection.
