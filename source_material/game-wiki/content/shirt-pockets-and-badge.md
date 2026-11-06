# Shirt pockets and detachable badge

**Implemented November 5:** two buttoned shirt chest pockets and a separate badge attached to an exposed clip above the wearer's left pocket. **Current reader follow-up:** [badge-controlled access](badge-door-access.html) accepts an exposed worn badge automatically during realtime approach; otherwise equip it in hand and present with E. A bounded reader/credential service now exists; full facility identity/credential administration remains future work. This is game equipment design, not an SCP canon claim. See the [current guide](doc-docs-54-shirt-pockets-and-badge-md.html) and [actual evidence](doc-docs-reviews-inventory-runtime-checks-md.html).

## Physical items and access

The shirt adds `chest-left` and `chest-right`, each a provisional 75×100 mm opening/grid, 20 mm deep and 300 g maximum, initially buttoned. Left means wearer-left/viewer-right. [Pocket segments](paperdoll-pocket-segments.html) link actual contours to normal paid opening, independent fastening menus and scoped storage. Zipped jackets block both physical mouths; opening restores physical access but opaque jacket side panels can still hide small-body targets. The enlarged shirt view remains available.

The clip is an [attachment support](mount-points.html), not a pocket. `personnel-badge` is a separate 20 g item, initially inside the spare backpack main compartment. Moving the shirt carries attached badge and pocket contents once each. [Independent transfers](inventory-transfers.html), [ground staging](ground-storage.html), [body inspection](body-inspection.html) and normal time/AP still apply.

## Covered retention versus manipulation

[Attachment validation](file-src-sitesimulation-inventory-equipment-inventoryattachments-cs.html) adds optional `AttachmentProfile.CanBeCovered=false`; the badge sets true so an outer jacket may cover it without invalidating the graph. [Access traversal](file-src-sitesimulation-inventory-access-inventoryaccessservice-cs.html) honors optional `AttachmentSurface.RequiresExposedAccess=false`; the clip sets true, requiring its offered contact exposed for source/destination/ancestor access. Covered badges cannot be removed or attached through clothing. Neither flag waives normal interface, mass, collision, ancestry or paid commitment checks. Existing hooks/pouches retain their previous rules.

[Domain checks](check-shirtbadgechecks.html) qualify both pockets, limits, coverage, paid badge moves, covered retention/access and exact continuation. [Native checks](file-game-presentation-spatialscene-paperdollshirtsmoke-cs.html) qualify real sprite input/menus, storage, badge clip drops, own badge picking and jacket occlusion. Focused `--shirt-pocket-smoke` and pocket/full inventory modes run the fixture.

## Presentation and identity

The [asset library](asset-library.html) and [item catalog](items.html) expose a new two-pocket burgundy shirt sheet and separate badge sprite, copied from built-in imagegen with unchanged alpha/source bytes. The guide links exact prompts/hashes. The original shirt/jacket atlas remains preserved. Gear attachment metadata optionally names normalized `providerOrigin`, shared by drawing and picking; default remains centre. [Full-body presentation](full-body-paperdoll.html) uses a separate attachment plane and actual dressing order. World outfits remain baked.

The native kit now has **20 definitions/40 instances** and two extra pocket states. Immutable content identity changes: use a fresh scene for this kit; matching continuation remains schema **15**, with no new mutable field. [Persistence](system-persistence.html) rejects mismatched old kits atomically.

## Door-access boundary

The [bounded local reader](badge-door-access.html) now checks issuer, clearance, optional assigned actor, condition, exposed wearing/hand placement and authored reader power. Manual presentation costs 500 local ms/2 AP. Production issuance, revocation, expiration, network identity and OSCP account authorization remain future work. Visible wearing is required for automatic entry, not sufficient permission for every door. Covered/stored badges and painted badge pixels never grant automatic authorization. The [jacket follow-up](jacket-badge-clip.html) removes the painted card from a new art version and supplies a real exterior clip for this same badge. Original source art is preserved. [OSCP](system-oscp.html) and facility/world permissions must eventually share authoritative credential services without omniscience.

Related: [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [engine](engine.html), [ledger](status.html), [design library](documents.html), [appearance](wearable-appearance.html), [worn containers](worn-container-drops.html), [sprite drops](sprite-equipment-drops.html).

[Current visible-pocket contract](visible-pockets.html): depicted pockets require real storage; multiple views share one inventory.

[Real jacket badge clip](jacket-badge-clip.html) now combines four usable pockets with exterior mounting, badge-free source art and the existing reader checks.
