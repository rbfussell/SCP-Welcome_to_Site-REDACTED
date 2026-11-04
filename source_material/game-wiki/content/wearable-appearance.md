# Character-matched clothing and silhouette highlights

**Confirmed presentation requirement, not yet the finished renderer.** The paperdoll should show the controlled character's matching body/appearance and fitted worn clothing sprites. The same equipped graph supplies item identity, appearance variants, placement and fastening state for the paperdoll and world character. See the [appearance contract](doc-docs-26-wearable-appearance-pipeline-md.html), [surface-mapping proposal](doc-docs-28-wearable-metadata-and-surface-mapping-md.html), [inventory](system-inventory.html) and [presentation](system-presentation.html). This is game presentation design, not an SCP canon claim.

## Invisible fitting geometry, visible garment shapes

The normal paperdoll hides its body/gear fit grid while retaining that grid for occupancy, contacts, layers, support and access. Engineering diagnostics can display it. The [symmetric body](body-fit-cleanup.html), [direct fitting drops](body-drag-fitting.html) and [attachment providers](mount-points.html) keep their physical contracts.

Highlights follow the current worn sprite's visible silhouette/alpha, including its pose, rotation, closure state and occlusion by other clothing. No tint or outline should fill transparent padding, neck/arm openings or cutout gaps with squares/hexes. Visual picking uses the same sprite transforms and occlusion. An open jacket reveals the correct underlying shirt. Preserve detailed pixel artwork rather than replacing its contour with a generic rounded rectangle or smoothing away pixels.

Sprite opacity is separate from fitting, access cover and removal restraint. A visually transparent surface can still prevent access. Render masks never change capacity, permit invalid dressing or reveal packed contents. Exact production masks, thresholds and asset-generation methods remain to be qualified.

## Current evidence and limits

The optional [full-body sprite proof](full-body-paperdoll.html) extends the [original upper-body fixture](paperdoll-sprite-proof.html) with actual underwear, trousers, socks, shoes and mount-aware backpack/harness art. Paired visual parts, committed closures/removal, alpha feedback, foreground head parts and authored base-arm cutouts are qualified for one default-pose reference body. Unsupported gear and painted modesty garments are explicitly disclosed. This remains a manually registered art fixture, not the finished character-matching/world wardrobe pipeline.

[Front/back body inspection/removal](body-inspection.html) now adds diagnostic exposed-material picking and paid drag sources, while final sprite-alpha picking remains future work. The [body-fit panel](file-game-presentation-inventory-inventorybodyfitpanel-cs.html) currently draws diagnostic hex cells/contacts. [Gear-view segments](file-game-presentation-inventory-inventorygearsurface-cs.html) and [inventory art](file-game-presentation-inventory-inventoryart-cs.html) already use source alpha for tint/picking; polygon outlines and diagnostic bodies are not the final clothing silhouette renderer. World human artwork remains baked, rather than dynamically composed from inventory.

The shared rig/sprite-baking and body-surface projection routes are proposals. The original clarification added no renderer; the separate sprite proof now qualifies a bounded presentation adapter without changing item state, save schema or physical handling. Future verification must compare paperdoll/world character identity, outfit and closure state, check transparent margins/openings and occlusion at relevant scales/rotations, and confirm unchanged physical access/fit outcomes.

Related: [engine](engine.html), [sprite direction](doc-docs-06-sprites-and-placeholders-md.html), [three-view inventory](doc-docs-27-three-view-inventory-and-body-fit-md.html), [body fit](body-fit-cleanup.html), [body drops](body-drag-fitting.html), [mount points](mount-points.html), [implementation ledger](status.html), [design library](documents.html).

Presentation follow-up: [engine-rendered paperdoll mosaic](paperdoll-mosaic.html) now covers the base groin beneath real clothing. The current accepted reference is shirtless with close-fitting tan fabric briefs, replacing the painted undershirt/loose shorts. The earlier featureless mannequin request remains unimplemented. Physical equipment and access rules are unchanged.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.

[Worn-only alpha masks](worn-alpha-masks.html) now qualify intact loose-source versus masked worn-shoe appearance, shared drawing/picking and unchanged physical handling.

[Asset organization and item bundles](asset-library.html) connect these authoring requirements to the [visual item catalog](items.html), current native definitions, source artwork and future representation-specific mask filenames.
