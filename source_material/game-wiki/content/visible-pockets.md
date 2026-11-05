# Every depicted pocket is usable

**Current authoring contract, November 5:** if artwork shows a pocket or pouch, it must have real usable storage. Earlier decorative-pocket exceptions are superseded. This is ordinary equipment design, not SCP canon. The [implementation guide](doc-docs-55-visible-pocket-contract-md.html) records IDs, provisional capacities, initial states and verification boundaries.

## Current coverage

The field coat has four front pockets (`chest`, `chest-left`, `lower-left`, `lower-right`). Trousers have two snug/unsealable hip pockets, two buttoned cargo pockets and two buttoned rear pockets. Front/back cargo illustrations share the same real inventories. The backpack has main, side and front-pouch compartments on its bag side, with separate closures. The shirt retains two buttoned chest pockets and its distinct badge clip. [Pocket contours](paperdoll-pocket-segments.html) link these illustrations to authoritative regions; pixels never create capacity automatically.

New pockets start secured on every kit instance. Coat enlarged art uses a metadata crop of its existing worn source so layouts agree; original PNG bytes are preserved. Enlarged views offer explicit compartment buttons where the current illustration lacks a hotspot, including rear trouser pockets. [Asset authoring](asset-library.html) and the [visual item catalog](items.html) must expose the same region identities and registrations.

## Physical authority

[Inventory services](system-inventory.html) retain capacity, packing, independent closures, paid time/AP, source retention and completion revalidation. [Coverage](doc-docs-31-pocket-coverage-prototype-md.html) uses the actual front/back mouth: a jacket may cover rear pockets even when its front is open. [Transfers](inventory-transfers.html), [ground staging](ground-storage.html), [body inspection](body-inspection.html), [worn container drops](worn-container-drops.html) and [sprite drops](sprite-equipment-drops.html) keep their normal access rules. Opening one view never duplicates or freely opens another inventory.

The kit remains 20 definitions/40 instances but immutable content identity changes. Use a fresh scene; matching [schema-15 persistence](system-persistence.html) adds no new mutable field. [Shirt/badge design](shirt-pockets-and-badge.html) still distinguishes physical storage/attachment from future credential readers; painted badge pixels grant no permission.

## Evidence and limits

[Domain checks](check-visiblepocketchecks.html) verify region sets, closed refusal, paid storage, rear mouths and exact loaded continuation. [Native contour checks](file-game-presentation-spatialscene-visiblepocketsmoke-cs.html) and [pack checks](file-game-presentation-spatialscene-paperdollpackpocketsmoke-cs.html) exercise actual input. See [actual results](doc-docs-reviews-inventory-runtime-checks-md.html). Automatic image segmentation, arbitrary body variants, flap animation, final capacity balance and world wardrobe composition remain future work.

Related: [ledger](status.html), [engine](engine.html), [presentation](system-presentation.html), [appearance](wearable-appearance.html), [full body](full-body-paperdoll.html), [backpacks](worn-backpack.html), [mounts](mount-points.html), [design library](documents.html).
