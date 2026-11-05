# Ground inventory and physical piles

**Implemented bounded inventory feature, November 4.** The inventory always shows **Ground / at your feet** alongside its three inspection panes. These are real map-local item locations, not extra carried storage. See the [runtime contract](doc-docs-49-ground-inventory-and-piles-md.html), [inventory](system-inventory.html), [world](system-world.html), [presentation](system-presentation.html) and [ledger](status.html). This is ordinary gameplay, not an SCP canon adaptation.

## Drop, stack and dress

The current physical navigation hex supplies a square floor grid. Drag an accessible item to an exact cell; overlapping items stack. The explicit Put down shortcut prefers free space, then the pile origin. Rotation remains horizontal/vertical. Neither path rearranges neighbors. A floor-grid drop places something on the ground, even over a container; inspect the container and address its normal compartment to insert inside it.

Piles retain separate item identities, condition and children. Put down the backpack, coat and shirt, then put them back on in reverse order without needing a spare hand or backpack capacity. The grid remains visible in equipment, body-fit and sprite modes. [Body removal](body-inspection.html), [worn backpacks](worn-backpack.html), [mount providers](mount-points.html) and [independent source/destination views](inventory-transfers.html) still use ordinary handling.

**Move covering items first.** A higher overlapping footprint blocks the item below, its compartments and its descendants. Top items draw/pick above lower ones; a badge counts overlapping items underneath an exposed top item. Removing a blocker exposes the next item. There is no implicit rummaging, free unstacking, floor Arrange or count merging. Normal storage packing cannot bypass a buried ancestor.

## Authority and persistence

[InventoryGround](file-src-sitesimulation-inventory-placement-inventoryground-cs.html) resolves canonical `ground:q:r` roots from the map without saving empty tiles or mutating queries. [Root/access handling](file-src-sitesimulation-inventory-access-inventoryaccessservice-cs.html) requires the same physical hex as the actor. Walking away leaves the pile and invalidates unreachable container views; returning shows the same items. Root IDs are local to the current map context.

The current patch is 10×10 cells at 50 mm, capped at 100 kg and 1000 mm accumulated envelope thickness per cell. These are explicit prototype defaults. Piles use `GroundOrder` as relative ordering, independent of worn order and body layers. [Placement](file-src-sitesimulation-inventory-placement-inventoryplacement-cs.html) resolves the exact paid result; [saving](system-persistence.html) preserves item locations, order and pending handling.

Current saves use **schema 15**. Matching schema-14 inventories migrate without ground placements; native kit identity remains unchanged. Invalid floor IDs, placements, order/limits and paid continuations fail detached restore. [Domain checks](check-groundchecks.html) and [native input checks](file-game-presentation-spatialscene-groundsmoke-cs.html) are linked to the [recorded results](doc-docs-reviews-inventory-runtime-checks-md.html); wiki generation does not run gameplay tests.

## Limits and future interface

World floor-item sprites/collision, spills, autonomous hauling, count-stack merging, 3D support/stability and cross-domain floor transfers remain future work. The logical pile/rectangular-envelope model does not claim physical cloth or rigid-body simulation. Assets and native item counts are unchanged.

Floating inventory windows inside their parent object's illustration are a user-proposed production-UI direction. That layout is not implemented or finalized here. Its eventual controls should reuse the same item graph, validated access, physical closures and paid actions. The current debug browser and floor dock remain separate.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [world](system-world.html), [body inspection](body-inspection.html), [worn backpacks](worn-backpack.html), [mount points](mount-points.html), [source/destination views](inventory-transfers.html), [ledger](status.html), [design library](documents.html).

[Worn attachment chains](worn-attachment-chains.html) now render and alpha-pick the normal front belt → hook → pouch assembly through shared composed frames. Physical access, paid handling, independent trails and loaded ground transfers remain authoritative.

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.
