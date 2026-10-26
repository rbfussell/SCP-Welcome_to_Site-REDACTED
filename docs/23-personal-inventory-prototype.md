# Physical personal inventory prototype

Status: Bounded native implementation, October 25, 2026. This is the first personal equipment/nearby-container slice, not complete inventory/logistics/crafting or STO-01–10 acceptance.

The user subsequently revises navigation to [three fixed views, explicit securing and hex-body shape fitting](27-three-view-inventory-and-body-fit.md). That contract supersedes the expanding-column and Esc-as-Back design below, which still describes the current runtime. Closure/dismissal and non-60° orientation questions are pending before the replacement is wired.

## Playing it

Press **I** in local gameplay to open personal equipment. **I** or Close dismisses it. **Esc** returns up the active container branch, and dismisses the screen when no container views remain; during an uncommitted drag it dismisses and cancels the drag. The world **continues in real time**: opening, inspecting, selecting and previewing rotation do not introduce a pause. Space remains the explicit manual pause control; opening/closing inventory preserves it. The separately approved field-phone pause rule is unchanged.

The paperdoll equipment panel stays visible on the left. Only worn/held objects appear initially. Right-clicking a garment or bag opens its compartments to the right; a nested case opens beside its parent. Independently inspected worn containers can coexist for transfers, with scrolling confined to the storage area. Items use generated transparent object sprites with restrained grids. The Pacific Drive examples guide packing, rotation and compartment semantics, not card artwork or copied interface styling.

- Drag an object onto a compartment cell, or select it and click an empty cell. During a drag, the cursor's cell anchors the footprint's top-left; the object snaps to the exact size, position and rotation it will occupy. Destination cells and the outline are green for a valid physical fit and red for a blocked fit. Invalid releases leave the item at its source. Moving off the grid clears the highlight; closing the view cancels an uncommitted drag.
- **Layers** above the paperdoll switches between exposed equipment, all known worn equipment and a numbered layer. Covered garments can be identified but remain inaccessible for dragging or manipulation; their pockets/cases are not revealed. The filter is read-only and resets on reopening inventory.
- **R** or Rotate changes the placement preview. Dropping/placing starts actual handling; preview rotation is free, actual repacking is not.
- Double-click/right-click a visible container, or select it and choose Inspect, to see its compartments and breadcrumb. The utility case and six-piece gum packet demonstrate nested and differently scaled storage. A packed case requires inspection of its containing storage first. Use the view's × or Back/Esc to close it; closing a parent also closes its descendant views. This navigation does not physically fasten a lid. Reopening inventory starts with the paperdoll again.
- Open/Close changes a compartment through a timed action. A closed bag can still be carried; its live contents are hidden and cannot be retrieved through the lid. The backpack side pocket is independent of the main zipper.
- Select a coat and use **Open/Fasten selected garment** to expose or cover underlying equipment. This is separate from opening a pocket, and takes physical time/AP. The coat begins open in the test fixture. **Take selected in hand** and **Wear selected (default mount)** use the same validated manipulation service; drag to the front-pack mount for alternative mounting.
- Switch selected power operates a supported device. Cancel handling is available during real-time execution; elapsed time is not refunded. Closing the view lets already committed handling continue.
- Nearby supply crate opens the same existing `crate-a` fixture when within reach and with clear access. Its stock uses the same item graph and transfers. There is no remote grabbing, actor-to-actor editing or map-wide stock browser.

The expanded fixture has 15 definitions and 31 instances. It adds a worn shirt, socks and shoes for the director, plus a chemical coverall, spare socks and a spare backpack in the supply crate. The crate now has a 24 × 20 packing grid to hold those actual items. Chen and Ortiz retain their original loadouts under the same coverage/equipment rules. The paperdoll equipment column scrolls vertically to expose additional mount slots while remaining beside the inspected storage area.

This replaces the earlier always-visible pocket/pack columns. Choosing a compartment view is separate from opening a physical lid, reaching its contents and paying handling time/AP; those validated access rules still apply. Shutting an ancestor compartment dismisses its descendant views; moving a viewed container elsewhere or leaving fixture reach also dismisses its stale view. [Body equipment and container navigation](24-body-equipment-and-container-navigation.md) records the implemented bounded layer/coverage/fastening and front/back-pack rules, alongside future belt attachments and anatomical paperdoll artwork.

## Time and action points

Each compartment declares handling milliseconds and access AP, independently. Item handling contributes its own base cost. Retrieval/placement traverses the actual ancestor path and adds applicable source/destination costs. These are authored test values, not final balancing.

| Retrieving a flashlight into the hand | Time | AP in combat |
|---|---:|---:|
| Trouser pocket | 0.50s | 2 |
| Backpack side pocket | 0.75s | 3 |
| Backpack main section | 1.25s | 4 |

Nested cases add their own access cost. Opening a lid, changing power and moving clothing have their own computed costs; moving an item between two slow compartments includes both paths. Very expensive moves can exceed remaining AP and are rejected; multi-turn handling is not implemented.

A valid action begins handling and stops the character's walking route at its exact fractional position. The item stays at its source until the timed manipulation finishes. Closing the UI does not finish or cancel it. A successful final validation commits one placement/power/lid change; access/fit changes or startle can make the attempt fail after time is spent. Moving or changing attention can interrupt real-time work; other physical commands wait. Paid tactical handling reserves AP once, advances the ordinary local simulation clock and finishes before the next input wait. No free inventory transfers occur during local/manual holds or a phone pause.

One inventory manipulation may run at a time in this prototype. This serializes player handling and destination changes; it is not the later multi-worker reservation/hauling system. There is no carry animation or intermediate hand location during a short manipulation. Longer logistics jobs will use explicit pickup/carry/place work boundaries.

## Physical model and equipment access

Worn items claim body regions and layers while retaining one physical parent. Same-layer overlapping anatomy conflicts even across different mount slots. A higher layer blocks access to the regions it covers; authored fastening state can expose particular regions. The coat exposes the front when open, but its sleeves/back still prevent removing a shirt through it. Shoes cover socks. The sealed coverall blocks inner equipment until removed. Front and back backpacks use distinct compatible mounts and share the usual total load limit. A pack is outermost: fitting/removing a suit requires clearing each overlapping worn pack first, even when the eventual layered arrangement would be valid. No automatic reordering or free undress/redress occurs.

A root is a person's equipment or an existing fixture. Each item has one parent/region placement, stable ID, physical envelope in millimetres, mass, condition and power state. Regions have dimensions, grid scale, usable-cell mask, height/load limit, optional native category/mount restrictions and access metadata. Containers are items whose internal regions are separate from their external footprint.

The host validates overlapping footprints, rotation, thickness, region masks, native acceptance, recursive mass, total carrying load, cycles and an eight-item-level nesting bound. A 5mm-cell gum interior does not gain space because its view is drawn larger. The gum wrapper occupies two ordinary 25mm cells outside while six pieces occupy six fine cells inside. A flashlight cannot fit inside it.

Source and destination access check the complete container path, ownership and fixture reach/geometry. User-facing queries hide closed/unreachable contents. Exact known manifests, locks, seals, qualifications, exceptional/anomalous capacities, general item shapes and knowledge acquisition for unknown containers remain later work.

A separate equipment-ready property allows pockets/hand mounts to supply quick capabilities while the backpack main section requires retrieval. The phone interface needs the actual carried, powered, undamaged accessible handset as well as its service enrollment/link fixture. Personnel radio delivery likewise requires their actual ready radio. Packing or closing over equipment changes those existing services; viewing an inventory does not conjure a second phone or radio. Compatibility power/issuance switches in the earlier operation fixture remain additional service cut-outs, not another physical item. Battery depletion, electrical networks and radio propagation are still unimplemented.

## Boundaries and persistence

| Component | Owns |
|---|---|
| [Items](../src/SiteSimulation/Inventory/Items/InventoryDefinitions.cs) | Data definitions, placements, timed requests and snapshots |
| [Hierarchy](../src/SiteSimulation/Inventory/Hierarchy/InventoryHierarchy.cs) | Physical root, recursive mass and breadcrumbs |
| [Placement](../src/SiteSimulation/Inventory/Placement/InventoryPlacement.cs) | Envelope-to-cell mapping, masks, overlap, native fit and loads |
| [Access](../src/SiteSimulation/Inventory/Access/InventoryAccessService.cs) | Ownership, ancestor lids, worn coverage, fixture reach and equipment readiness |
| [Equipment](../src/SiteSimulation/Inventory/Equipment/InventoryWear.cs) | Anatomical layer claims, exposed regions, outermost packs and dressing dependencies |
| [Transactions](../src/SiteSimulation/Inventory/Transactions/InventoryActions.cs) | Pure staged quotes, timed work, interruption and final atomic commit |
| [Queries](../src/SiteSimulation/Inventory/InventoryQueries.cs) | Player-bounded roots/contents, cost projection and validated commands |
| [Presentation](../game/Presentation/Inventory) | Object art, compartment grids, selection, drag/drop and view lifecycle |
| [Container browser](../game/Presentation/Inventory/InventoryBrowser.cs) | Explicit inspection history, parent/child views, Back and access/move invalidation |
| [Container view](../game/Presentation/Inventory/InventoryContainerView.cs) | One inspected object's regions, breadcrumbs and physical lid controls |

Both current scenarios explicitly select the native `field-kit-v1` profile. Its [JSON content](../src/SiteSimulation/Inventory/Content/field-kit.json) is an embedded resource available without Godot or sibling UI scenes. The profile registry is a bounded content fixture, not the public mod loader. Content is included in the save identity; changing dimensions/costs/loadouts invalidates an old matching-content claim. Item art is separate and does not enter gameplay identity.

Outer schema **8** retains items, placements, compartment states, revision and pending handling, and persists each garment’s open/fastened state. Authored coverage, layer and mount profiles remain part of content identity alongside existing employment, orders, journal and tactical state. Restore validates the entire detached graph and checks inventory/tactical schedules agree before replacing the running world. Matching scenarios without inventory retain the earlier supported migration paths; current profiles require their real inventory snapshot. The changed scene/catalog identity requires fresh saves. SCP-650 module schema remains 2.

Warmth/protection effects, layered clothing on actor sprites, garment folding, consumption, stacking/splitting, materials, composition, crafting, injuries/wear, tools/treatments/lighting from inventory items, weapon/ammunition integration, two hands, other specialized fixture layouts and automated logistics remain future work. The existing nonlethal combat sidearm is still a training fixture, not an inventory-backed weapon. This slice establishes the reusable membership/access/handling foundation for those features.

## Verification

```bash
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/inventory-first.png
python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/inventory-operation.png
python3 tools/run_spatial_scene.py --oscp-smoke work/preview/spatial-scene/inventory-oscp.png
```

[Core checks](../tests/SpatialChecks/InventoryChecks.cs) cover physical fit/rotation/masks, recursive and total mass, nested access, real equipment service availability, distinct AP/time costs, no premature transfer, cancellation/holds, paid handling, fixtures, deterministic continuation and atomic invalid restores. Godot additionally exercises actual inventory input/rendering, real-time inspection, packing, nested object views, closing during work, save equivalence and combat AP. [Object sources and prompts](../game/Assets/inventory-objects/README.md) preserve the built-in image generation provenance and registration.
