# Paperdoll pocket segments

**Implemented bounded reference-body interaction, November 5.** **I → Sprites** now highlights and operates the jacket's four front pockets, all six trouser compartments and backpack main/side/front directly. The viewer-left jacket pocket maps to `avatar-coat / chest` in open/closed art; trouser contours map to `avatar-trousers / left`, `right`, `cargo-left` and `cargo-right`. All depicted pockets now require usable storage; see the [current pocket contract](visible-pockets.html). See the [runtime/metadata guide](doc-docs-53-paperdoll-pocket-segments-md.html), [worn container drops](worn-container-drops.html), [sprite equipment](sprite-equipment-drops.html) and [appearance contract](wearable-appearance.html). This is ordinary equipment, not SCP canon.

## Segments and authority

Optional `PaperdollPart.pockets` entries name a real `region`, `side` and normalized source-local `polygon`. Whole-part bag registrations remain separate. [Pocket drawing/validation](file-game-presentation-inventory-inventorypaperdollart-pockets-cs.html) uses the current texture/alpha and normal layer order. [Storage picking](file-game-presentation-inventory-inventorypaperdollart-storage-cs.html) requires topmost visible material before a segment can target its compartment. Transparent gaps and covered pixels do not turn into clickable rectangles. A front-mounted [pack/harness](worn-backpack.html) hides the covered chest pocket.

These are bounded authored visual registrations, not new physical pocket masks or automatic segmentation. [Inventory](system-inventory.html) continues to own coverage, capacity, closures, access, time/AP and exact placement. The [complete-pocket follow-up](visible-pockets.html) adds region definitions and changes kit identity while retaining [schema-15 continuation](system-persistence.html); source PNGs are retained.

## Hip versus cargo pockets

The [trouser input fixture](file-game-presentation-spatialscene-paperdolltrouserpocketsmoke-cs.html) extends the same segment metadata, not simulation rules. Hip openings are snug/unsealable: accessible double-click inspection buys no fastening work and the menu offers no closure or securing preference. Cargo pockets retain normal paid opening and Button/Unbutton controls. All four front contours preserve existing region IDs and physical mouth masks.

A zipped coat covers hip mouths while the lower cargo pockets remain accessible. Topmost alpha prevents clicking through opaque coat panels; physical [pocket coverage](doc-docs-31-pocket-coverage-prototype-md.html) also gates visible targets. Unzipping restores physical hip access without making side-panel pixels transparent. Removing the coat exposes all four contours. The enlarged gear view retains explicit compartment choice. Rear art now registers separate rear seat pockets and aliases the existing cargo inventories. Drops remain ordinary paid scoped packing; hip inspection is read-only, not free item handling.

## Backpack main and side

[Backpack segment checks](file-game-presentation-spatialscene-paperdollpackpocketsmoke-cs.html) use the upper zipped panel for `main` and the visible buckled side pocket for `side`. Registrations follow the bag's mounted side in both frames; harnesses remain inspection-only. The depicted front pouch now has its own real compartment and buckle. Outside contours the established bag-material drop fallback remains `main`, with ordinary pack inspection on double-click.

Explicit segments take precedence over that fallback. A closed/full side pocket refuses without packing into main or fitting clothing. All three retain independent Zip/Unzip or Buckle/Unbuckle menus, paid opening, source retention and exact continuation. Contour highlighting suppresses whole-part storage tint; two packs retain independent actual owner IDs. The focused `--pack-pocket-smoke` launcher runs this fixture alone; pocket/full inventory modes also include it. The latest follow-up changes native compartment definitions, preserving source PNGs and save schema. [Worn backpacks](worn-backpack.html) and [container drops](worn-container-drops.html) retain their existing authority and limits.

## Jacket interaction

Hover highlights just the segment and reads actual pocket state. Double-click inspects the jacket and opens that specific pocket through normal paid handling when necessary. Paused opening refuses. Right-click reads the pocket's Button/Unbutton action and securing preference, rather than the jacket-front zipper. The pocket remains a compartment of the jacket; dragging moves the owning garment through ordinary removal rules.

Drops reuse [worn container packing](worn-container-drops.html), without free opening or fallback to body fitting. Zipping the jacket front does not button its chest pocket. [Independent source views](inventory-transfers.html), [body inspection](body-inspection.html) and view-closing rules keep their existing behavior. Animation is not yet added.

[Native checks](file-game-presentation-spatialscene-paperdollpocketsmoke-cs.html) cover source-alpha hover, paid double-click/exact restore, context fastening, direct storage, both jacket states, rear exclusion and pack occlusion. Focused `--pocket-smoke` and the larger inventory suites run the fixture. See [actual evidence](doc-docs-reviews-inventory-runtime-checks-md.html); building the wiki does not run gameplay tests.

Qualified registrations now include four coat pockets, front/rear trouser pockets with shared cargo views, and backpack main/side/front on its bag side. Arbitrary bodies, automatic tracing, flap animation and world wardrobe composition remain future work. [Asset organization](asset-library.html) and the [item catalog](items.html) expose registration evidence without becoming gameplay authority.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [worn containers](worn-container-drops.html), [sprite drops](sprite-equipment-drops.html), [body inspection](body-inspection.html), [full-body proof](full-body-paperdoll.html), [appearance](wearable-appearance.html), [transfers](inventory-transfers.html), [assets](asset-library.html), [items](items.html), [physical pocket coverage](doc-docs-31-pocket-coverage-prototype-md.html), [ledger](status.html), [design library](documents.html).

[Shirt pockets and detachable badge](shirt-pockets-and-badge.html) add two buttoned chest compartments and a real clip-mounted badge. Covered retention and manipulation are separate; this changes native kit identity to 20 definitions/40 instances while retaining schema 15. [Badge-controlled door access](badge-door-access.html) now implements exposed wearing or equipped-hand presentation at one test reader; production credential administration remains future work.
