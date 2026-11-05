# Worn container drops

**Implemented bounded reference-body interaction, November 5.** **I → Sprites** accepts drops into the real backpack main compartment and utility-pouch interior through registered visible bag material. This extends [sprite equipment drops](sprite-equipment-drops.html), [worn backpacks](worn-backpack.html), [worn attachment chains](worn-attachment-chains.html) and [independent transfers](inventory-transfers.html). See the [runtime/metadata guide](doc-docs-52-worn-container-drops-md.html). This is ordinary equipment, not SCP canon.

## Material and intent

Optional `PaperdollPart.dropRegion` links actual illustrated material to a real ordinary compartment. Backpack bag parts name `main`; harness parts name none. The pouch names `inside`. Topmost per-part alpha picking shares the current side, masks, source crop and attachment transform. Front/back mounting selects the bag side normally. Transparent gaps and unregistered garment material do not offer storage. Tint is drawn within normal outfit order, preserving later occlusion and packed-content privacy.

The provisional first-version precedence is **exact mount → registered container → free body fit**. The user was asked about this optional UI preference; this implementation choice is not a settled permanent design rule unless confirmed later. Closed/full container refusal never falls through to equipping clothing or choosing another pocket. R over container material leaves body/mount drafts unchanged; normal packing chooses a legal stored orientation.

## One normal transaction

[Storage targeting](file-game-presentation-inventory-inventorypaperdollart-storage-cs.html) supplies an item/compartment ID. [The sprite surface](file-game-presentation-inventory-inventorypaperdollsurface-drop-cs.html) reuses the existing packing quote and AutoPlace action. Compartments must already be open/accessibly unsealable; there is no free opening, source search or hidden-content view. Normal free-fit/scoped-rearrangement rules, time/AP, access, load, cycle rejection, source retention and commitment revalidation remain authoritative.

[Source/destination trails](inventory-transfers.html) remain unchanged, allowing direct source-grid-to-worn-pouch transfers with an occupied hand. [Ground storage](ground-storage.html) remains a separate physical staging location. Invalid/paused release changes nothing. Current [schema-15 continuation](system-persistence.html) gains no new fields or kit identity; source PNGs and native definitions are unchanged.

## Evidence and limits

[Native checks](file-game-presentation-spatialscene-wornstoragesmoke-cs.html) cover bag-only versus harness targets, closed/paused/full/self refusals, paid source retention/exact restore, retained source views, pouch transfer and both backpack mount sides. Focused `--worn-storage-smoke` and paperdoll/full inventory modes run the fixture. See [actual results](doc-docs-reviews-inventory-runtime-checks-md.html); wiki generation does not run gameplay checks.

Other body pocket segments, on-sprite closure animation, general body variants, compartment selectors, new packing algorithms and world outfit composition remain future work. Enlarged gear/contents views retain explicit pocket choice and fastening controls. [Asset organization](asset-library.html) and the [item catalog](items.html) expose registration evidence without changing physical authority.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [sprite drops](sprite-equipment-drops.html), [backpacks](worn-backpack.html), [chains](worn-attachment-chains.html), [mounts](mount-points.html), [full-body proof](full-body-paperdoll.html), [appearance](wearable-appearance.html), [transfers](inventory-transfers.html), [ground](ground-storage.html), [asset library](asset-library.html), [items](items.html), [ledger](status.html), [design library](documents.html).

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify the jacket's front chest pocket and all four front trouser compartments: contour hover, snug/unsealable hip inspection, paid cargo opening, pocket-specific actions and ordinary storage. Jacket coverage still blocks hips while leaving lower cargo accessible. Backpack main/side contours now follow the mounted bag side with independent Zip/Buckle actions; other unregistered compartments and animation remain future work.
