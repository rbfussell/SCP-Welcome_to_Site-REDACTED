# Physical personal inventory prototype

Status: Bounded native implementation, October 25, 2026. This is the first personal equipment/nearby-container slice, not complete inventory/logistics/crafting or STO-01–10 acceptance.

The user subsequently revises navigation to [three fixed views, explicit securing and hex-body shape fitting](27-three-view-inventory-and-body-fit.md). That contract supersedes expanding-column navigation. The runtime implements that three-view navigation and paid automatic securing. Esc remains interface-only. The [first body-mask/contact fit view](30-body-fit-prototype.md) now adds geometric validation and paid fitting alongside legacy mounts; the subsequent [pocket coverage slice](31-pocket-coverage-prototype.md) now gates mapped pockets by actual overlap; animated clothing remains future work.

## Playing it

Press **I** in local gameplay to open personal equipment. **I**, **Esc** or Close dismisses the interface. Esc never performs physical container closure; open containers stay open and already committed work can continue. Dismissing an uncommitted drag cancels its preview. The world **continues in real time**: opening, inspecting, selecting and previewing rotation do not introduce a pause. Space remains the explicit manual pause control; opening/closing inventory preserves it. The separately approved field-phone pause rule is unchanged.

The paperdoll equipment panel stays visible on the left. Only worn/held objects appear initially. Double-clicking a garment or bag shows its enlarged illustration in the middle, with pouch-shaped hover regions; opening one compartment shows its contents on the right. Nested contents replace that right view and retain an Up trail. The [independent-transfer follow-up](48-independent-inventory-transfers.md) supersedes the earlier switch-clears-contents behavior: another top-level object replaces only the middle trail, retaining an accessible source on the right. Items use generated transparent object sprites with restrained grids. The Pacific Drive examples guide packing, rotation and compartment semantics, not card artwork or copied interface styling.

- Drag an object onto a compartment cell, or select it and click an empty cell. During a drag, the cursor's cell anchors the footprint's top-left; the object snaps to the exact size, position and rotation it will occupy. Destination cells and the outline are green for a valid physical fit and red for a blocked fit. Invalid releases leave the item at its source. Moving off the grid clears the highlight; closing the view cancels an uncommitted drag.
- **Layers** above the paperdoll switches between exposed equipment, all known worn equipment and a numbered layer. Covered garments can be identified but remain inaccessible for dragging or manipulation; their pockets/cases are not revealed. The filter is read-only and resets on reopening inventory.
- **R** or Rotate changes the placement preview. Dropping/placing starts actual handling; preview rotation is free, actual repacking is not.
- Double-click a visible object/illustrated hotspot to Open; right-click for inspection, its state-derived Zip/Unzip or other closure action, its own Secure on view close preference, and Remove → hand. The utility case and six-piece packet demonstrate nested and differently scaled storage. A packed case requires its containing compartment to be inspected first. ↑ returns one step; × closes the whole corresponding trail, including dependent contents when closing gear. Both consult each fastening’s authored default or player override and prepare eligible securing work, with children before parents. Jacket fronts default to staying open; pocket flaps and bag compartments can auto-secure independently. Esc/I/Close hide the interface without securing. Reopening starts with the paperdoll again.
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

One inventory manipulation may run at a time in this prototype. A saved securing queue can purchase successive closures through that same service; it waits for sufficient AP and the player’s turn without automatically ending the turn. Failed access, changed placement or interruption stops remaining work. Busy handling prevents another close sequence until finished/canceled. This serializes player handling and destination changes; it is not the later multi-worker reservation/hauling system. There is no carry animation or intermediate hand location during a short manipulation. Longer logistics jobs will use explicit pickup/carry/place work boundaries.

## Physical model and equipment access

Worn items claim body regions and layers while retaining one physical parent. Same-layer overlapping anatomy conflicts even across different mount slots unless both profiles explicitly permit overlap through matching tags. A higher layer with `CoversAccess` blocks access to covered regions; authored fastening state can expose particular regions. `BlocksRemoval` independently requires removing the blocker before changing underlying clothing, even after opening. Both default to true in existing profiles. [Wearable metadata](28-wearable-metadata-and-surface-mapping.md) records the distinction and the future surface/attachment refinements. The coat exposes the front when open, but its sleeves/back still prevent removing a shirt through it. Shoes cover socks. The sealed coverall blocks inner equipment until removed. Front and back backpacks use distinct compatible mounts and share the usual total load limit. A pack is outermost: fitting/removing a suit requires clearing each overlapping worn pack first, even when the eventual layered arrangement would be valid. No automatic reordering or free undress/redress occurs.

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
| [Container browser](../game/Presentation/Inventory/InventoryBrowser.cs) | Separate bounded gear/contents trails, Up/×, ownership and stale-view invalidation |
| [Container view](../game/Presentation/Inventory/InventoryContainerView.cs) | Fixed middle/right views, illustrations, current contents grid and breadcrumbs |
| [Closure policy](../src/SiteSimulation/Inventory/Transactions/InventoryClosures.cs) | State-derived action labels and per-fastening defaults/overrides |
| [Securing](../src/SiteSimulation/Inventory/Transactions/InventorySecuring.cs) | Persisted close intent; serial child-first paid actions and interruption |
| [Gear surface](../game/Presentation/Inventory/InventoryGearSurface.cs) | Source-image segment contours; hover/picking bound to real compartments/fastening |

Both current scenarios explicitly select the native `field-kit-v1` profile. Its [JSON content](../src/SiteSimulation/Inventory/Content/field-kit.json) is an embedded resource available without Godot or sibling UI scenes. The profile registry is a bounded content fixture, not the public mod loader. Content is included in the save identity; changing dimensions/costs/loadouts invalidates an old matching-content claim. Item art is separate and does not enter gameplay identity.

Outer schema **12** retains items, placements, compartment states, revision, pending handling, each garment’s open/fastened state, per-instance/compartment securing overrides, remaining securing intent and exact pending packing plans, plus optional worn body coordinates and pending fit destinations. Matching schema-8 inventory snapshots migrate with an empty close queue, and matching older snapshots without overrides inherit authored fastening defaults. Authored coverage, layer and mount profiles remain part of content identity alongside existing employment, orders, journal and tactical state. Restore validates the entire detached graph and checks inventory/tactical schedules agree before replacing the running world. Matching scenarios without inventory retain the earlier supported migration paths; current profiles require their real inventory snapshot. The changed scene/catalog identity requires fresh saves. SCP-650 module schema remains 2.

Warmth/protection effects, layered clothing on actor sprites, garment folding, consumption, stacking/splitting, materials, composition, crafting, injuries/wear, tools/treatments/lighting from inventory items, weapon/ammunition integration, two hands, other specialized fixture layouts and automated logistics remain future work. The existing nonlethal combat sidearm is still a training fixture, not an inventory-backed weapon. This slice establishes the reusable membership/access/handling foundation for those features.

## Verification

```bash
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/inventory-first.png
python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/inventory-operation.png
python3 tools/run_spatial_scene.py --oscp-smoke work/preview/spatial-scene/inventory-oscp.png
```

[Core checks](../tests/SpatialChecks/InventoryChecks.cs) cover physical fit/rotation/masks, recursive and total mass, nested access, real equipment service availability, distinct AP/time costs, no premature transfer, cancellation/holds, paid handling, fixtures, deterministic continuation and atomic invalid restores. Godot additionally exercises actual inventory input/rendering, real-time inspection, packing, nested object views, closing during work, save equivalence and combat AP. [Object sources and prompts](../game/Assets/inventory-objects/README.md) preserve the built-in image generation provenance and registration.

## Scoped packing update

[Container-target packing and the gear standard](29-gear-data-standard.md) distinguish exact manual grid placement from deliberate drops onto a different open container or pouch. Container drops use free space first, then rearrange only the addressed compartment if necessary. Right-click Arrange is available on blank grid, its items, or the external container/pouch. Every moved item pays handling time/AP; completion is atomic and saveable. Failed manual drops and spills never invoke rearrangement. Spillage itself remains future work.

## Body fitting update

Press **Body fit · hex view** in the left panel to inspect the authored nine-shoulder-cell silhouette, move a clothing anchor and rotate it in sixth turns. **Fit / equip** uses the ordinary timed/AP-bearing Move command. Body poses and physical storage envelopes are independent. [The body-fit guide](30-body-fit-prototype.md) explains controls, mask authoring and the remaining coverage/attachment boundaries.

## Geometric pocket coverage update

The trousers now have separate hip and cargo regions with authored access footprints. A closed coat blocks the hip mouths while lower cargo stays accessible; opening changes only its authored cover masks. Segmented hover and body diagnostic markers read that same state. See [the coverage guide](31-pocket-coverage-prototype.md) for native fields, fallback behavior and proof limits.
