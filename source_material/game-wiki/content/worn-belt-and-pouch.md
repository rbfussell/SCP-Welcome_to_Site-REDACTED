# Worn waist belt and direct pouch

**Implemented bounded reference-body fixture, November 4.** **I → Sprites** shows the actual waist belt front/back and a directly attached utility pouch at its real local hex position and sixth-turn rotation. See the [runtime/metadata guide](doc-docs-47-worn-belt-and-pouch-paperdoll-md.html), [full-body paperdoll](full-body-paperdoll.html), [mount points](mount-points.html) and [appearance contract](wearable-appearance.html). This is ordinary equipment presentation, not SCP canon.

## Real parent, real child

The ordinary worn graph and committed dressing order choose the qualified parent. An authored provider plane maps its existing attachment coordinates to the reference canvas. The direct pouch retains its own physical item ID, parent, contents and alpha target. Rendering, selection tint and inverse picking share source pixels and the exact projected/rotated transform. Later worn garments retain their existing visual draw precedence. Front attachments are not visible through the rear body view.

Click selects the actual belt or pouch. Double-click inspects its ordinary gear illustration; a pouch selected directly from the body establishes **belt → pouch** even from an unrelated garment view. [The browser](file-game-presentation-inventory-inventorybrowser-cs.html) checks the own-worn attachment chain and ordinary inspect access before creating that trail. It neither opens flaps nor searches storage. Normal paid closures then control the right contents view. Context actions, physical coverage and removal restraints remain authoritative.

## Registration and scope

[Attachment presentation](file-game-presentation-inventory-inventorypaperdollart-attachments-cs.html) is separated from [base atlas/mask drawing](file-game-presentation-inventory-inventorypaperdollart-cs.html). `PaperdollBinding.attachmentPlane` supplies a normalized origin and pixel step; manifest `attachments` supplies definition/frame/anchor/surface bindings. The actual direct child's `AttachmentPose` supplies position and sixth-turn rotation. These are bounded presentation fields, not new physical rules or a public mod format.

This original slice qualified the reference body's normal waist pose and direct front pouch. The [worn chain follow-up](worn-attachment-chains.html) now also qualifies the front hook → pouch chain. Held/stored/foreign providers, packed contents, non-default belt fits and unregistered provider art are omitted. Omission feedback includes exterior children so unsupported gear does not silently disappear. [Physical body fit](body-fit-cleanup.html), six-direction [mounts](mount-points.html) and [inventory](system-inventory.html) still handle unsupported art cases in the engineering views.

A paid loaded-belt removal keeps both images until commitment, then omits the held assembly. Its pouch and packed descendants remain attached/contained normally. Exact pending restore reconstructs the same outfit. No native content identity, physical mask or save schema changes; [persistence](system-persistence.html) was schema 14 at this slice’s qualification.

## Art, catalog and evidence

The new front/back belt atlas is copied unchanged from built-in imagegen into the first [item bundle](asset-library.html), with [prompt/hash/source provenance](evidence-game-assets-items-worn-accessories-belts-utility-belt-provenance-md.html). Runtime registration still lives in the legacy paperdoll manifest; validated relative PNG references inside Assets allow the new path without a mass migration. The direct pouch uses its unmodified existing illustration. Flap animation, arbitrary cloth fitting and world outfit composition remain future work.

[The item catalog](items.html) now shows wearable versus exterior-attachment bindings, exact registered frames and links to original worn source sheets. Both original and published worn images are included in freshness validation.

[Native belt/pouch checks](file-game-presentation-spatialscene-paperdollbeltsmoke-cs.html) cover positions/rotations, picking, front/back, validated trails, paid contents/removal, loaded restore and unsupported-pose omission. The [runtime review](doc-docs-reviews-inventory-runtime-checks-md.html) records actual runs; this wiki build does not run gameplay tests.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [backpack/harness](worn-backpack.html), [worn masks](worn-alpha-masks.html), [asset library](asset-library.html), [ledger](status.html), [design library](documents.html).

[Independent source/destination views](inventory-transfers.html) retain an accessible source compartment while inspecting other gear, allowing direct pocket-to-provider transfers. Physical access, exact fit, paid handling and provider-dependent closure remain authoritative.

Current floor/persistence follow-up: [ground inventory and piles](ground-storage.html) now supply persistent clothes staging and top-only pile access through ordinary handling. Current snapshots use schema 15, with matching schema-14 migration; schema-14 statements on this page describe this feature’s earlier qualification. This bounded floor feature supersedes any earlier ground-as-future statement, while world floor-item sprites and general hauling remain unimplemented.

[Worn attachment chains](worn-attachment-chains.html) now render and alpha-pick the normal front belt → hook → pouch assembly through shared composed frames. Physical access, paid handling, independent trails and loaded ground transfers remain authoritative.
