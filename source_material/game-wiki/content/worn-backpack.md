# Worn backpacks and harnesses

**Implemented bounded presentation, November 4.** **I → Sprites** displays an actual back-mounted pack as front straps/rear bag with visible shoulder connections. The harness uses the same registered shoulder scale on both sides, and the bag overlays it only on the mounted side. Front mounting puts that bag overlay in front instead. Two worn packs retain separate identities and inventories. See the [runtime guide](doc-docs-44-worn-backpack-paperdoll-md.html), [full-body fixture](full-body-paperdoll.html), [appearance](wearable-appearance.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [ledger](status.html). This is ordinary equipment presentation, not an SCP canon adaptation.

## A view of the existing item graph

Visible bag and harness pixels select one actual own worn pack. Double-click opens its enlarged gear view; right-click uses ordinary actions. Transparent gaps expose lower garment targets. The enlarged illustration retains its existing main/side segments, each opening its own contents view through physical access and paid closure services. Inspecting the pack alone reveals no contents. Decorative art does not add compartments.

[The adapter](file-game-presentation-inventory-inventorypaperdollart-cs.html) supports optional mount-slot-to-frame bindings, choosing pack/harness destinations from the committed mount and worn order. Stored, held, foreign and unqualified poses/mounts stay omitted. Optional per-part `front`/`back` side selection shares one filter between drawing and alpha/letterbox picking; an omitted side means both. The base-body part remains on both sides. The original front harness registration is retained. It does not modify fit, physical coverage, restraint, access or carrying capacity. The [inspection](body-inspection.html), [body fitting](body-fit-cleanup.html), [body drops](body-drag-fitting.html) and [provider graph](mount-points.html) retain their authority.

Normal paid handling keeps source art until completion and carries a loaded pack intact. [Persistence](system-persistence.html) reconstructs its appearance from the item graph and retains exact pending work. No content identity, mutable field or schema change: inventory schema was 14 at qualification.

## Evidence and limits

[Native pack checks](file-game-presentation-spatialscene-paperdollbackpacksmoke-cs.html) exercise actual identity, both mount directions, independent main/side contents, paused/paid closures, loaded removal/restore, held omission and two distinct packs. [The parent paperdoll suite](file-game-presentation-spatialscene-paperdollsmoke-cs.html) retains clothing picking/closure assertions outside pack alpha. [Recorded results](doc-docs-reviews-inventory-runtime-checks-md.html) distinguish actual checks from requirements; wiki building does not run gameplay tests.

The runtime guide links the untouched built-in-generated atlas and exact prompt/source/hash provenance. Worn bag art is static across lid states; existing menus/contents expose actual closure state. Other unregistered small-body pocket segments, worn closure animations, general cloth fitting, arbitrary poses, body variants, unregistered attachment art and world wardrobe composition remain future work.

Related: [full-body paperdoll](full-body-paperdoll.html), [appearance](wearable-appearance.html), [inspection](body-inspection.html), [body fit](body-fit-cleanup.html), [body drops](body-drag-fitting.html), [mounts](mount-points.html), [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [ledger](status.html), [design library](documents.html).

[Worn-only alpha masks](worn-alpha-masks.html) now qualify intact loose-source versus masked worn-shoe appearance, shared drawing/picking and unchanged physical handling.

[Worn waist belt and direct pouch](worn-belt-and-pouch.html) now qualify default-pose front/back belt art and a real direct attachment, with normal access, paid handling and source-backed catalog registration. The [worn chain follow-up](worn-attachment-chains.html) qualifies the normal front hook → pouch chain.

[Independent source/destination views](inventory-transfers.html) retain an accessible source compartment while inspecting other gear, allowing direct pocket-to-provider transfers. Physical access, exact fit, paid handling and provider-dependent closure remain authoritative.

Current floor/persistence follow-up: [ground inventory and piles](ground-storage.html) now supply persistent clothes staging and top-only pile access through ordinary handling. Current snapshots use schema 15, with matching schema-14 migration; schema-14 statements on this page describe this feature’s earlier qualification. This bounded floor feature supersedes any earlier ground-as-future statement, while world floor-item sprites and general hauling remain unimplemented.

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now add the jacket chest and four front trouser contours. Topmost bag/harness alpha still hides lower segments where opaque; physical coverage remains an independent access gate. Hip openings are snug/unsealable while cargo flaps use normal paid fastening.
