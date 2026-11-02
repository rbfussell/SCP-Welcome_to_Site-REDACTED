# Engine-rendered paperdoll mosaic

**Implemented bounded presentation, November 2.** In **I → Sprites**, Godot draws an opaque coarse mosaic over the reference body's groin on front/back, beneath actual clothing. It samples/quantizes the base image into blocks at runtime, using authored normalized regions and reference-canvas block size. The PNG has no baked mosaic. See the [runtime guide](doc-docs-43-paperdoll-mosaic-md.html), [full-body fixture](full-body-paperdoll.html), [appearance](wearable-appearance.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [ledger](status.html). No SCP canon changes.

## Committed outfit, visual overlay

Actual worn garment alpha conceals the overlay; real underwear/trousers removal reveals it after paid commitment. Held/stored gear does not conceal it. It has no item identity, mass, fit, physical access mask, storage or protection and adds no time/AP bypass. Picking continues through actual garment alpha and never selects the mosaic. Both sides use the same resized letterbox transform. [Body inspection](body-inspection.html) and normal inventory services retain physical authority.

[The renderer](file-game-presentation-inventory-inventorypaperdollart-cs.html) draws base → engine mosaic → worn layers → foreground head. [Tile generation](file-game-presentation-inventory-inventorypaperdollmosaic-cs.html) is separate readable presentation code. Matching [saved outfits](system-persistence.html) reconstruct the mosaic without a flag or content-identity change; schema stays 14.

## Current reference and asset history

The current shirtless reference has **close-fitting matte tan fabric briefs** (`full-body-v2.png`), replacing the former painted undershirt/charcoal shorts. The briefs retain opaque coverage, waistband and fabric seams; their compact shape removes loose hems and reduces color contrast. They are visual pixels, not real inventory equipment. Front/back registration and the engine mosaic are unchanged; the original PNG is preserved.

The earlier featureless mannequin request was rejected by built-in imagegen at output validation (`sexual` category). It remains unimplemented. The current user-selected clothed alternative was accepted; exact prompts, source identities and hashes are recorded in the runtime guide's linked provenance. Other bodies, general garment transparency and world wardrobe composition remain future work.

## Evidence

[Native removal checks](file-game-presentation-spatialscene-paperdollunderwearsmoke-cs.html) exercise covered front/back regions, paid retention/commitment, non-target exposure, resized picking, restore reconstruction and no state mutation, with front/back captures. [Recorded runs](doc-docs-reviews-inventory-runtime-checks-md.html) distinguish actual results from planned checks; a wiki build does not run gameplay tests.

Related: [full-body paperdoll](full-body-paperdoll.html), [appearance](wearable-appearance.html), [inspection](body-inspection.html), [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [ledger](status.html), [design library](documents.html).
