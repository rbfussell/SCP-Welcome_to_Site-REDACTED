# Sprite equipment drops

**Implemented bounded reference-body interaction, November 4.** **I → Sprites** accepts free body-hex fitting drops and exact exposed belt/hook targets, and supplies worn-sprite drag sources. See the [runtime/metadata guide](doc-docs-51-sprite-equipment-drops-md.html), [diagnostic drops](body-drag-fitting.html), [worn chains](worn-attachment-chains.html) and [appearance contract](wearable-appearance.html). This is ordinary equipment handling, not SCP canon.

## Exact input, physical validation

The selected design is **free body-hex placement**, not default snapping. The manifest's authored `bodyPlane` projects the real twenty-row body onto one reference sprite. Picking and temporary fit outlines use the same polygons; blank margins/gaps refuse. Back view mirrors coordinates. R over the body cycles six body orientations; normal allowed rotations, contacts, coverage/restraints and fit still decide success. Distinct front/back mounts use the matching viewed side.

Registered normal poses preview the actual garment; other poses disclose their physical footprint without borrowing incompatible artwork. A valid non-default committed fit may remain omitted by the art proof. This is an approximate authored projection, not cloth fitting or automatic anatomical segmentation.

[Drop projection](file-game-presentation-inventory-inventorypaperdollart-drop-cs.html) also offers exact points on registered visible providers, including nested hooks. A later unrelated opaque garment hides the point. Existing descendants leave occupied points targetable for explicit collision feedback. Ties prefer the later drawn provider. R cycles six attachment turns; storage still has two independent orientations. Registered loaded exterior descendants follow draft transforms; packed contents stay excluded.

[The input surface](file-game-presentation-inventory-inventorypaperdollsurface-drop-cs.html) quotes the same [inventory](system-inventory.html) commands as diagnostic views. Invalid/paused release leaves the source. Accepted work pays normal time/AP, retains its source until commitment and revalidates access. Current [schema-15 continuation](system-persistence.html) gains no new fields or kit identity. Esc dismisses/cancels without physical closure changes.

Worn alpha pixels supply drag sources as well as selection/inspection. The pressed item identity survives the drag threshold; normal access still rejects covered or restrained gear. [Independent source/destination views](inventory-transfers.html) and [ground piles](ground-storage.html) remain ordinary transfer paths.

## Evidence and limits

The focused launcher `--sprite-drop-smoke` runs the new fixture alone; paperdoll/full inventory modes include it in their larger regressions. [Native input checks](file-game-presentation-spatialscene-paperdolldropsmoke-cs.html) cover free anchors/six turns, footprint-only poses, margins, pause, Esc, paid retention/exact restore, belt and nested hook targets, occupied refusal and loaded sprite sources. See [recorded run results](doc-docs-reviews-inventory-runtime-checks-md.html); building the wiki does not run gameplay checks.

General body variants, arbitrary fitted artwork, other pocket segments, cloth physics and world wardrobe composition remain future work. Enlarged gear/contents views retain detailed storage/mount controls.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [body drops](body-drag-fitting.html), [body fitting](body-fit-cleanup.html), [inspection](body-inspection.html), [mount points](mount-points.html), [worn chains](worn-attachment-chains.html), [full-body proof](full-body-paperdoll.html), [appearance](wearable-appearance.html), [transfers](inventory-transfers.html), [ground](ground-storage.html), [ledger](status.html), [design library](documents.html).

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify the jacket's front chest pocket and all four front trouser compartments: contour hover, snug/unsealable hip inspection, paid cargo opening, pocket-specific actions and ordinary storage. Jacket coverage still blocks hips while leaving lower cargo accessible; other body pocket registrations remain future work.
