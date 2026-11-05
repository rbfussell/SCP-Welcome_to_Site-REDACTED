# Current-tile ground inventory and physical piles

Implemented November 4, 2026. The inventory has a permanent **Ground / at your feet** dock, independent of the [three inspection panes](27-three-view-inventory-and-body-fit.md) and their [source/destination trails](48-independent-inventory-transfers.md). This extends [physical storage](05-storage-and-logistics.md), [personal inventory](23-personal-inventory-prototype.md), [body equipment](24-body-equipment-and-container-navigation.md) and [item hierarchy](16-item-composition-and-crafting.md). See the [wiki mechanic](../source_material/game-wiki/ground-storage.html).

## Interaction

The dock shows a square packing grid for the physical navigation hex beneath the player. It is visible in equipment, body-fit and sprite modes, even when neither gear nor contents is open. Moving to another hex changes the dock's world location; it never carries a pile with the player. The anchor uses `Hex.FromWorld(actor.Position)`, including fractional movement/stops, rather than the actor's last completed step.

Drag worn or accessible stored equipment to an exact floor cell, or use **Put down → ground** / **Put selected down**. The explicit shortcut prefers the first free placement, then the top of a pile at the origin; it does not rotate items to force a fit or rearrange anything. Grid drags retain the requested cells and horizontal/vertical rotation. Dragging onto an occupied floor footprint means stacking on the ground, not automatically inserting into that object's interior. Inspect the exposed bag/case and use its normal gear/contents targets to put something inside it.

Pick floor items up through the same hand, body or storage destinations used elsewhere. Double-click an accessible floor container to inspect its gear illustration; its compartments open individually through normal paid handling. Bags, pockets, attached providers, condition and contents retain their original identities. A dropped phone is no longer carried equipment. Inventory inspection does not pause time or waive normal handling/AP costs.

## Stack access

Different item footprints can overlap on the floor. Each direct floor item has a positive, unique `ItemPlacement.GroundOrder`; new placements go above existing items. This is relative ordering, not an absolute vertical height, count-stack merge or clothing layer. Gaps in the numbers do not create floating items. The grid draws low to high, picks the top footprint at the pointer and badges exposed top items with their overlapping-item count.

**Move covering items first.** Any higher overlapping footprint blocks manipulation/inspection of the lower item, including access to that item's compartments and nested contents. Merely exposed pixels do not let a player pull a larger buried item through the pile. Removing or moving a top item reveals the next accessible item. No rummaging shortcut, hidden rehandling or implicit unstacking occurs. This applies to any ordinary item; it is not a clothing-specific exception.

Floor piles are positioned manually. Arrange/AutoPlace do not organize a floor patch, and dropping onto the floor never repacks its neighbors. Normal container-target packing still works when the destination is an actual inspected compartment. Pile access follows every ancestor, so AutoPlace cannot extract a child from a buried bag.

## World location, limits and saves

[InventoryGround](../src/SiteSimulation/Inventory/Placement/InventoryGround.cs) resolves canonical map-local `ground:q:r` roots lazily from the current map's cell set. Empty tiles create no saved records or inventory revisions. Ground roots have neither an actor owner nor a prop binding; the actor must stand on that exact physical hex to access them. Existing identity, single-parent, cycle, mass and descendant rules still apply. The current patch is **10×10 cells at 50 mm**, with a **100 kg** total load limit and **1000 mm** cumulative envelope thickness per occupied cell. These are bounded prototype defaults in the ground service, not a final floor-strength/volume model or a new public authoring format.

Ground order is only valid on a direct floor placement; equipping, attaching or packing clears it. [InventoryPlacement](../src/SiteSimulation/Inventory/Placement/InventoryPlacement.cs) resolves ground order and ordinary dressing order together. Paid moves save that exact result and retain the source until commitment. Movement, access loss, a changed pile/revision or cancellation uses existing interruption/revalidation rules.

Current snapshots use **schema 15**. Matching schema-14 inventory saves migrate with no ground placements; native kit/map identity is unchanged. Older inventory versions remain rejected. Downgrading a pile or paid ground request to schema 14 is rejected. Unknown/noncanonical/out-of-map roots, invalid or duplicated pile orders, invalid coordinates, pile height/load overflow and forged paid orders fail detached restore without replacing the live world. Root locations are reconstructed from the matching map; item placements and pile orders are saved normally.

## Verification and boundaries

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --ground-smoke work/preview/spatial-scene/ground.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/ground-full.png
```

[Domain checks](../tests/SpatialChecks/GroundChecks.cs) qualify persistent floor locations, read-only queries, overlapping order, top-only access through nested ancestors, clothes staging and mass conservation, exact rotation/repositioning, load/height limits, pause, tactical AP, interruption, canonical pending continuation, schema-14 migration and atomic invalid restore. [Native input checks](../game/Presentation/SpatialScene.GroundSmoke.cs) exercise the dock, drag stacking/picking, occupied-hand undressing/redressing, buried bags, ordinary container views, sprite mode, tile changes and interface reopening. Actual results belong in [runtime evidence](reviews/inventory-runtime-checks.md).

This is a single-map inventory feature. It does not add world floor-item sprites/collision, spills, autonomous hauling, count-stack merging, arbitrary 3D support/stability or cross-domain location transfer. Logical pile order plus authored rectangular thickness envelopes approximate floor stacking; they are not a physics simulation. Source item art and the 19 definitions/39 instances/9 body-fit profiles are unchanged.

## Future interface direction

The user proposes expanding an inspected object's inventory as a floating internal window within its parent's view. This is a future production-UI consideration, not a debug-mode implementation or a finalized replacement for the current three-pane browser. The goal is intuitive access to nested objects with less navigation friction. Placement, access, pile obstruction, closure state, time/AP and identity must remain simulation services so another window layout can reuse them. The permanent floor dock is independent of those inspection trails.

Current November 4 follow-up: [worn attachment chains](50-worn-attachment-chains.md) qualify the normal front belt → hook → pouch art, shared composed transforms, distinct alpha targets and loaded ground handling. Earlier direct-only/nested-art limits above describe the prior slice. Other strap poses and world clothing remain unqualified; current saves use schema 15.
