# Real jacket badge clip

**Implemented bounded reference-art fixture, November 6:** the field coat now offers a real exterior badge clip. The painted white card/red clip has been removed from a new preserved-alpha art version. Only an actual attached badge appears in the worn/enlarged jacket views. See the [implementation guide](doc-docs-57-jacket-badge-clip-md.html), [exact art prompt/provenance](evidence-game-assets-items-worn-clothing-coats-coat-provenance-md.html) and [recorded checks](doc-docs-reviews-jacket-badge-runtime-checks-md.html). This is equipment design, not an SCP canon claim.

## Physical equipment

The coat's front `badge-clip` support accepts the existing badge at `(0,2)`, with exposed-access checks and a 100 g limit. It is distinct from the jacket's four real [pockets](visible-pockets.html) and has no storage or lid. [Shirt and jacket](shirt-pockets-and-badge.html) share the same badge interface and physical item; paid transfer never duplicates it.

Open or zipped jackets retain the badge. A front-worn backpack covers it, blocking manipulation and [reader entry](badge-door-access.html) while preserving attachment. Loaded jacket removal carries the badge once. A jacket held in hand does not count as wearing or directly holding the badge. The existing [credential service](system-security.html) handles these states without a coat-specific exception.

## Hybrid gear interaction

The [enlarged gear surface](file-game-presentation-inventory-inventorygearsurface-cs.html) now preserves pocket/zipper menus and direct storage alongside mounts. Compatible attachments near a support use its mount quote; ordinary items on exposed pocket material use its normal compartment packing quote. An invalid explicit mount cannot become a pocket drop. An opaque attached badge blocks drops into the hidden pocket behind it, while exposed pocket material remains usable.

[Mounted contour highlighting](file-game-presentation-inventory-inventoryart-cs.html) uses the same projection as drawing and picking. Source-grid placement, [transfers](inventory-transfers.html), [ground staging](ground-storage.html), [body inspection](body-inspection.html), [mount clearance](mount-points.html), coverage and time/AP remain shared inventory facts.

## Artwork and continuation

[The coat bundle](item-coat.html) contains a new byte-identical copy of built-in imagegen output. Original atlas/source art is preserved; only coat frames/crops use the new version. [Asset authoring](asset-library.html) records bundle/provenance conventions. [Full-body presentation](full-body-paperdoll.html) renders the separate badge in open/closed front states; [appearance](wearable-appearance.html) still distinguishes this reference proof from production world clothing. Worn-only [shoe masks](worn-alpha-masks.html) are unrelated to the badge-free edit.

Placement is slightly inward over the left pocket to clear the default back-pack harness. Arbitrary body/coat poses, world wardrobe composition and animated gear closures remain future work. The enlarged coat still uses its closed reference image with authoritative state menus.

Kit remains 20 definitions/40 instances; no extra mutable region or save field is added. Immutable support identity changes, so start a fresh scene. Matching [schema-15 continuation](system-persistence.html) preserves paid transfers and attached children.

[Domain checks](check-jacketbadgechecks.html) and [native input checks](file-game-presentation-spatialscene-jacketbadgesmoke-cs.html) qualify the bounded behavior. Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [reader policy](badge-door-access.html), [pocket segments](paperdoll-pocket-segments.html), [engine](engine.html), [ledger](status.html), [design library](documents.html).
