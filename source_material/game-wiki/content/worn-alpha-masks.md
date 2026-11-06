# Worn-only alpha masks

**Implemented bounded presentation fixture, November 4.** Worn work shoes hide their empty shaft/back-rim pixels while their intact source remains available for loose representation. Both feet have separate front/back cutouts. This extends the [full-body paperdoll](full-body-paperdoll.html) and [appearance contract](wearable-appearance.html); see the [runtime guide](doc-docs-45-worn-alpha-masks-md.html). No SCP canon claim or physical inventory rule changes.

## One source, context-specific visibility

`PaperdollPart.frontWornCutouts` / `backWornCutouts` are optional arrays of normalized source-local polygons. They remove existing alpha only in the committed worn illustration. Unmasked parts preserve their original alpha. The base body cannot use these garment masks. Geometry is bounded and validated; masks are authored per source view rather than automatically inferred from a foot or body grid.

[The raster helper](file-game-presentation-inventory-paperdollwornmask-cs.html) creates visual holes on a private cropped copy. [The renderer](file-game-presentation-inventory-inventorypaperdollart-cs.html) caches small derived images/textures per part/side and disposes them with the view. Drawing, tint, alpha picking and visual exposure use those same worn pixels and transforms; transparent shoe openings do not remain selectable shoe pixels. Source PNGs and original alpha remain unchanged.

Packing and drag ghosts continue using intact [loose-object art](file-game-presentation-inventory-inventoryart-cs.html). Existing loose illustrations are still separate assets; this does not claim a universal source atlas, new sprite-drag controls, procedural segmentation or animated world outfits. A compatible source can be reused by keeping its full loose form and applying cutouts only for the worn form.

## Physical state remains authoritative

The actual equipped graph and dressing order determine when an item is worn. Pending removal retains its worn appearance until the paid transaction commits. [Inventory](system-inventory.html), [body fitting](body-fit-cleanup.html), [inspection/removal](body-inspection.html), physical coverage and restraints retain normal access, time/AP, ownership and save behavior. Showing a sock through a visual hole does not make it physically accessible. This is distinct from suppressing reference-body pixels beneath sleeves, or changing a garment's fit mask.

## Evidence and limits

[Native mask checks](file-game-presentation-spatialscene-paperdollmasksmoke-cs.html) cover both feet/sides, surviving source/loose pixels, validation, masked picking, resize and unchanged simulation state. The parent [paperdoll fixture](file-game-presentation-spatialscene-paperdollsmoke-cs.html) retains paid shoe/sock handling and restore checks. See [recorded results](doc-docs-reviews-inventory-runtime-checks-md.html); building this wiki does not run gameplay checks.

Only the authored reference body/default shoe pose is qualified. Arbitrary gear poses, world clothing composition, deformed footwear and automatic mask authoring remain future work.

Related: [presentation](system-presentation.html), [inventory](system-inventory.html), [full-body proof](full-body-paperdoll.html), [appearance](wearable-appearance.html), [worn backpack](worn-backpack.html), [implementation ledger](status.html), [design library](documents.html).

[Asset organization and item bundles](asset-library.html) connect these authoring requirements to the [visual item catalog](items.html), current native definitions, source artwork and future representation-specific mask filenames.

[Real jacket badge clip](jacket-badge-clip.html) now combines four usable pockets with exterior mounting, badge-free source art and the existing reader checks.
