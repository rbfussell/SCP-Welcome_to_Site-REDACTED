# Upper-body paperdoll sprite proof

**Historical first qualification.** The current native view is [full-body paperdoll/base garments](full-body-paperdoll.html), including new kit identity, six clothing bindings, paired feet and explicit visual-only modesty base. The October 31 scope below is preserved as historical evidence; the linked source pages reflect current code.

**Implemented bounded art fixture, October 31.** Open **I → Sprites** for one reference upper body with worn shirt/coat layers, front/back artwork and an openable jacket. This is ordinary game presentation, not SCP canon. Read the [runtime/limitations guide](doc-docs-41-paperdoll-sprite-proof-md.html), [appearance target](wearable-appearance.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [implementation ledger](status.html).

## One outfit source, separate art geometry

[The appearance adapter](file-game-presentation-inventory-inventorypaperdollart-cs.html) reads the actual player's compatible worn mounts, instance IDs, dressing order, authored default pose and committed fastening state. Only this fixture's avatar/body/shirt/coat bindings are qualified. Held/stored/foreign items and packed contents are not artwork layers. Unsupported worn gear is explicitly listed as omitted; the proof cannot establish physical access through omitted equipment.

The manifest registers a generated atlas to a common display canvas, separately authored front/back rectangles and a head/neck foreground cutout. Drawing/picking share registration, letterboxing, order and source alpha. Open coat pixels reveal the shirt; higher opaque pixels cover lower tint. The foreground head occludes the far collar. The front includes the neck; the back cutout ends at the hair so the collar stays over the neck. Highlights modulate sprite pixels without filling transparent padding or garment holes with cells or boxes. Nearest filtering and a fixture-only alpha cutoff of 0.1 remove generation fringe; this does not establish a production material policy.

Physical [body geometry](body-fit-cleanup.html), [fitting drops](body-drag-fitting.html), [inspection/removal](body-inspection.html), pocket access and [provider attachments](mount-points.html) remain authoritative and separate from visible pixels. Use those diagnostics for fitting, dragging and attachment work; this sprite proof supplies selection, ordinary gear inspection/context and normal paid Zip/Unzip only.

## States and evidence

The [native panel](file-game-presentation-inventory-inventorypaperdollproof-cs.html) uses existing commands. Paused controls supply no free fastening. A jacket retains its old sprite until paid completion. Front/back inspection and hover perform no physical work. Reopening resets the transient side; [persistence](system-persistence.html) remains schema 14 with unchanged inventory identity and exact paid continuation.

[Native checks](file-game-presentation-spatialscene-paperdollsmoke-cs.html) exercise source-alpha overlap/holes, head occlusion, resize, own identity/order, selection, normal restraint, pending/paused fastening, restore and side reset. See [actual runtime evidence](doc-docs-reviews-inventory-runtime-checks-md.html). Building the wiki does not execute game checks.

## Limits and next art work

This is a manually registered upper-body stand-in, inspired by the player reference. Packs, full-body clothing, attachments and non-default poses are omitted and disclosed. Loose item illustrations and world actors remain separate baked artwork; universal character matching, appearance variants, world walks/facings, procedural fit and general cloth/transparency are not implemented. [The shared appearance contract](wearable-appearance.html) and rig/surface-mapping proposals retain their future scope. Asset prompts/provenance are linked from the runtime guide.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [appearance](wearable-appearance.html), [body inspection](body-inspection.html), [body drops](body-drag-fitting.html), [fit](body-fit-cleanup.html), [mounts](mount-points.html), [design library](documents.html).
