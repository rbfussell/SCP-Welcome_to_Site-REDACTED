# Front/back body inspection and removal

**Implemented native fixture, October 30:** inspect exposed worn material and drag a wearable from the body into existing storage. See the [runtime guide](doc-docs-40-body-inspection-and-removal-md.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [implementation ledger](status.html). This is ordinary equipment handling, not SCP canon.

## Exposed targets

The [read-only body query](file-src-sitesimulation-inventory-equipment-bodyfit-inventorybodyinspection-cs.html) projects the player's worn items in dressing order onto explicit front/back surfaces. Active open/closed masks clip lower items; each exposed hex has one target. Open jackets expose shirts, closed jackets cover them, and back-mounted packs appear on the back. Wrapping belts retain one identity on both sides. No held gear, packed contents, stored items or foreign outfit is enumerated.

Targets report ordinary removal access. Visible material can still be restrained by other gear. Neither camera side nor diagnostic picking grants physical access. The [symmetric fit masks](body-fit-cleanup.html), [overlay bands](doc-docs-36-overlay-sublayers-md.html) and [provider graph](mount-points.html) remain authoritative.

## Native interaction and continuation

In **Body fit · hex view → Inspect worn**, click selects, double-click opens the middle gear view, right-click opens normal context actions and dragging starts ordinary handling when permitted. [Input/source handling](file-game-presentation-inventory-inventorybodyfitsurface-inspect-cs.html) retains the pressed cell/item across the drag threshold and revalidates it. Back view mirrors screen coordinates, without changing body coordinates, physical mount or actor facing.

Disable inspection to use the existing click/Default/Apply fitting tools. [Incoming body drops](body-drag-fitting.html) keep exact placement and authored mount policy; one-sided inspection targets require the matching view. A flip cancels an unsubmitted drag. Reopening resets transient inspection choices. Normal browser closure preferences and compartment access still apply.

Paid moves retain the source on the body until completion and use exact grid placement, normal time/AP and whole-graph revalidation. [Persistence](system-persistence.html) was schema 14 at this slice’s qualification with unchanged content identity; matching saves and paid continuations remain compatible. Loaded providers retain their descendants and counted mass.

## Evidence and limits

- [Domain checks](check-bodyinspectionchecks.html): exposed own surfaces, no hidden contents, changing covers, front/back packs, restraint, loaded removal, AP and continuation.
- [Native checks](file-game-presentation-spatialscene-bodyinspectionsmoke-cs.html): mode/flip, mirrored picking, selection, context, exact drops, paused refusal and paid work.
- [Recorded results](doc-docs-reviews-inventory-runtime-checks-md.html): actual evidence; wiki building does not run gameplay checks.

This is colored diagnostic hex material. [Character-matched sprites and alpha-clipped highlights](wearable-appearance.html) remain future presentation. The final visual picker must refine sprite occlusion separately from physical masks. Exterior child attachments still use the enlarged gear view; body targets identify their worn provider. World clothing, general cloth/transparent-material rendering and anatomical mount inference are not added.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [incoming fitting](body-drag-fitting.html), [body geometry](body-fit-cleanup.html), [mount points](mount-points.html), [appearance target](wearable-appearance.html), [design library](documents.html).

Presentation follow-up: [full-body sprite qualification](full-body-paperdoll.html) tests a separate partial outfit with alpha-based picking/highlights. It does not replace this page's physical fit, access, provider or handling contracts.

Presentation follow-up: [engine-rendered paperdoll mosaic](paperdoll-mosaic.html) now covers the base groin beneath real clothing. The current accepted reference is shirtless with close-fitting tan fabric briefs, replacing the painted undershirt/loose shorts. The earlier featureless mannequin request remains unimplemented. Physical equipment and access rules are unchanged.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.

[Independent source/destination views](inventory-transfers.html) retain an accessible source compartment while inspecting other gear, allowing direct pocket-to-provider transfers. Physical access, exact fit, paid handling and provider-dependent closure remain authoritative.

Current floor/persistence follow-up: [ground inventory and piles](ground-storage.html) now supply persistent clothes staging and top-only pile access through ordinary handling. Current snapshots use schema 15, with matching schema-14 migration; schema-14 statements on this page describe this feature’s earlier qualification. This bounded floor feature supersedes any earlier ground-as-future statement, while world floor-item sprites and general hauling remain unimplemented.

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify the jacket's front chest pocket and all four front trouser compartments: contour hover, snug/unsealable hip inspection, paid cargo opening, pocket-specific actions and ordinary storage. Jacket coverage still blocks hips while leaving lower cargo accessible. Backpack main/side contours now follow the mounted bag side with independent Zip/Buckle actions; other unregistered compartments and animation remain future work.
