# Mount points and placement extenders

**Implemented native fixture:** belt → utility hook → pouch → contents. This is ordinary development equipment, not an SCP canon artifact. See the [runtime contract and controls](doc-docs-37-placement-extenders-and-mount-points-md.html) and [inventory system](system-inventory.html).

## Attached providers

The hook uses an upper connector on belt webbing and offers a separate lower point for the existing pouch. It has one parent and no loose-item interior or lid. Required/offered interfaces, occupied shape, mating contacts, surface and load are distinct authored facts. The hook weighs 80 g; its lower mount supports 500 g of descendants. See the [gear author sheet](doc-templates-gear-data-sheet-md.html) and [gear standard](doc-docs-29-gear-data-standard-md.html).

[Attachment frames](file-src-sitesimulation-inventory-equipment-inventoryattachmentframes-cs.html) compose parent-local position and sixth-turn rotation without rewriting child placements. Square-grid contents remain contents. [Shared exterior clearance](file-src-sitesimulation-inventory-equipment-inventoryattachmentclearance-cs.html) prevents gear on different hooks, regions or worn belts from occupying the same place. Opposite front/back planes are separate. A child's required connector may meet its immediate provider; all other overlap still fails. This is coarse surface geometry, not arbitrary 3D depth or cloth physics.

Clothing cover, exposed contacts, fit bands, compatibility, ancestor access, hierarchy depth and recursive mass remain authoritative. Explicit surfaces inherit through nested providers. A rejected action leaves the graph intact. See [physical overlays](doc-docs-36-overlay-sublayers-md.html) and [the provider design](doc-docs-33-fit-ranks-and-worn-stacks-md.html).

## Interaction and continuation

[Clothing presentation](wearable-appearance.html) will hide normal body/gear fit grids and highlight actual visible sprite cutouts. Physical support/contact geometry remains separate from the rendered silhouette.

[Symmetric body fitting](body-fit-cleanup.html) adds bounded adjustable native belt spans and a human-body chest X. Resolved cells affect interference and contact exposure; the five central webbing points stay inside the fixed core. Extension does not create new mounts or resize the gear illustration.

The [gear-view presentation](system-presentation.html) draws and alpha-picks visible exterior descendants in composed coordinates. Drop on the lower hook point while viewing the belt; double-click the hanging pouch to follow **belt → hook → pouch** in the middle view. Contents stay on the right. Preview rotation carries visible descendants without doing work.

Normal inventory transactions charge time/combat AP and revalidate clearance/access at commitment. [Persistence](system-persistence.html) preserves exact empty/loaded paid continuation. This slice was qualified on schema 14; changed immutable fixture identity requires a fresh scene. Invalid saved collisions fail before replacing live state.

## Evidence and limits

- [Domain checks](check-hookchecks.html): rotations, mating exceptions, cross-provider/cousin clearance, surfaces, load, paid work, ancestry and invalid restore.
- [Native input checks](file-game-presentation-spatialscene-hooksmoke-cs.html): pointer drops, extended targets, trails, closures, rotation, occupied-mount refusal and loaded detachment.
- [Recorded run results](doc-docs-reviews-inventory-runtime-checks-md.html): actual evidence; this wiki build does not run gameplay checks.

Latching hooks, keyring content, arbitrary depth offsets, assembly packing envelopes, swinging and world clothing composition remain future work. This does not change the SCP module contract or reveal hidden contents to actors.

Related: [engine](engine.html), [implementation ledger](status.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [belt foundation](doc-docs-32-belt-and-attachment-prototype-md.html), [design library](documents.html).

[Body-fit drops](body-drag-fitting.html) are a separate target for wearable gear in the diagnostic body view. Enlarged provider surfaces keep their explicit mount points, six attachment rotations and existing clearance rules.

[Body inspection/removal](body-inspection.html) targets the worn provider on its exposed front/back material; moving it carries the loaded assembly through normal handling. Exterior children remain separately inspectable/dragged in the enlarged gear view.

Presentation follow-up: [full-body sprite qualification](full-body-paperdoll.html) tests a separate partial outfit with alpha-based picking/highlights. It does not replace this page's physical fit, access, provider or handling contracts.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.

[Worn waist belt and direct pouch](worn-belt-and-pouch.html) now qualify default-pose front/back belt art and a real direct attachment, with normal access, paid handling and source-backed catalog registration. The [worn hook-chain follow-up](worn-attachment-chains.html) now qualifies the normal front hook → pouch chain.

[Independent source/destination views](inventory-transfers.html) retain an accessible source compartment while inspecting other gear, allowing direct pocket-to-provider transfers. Physical access, exact fit, paid handling and provider-dependent closure remain authoritative.

Current floor/persistence follow-up: [ground inventory and piles](ground-storage.html) now supply persistent clothes staging and top-only pile access through ordinary handling. Current snapshots use schema 15, with matching schema-14 migration; schema-14 statements on this page describe this feature’s earlier qualification. This bounded floor feature supersedes any earlier ground-as-future statement, while world floor-item sprites and general hauling remain unimplemented.

[Worn attachment chains](worn-attachment-chains.html) now render and alpha-pick the normal front belt → hook → pouch assembly through shared composed frames. Physical access, paid handling, independent trails and loaded ground transfers remain authoritative.

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Shirt pockets and detachable badge](shirt-pockets-and-badge.html) add two buttoned chest compartments and a real clip-mounted badge. Covered retention and manipulation are separate; this changes native kit identity to 20 definitions/40 instances while retaining schema 15. [Badge-controlled access](badge-door-access.html) now implements exposed wearing or equipped-hand presentation at one local reader; production credential administration remains future work.

[Current visible-pocket contract](visible-pockets.html): depicted pockets require real storage; multiple views share one inventory.

[Badge-controlled test door](badge-door-access.html): exposed worn credentials or equipped-hand presentation use physical inventory and shared security validation.

[Real jacket badge clip](jacket-badge-clip.html) now combines four usable pockets with exterior mounting, badge-free source art and the existing reader checks.
