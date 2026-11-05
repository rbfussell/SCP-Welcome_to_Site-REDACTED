# Paperdoll pocket segments

**Implemented bounded reference-body interaction, November 5.** **I → Sprites** now highlights and operates the jacket's existing front chest pocket directly, using its visible contour in open/closed jacket artwork. The viewer-left pocket maps to `avatar-coat / chest`; other illustrated pockets remain decorative. See the [runtime/metadata guide](doc-docs-53-paperdoll-pocket-segments-md.html), [worn container drops](worn-container-drops.html), [sprite equipment](sprite-equipment-drops.html) and [appearance contract](wearable-appearance.html). This is ordinary equipment, not SCP canon.

## Segments and authority

Optional `PaperdollPart.pockets` entries name a real `region`, `side` and normalized source-local `polygon`. Whole-part bag registrations remain separate. [Pocket drawing/validation](file-game-presentation-inventory-inventorypaperdollart-pockets-cs.html) uses the current texture/alpha and normal layer order. [Storage picking](file-game-presentation-inventory-inventorypaperdollart-storage-cs.html) requires topmost visible material before a segment can target its compartment. Transparent gaps and covered pixels do not turn into clickable rectangles. A front-mounted pack hides the covered chest pocket.

These are bounded authored visual registrations, not new physical pocket masks or automatic segmentation. [Inventory](system-inventory.html) continues to own coverage, capacity, closures, access, time/AP and exact placement. Native definitions, kit identity and [schema-15 continuation](system-persistence.html) are unchanged; source PNGs are retained.

## Interaction

Hover highlights just the segment and reads actual pocket state. Double-click inspects the jacket and opens that specific pocket through normal paid handling when necessary. Paused opening refuses. Right-click reads the pocket's Button/Unbutton action and securing preference, rather than the jacket-front zipper. The pocket remains a compartment of the jacket; dragging moves the owning garment through ordinary removal rules.

Drops reuse [worn container packing](worn-container-drops.html), without free opening or fallback to body fitting. Zipping the jacket front does not button its chest pocket. [Independent source views](inventory-transfers.html), [body inspection](body-inspection.html) and view-closing rules keep their existing behavior. Animation is not yet added.

[Native checks](file-game-presentation-spatialscene-paperdollpocketsmoke-cs.html) cover source-alpha hover, paid double-click/exact restore, context fastening, direct storage, both jacket states, rear exclusion and pack occlusion. Focused `--pocket-smoke` and the larger inventory suites run the fixture. See [actual evidence](doc-docs-reviews-inventory-runtime-checks-md.html); building the wiki does not run gameplay tests.

Only this front chest segment is qualified. Other clothing pockets, additional pack segments, arbitrary bodies, automatic tracing, flap animation and world wardrobe composition remain future work. [Asset organization](asset-library.html) and the [item catalog](items.html) expose registration evidence without becoming gameplay authority.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [worn containers](worn-container-drops.html), [sprite drops](sprite-equipment-drops.html), [body inspection](body-inspection.html), [full-body proof](full-body-paperdoll.html), [appearance](wearable-appearance.html), [transfers](inventory-transfers.html), [assets](asset-library.html), [items](items.html), [physical pocket coverage](doc-docs-31-pocket-coverage-prototype-md.html), [ledger](status.html), [design library](documents.html).
