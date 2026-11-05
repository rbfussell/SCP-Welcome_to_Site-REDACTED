# Worn attachment chains

**Implemented bounded reference-body fixture, November 4.** **I → Sprites** shows the normal waist **belt → hook → pouch** chain. Each item retains its own identity, source alpha, position and sixth-turn rotation. This extends [direct belt/pouch art](worn-belt-and-pouch.html) using the existing [physical mount system](mount-points.html). See the [runtime/registration guide](doc-docs-50-worn-attachment-chains-md.html), [full-body paperdoll](full-body-paperdoll.html) and [appearance contract](wearable-appearance.html). This is ordinary equipment, not SCP canon.

## Physical frames, distinct targets

[The attachment adapter](file-game-presentation-inventory-inventorypaperdollart-attachments-cs.html) walks actual attachment edges and composes each parent-local frame with [the shared geometry helper](file-src-sitesimulation-inventory-equipment-inventoryattachmentframes-cs.html). Explicit surfaces override inherited `mounted` surfaces. Drawing, tint and inverse alpha picking share the resolved transform. Packed contents are excluded. A missing provider registration omits its descendants rather than drawing floating gear; unsupported own exterior equipment stays disclosed.

Click selects the actual hook or pouch. Double-click inspects through ordinary access services; selecting the pouch from another garment produces **belt → hook → pouch** in the middle trail. [Independent source/destination views](inventory-transfers.html) remain available for direct storage-to-mount transfers. Inspection does not reveal contents or perform paid work.

Dropping the loaded belt onto [current-tile ground](ground-storage.html) carries the complete graph; picking it up restores the same worn chain. Source sprites remain until normal handling commits. Clearance, load, coverage, closures and time/AP remain authoritative. [Persistence](system-persistence.html) still uses schema 15 and matching schema-14 migration; no new item/save fields or immutable kit identity are introduced.

## Artwork, evidence and scope

The hook reuses its unchanged source PNG and authored gear-view anchor, scaled to the paperdoll's provider step. The pouch registration is unchanged. [The catalog](items.html) exposes both registered exterior bindings; [asset organization](asset-library.html) preserves source/provenance and distinguishes registration from the future runtime bundle loader.

[Native chain checks](file-game-presentation-spatialscene-paperdollhooksmoke-cs.html) cover six hook rotations, inherited nested rotation/position, separate alpha targets and inspection, resizing, rear exclusion, paid loaded drop/pickup, exact continuation and omission of packed contents/unsupported roots. See [actual run evidence](doc-docs-reviews-inventory-runtime-checks-md.html); wiki generation does not run gameplay tests.

Only the normal waist root and registered front hook/pouch chain are qualified. Back attachment art, crossed-belt/chest art, arbitrary provider artwork, swinging/depth, general fitted sprite generation, closure animation and world clothing composition remain future work.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [mount points](mount-points.html), [direct belt/pouch](worn-belt-and-pouch.html), [full-body proof](full-body-paperdoll.html), [appearance](wearable-appearance.html), [transfers](inventory-transfers.html), [ground piles](ground-storage.html), [asset library](asset-library.html), [items](items.html), [ledger](status.html), [design library](documents.html).

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.
