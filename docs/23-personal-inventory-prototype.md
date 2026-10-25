# Physical personal inventory prototype

Status: Bounded native implementation, October 25, 2026. This is the first personal equipment/nearby-container slice, not complete inventory/logistics/crafting or STO-01–10 acceptance.

## Playing it

Press **I** in local gameplay to open personal equipment. **I**, **Esc** or Close dismisses it. The world **continues in real time**: opening, inspecting, selecting and previewing rotation do not introduce a pause. Space remains the explicit manual pause control; opening/closing inventory preserves it. The separately approved field-phone pause rule is unchanged.

The view shows worn/held objects, separate trouser pockets, coat pocket, backpack main and side compartments, and an inspectable nested container. Items use generated transparent object sprites with restrained grids. The Pacific Drive examples guide packing, rotation and compartment semantics, not card artwork or copied interface styling.

- Drag an object onto a compartment cell, or select it and click an empty cell. During a drag, the cursor's cell anchors the footprint's top-left; the object snaps to the exact size, position and rotation it will occupy. Destination cells and the outline are green for a valid physical fit and red for a blocked fit. Invalid releases leave the item at its source. Moving off the grid clears the highlight; closing the view cancels an uncommitted drag.
- **R** or Rotate changes the placement preview. Dropping/placing starts actual handling; preview rotation is free, actual repacking is not.
- Double-click/right-click a container, or select it and choose Inspect, to see its compartments and breadcrumb. The utility case and six-piece gum packet demonstrate nested and differently scaled storage.
- Open/Close changes a compartment through a timed action. A closed bag can still be carried; its live contents are hidden and cannot be retrieved through the lid. The backpack side pocket is independent of the main zipper.
- Switch selected power operates a supported device. Cancel handling is available during real-time execution; elapsed time is not refunded. Closing the view lets already committed handling continue.
- Nearby supply crate opens the same existing `crate-a` fixture when within reach and with clear access. Its stock uses the same item graph and transfers. There is no remote grabbing, actor-to-actor editing or map-wide stock browser.

The first fixture has 11 definitions and 25 instances: trousers, coat, backpack, handset, radio, flashlight, dressing, utility case, gum wrapper, six independent gum pieces and an inert training weight. Chen and Ortiz use the same inventory/equipment semantics. The supplied equipment fills only legs, outerwear, back and hand mount types; the other named garment mounts have no issued content yet.

Confirmed later presentation direction: right-clicking a jacket, backpack or other container will display that object's compartments in the inventory panel. All pockets and bags should not be displayed at once; the player must deliberately look inside the chosen storage. The current always-visible pocket/pack columns remain an engineering layout for now. Choosing a compartment view is separate from opening a physical lid, reaching its contents and paying handling time/AP; those validated access rules still apply.

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
| [Access](../src/SiteSimulation/Inventory/Access/InventoryAccessService.cs) | Ownership, ancestor lids, fixture reach and equipment readiness |
| [Transactions](../src/SiteSimulation/Inventory/Transactions/InventoryActions.cs) | Pure staged quotes, timed work, interruption and final atomic commit |
| [Queries](../src/SiteSimulation/Inventory/InventoryQueries.cs) | Player-bounded roots/contents, cost projection and validated commands |
| [Presentation](../game/Presentation/Inventory) | Object art, compartment grids, selection, drag/drop and view lifecycle |

Both current scenarios explicitly select the native `field-kit-v1` profile. Its [JSON content](../src/SiteSimulation/Inventory/Content/field-kit.json) is an embedded resource available without Godot or sibling UI scenes. The profile registry is a bounded content fixture, not the public mod loader. Content is included in the save identity; changing dimensions/costs/loadouts invalidates an old matching-content claim. Item art is separate and does not enter gameplay identity.

Outer schema **7** adds items, placements, compartment states, revision and pending handling, alongside existing employment, orders, journal and tactical state. Restore validates the entire detached graph and checks inventory/tactical schedules agree before replacing the running world. Matching scenarios without inventory retain the earlier supported migration paths; current profiles require their real inventory snapshot. The changed scene/catalog identity requires fresh saves. SCP-650 module schema remains 2.

Warmth/protection effects, layered clothing on actor sprites, garment folding, consumption, stacking/splitting, materials, composition, crafting, injuries/wear, tools/treatments/lighting from inventory items, weapon/ammunition integration, two hands, other specialized fixture layouts and automated logistics remain future work. The existing nonlethal combat sidearm is still a training fixture, not an inventory-backed weapon. This slice establishes the reusable membership/access/handling foundation for those features.

## Verification

```bash
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/inventory-first.png
python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/inventory-operation.png
python3 tools/run_spatial_scene.py --oscp-smoke work/preview/spatial-scene/inventory-oscp.png
```

[Core checks](../tests/SpatialChecks/InventoryChecks.cs) cover physical fit/rotation/masks, recursive and total mass, nested access, real equipment service availability, distinct AP/time costs, no premature transfer, cancellation/holds, paid handling, fixtures, deterministic continuation and atomic invalid restores. Godot additionally exercises actual inventory input/rendering, real-time inspection, packing, nested object views, closing during work, save equivalence and combat AP. [Object sources and prompts](../game/Assets/inventory-objects/README.md) preserve the built-in image generation provenance and registration.
