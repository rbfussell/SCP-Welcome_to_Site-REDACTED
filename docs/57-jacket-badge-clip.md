# Real jacket badge clip

Implemented November 6, 2026. The field coat now has a real left-chest clip for the existing personnel badge. Its painted white badge/red clip has been removed from a new versioned art source; the original atlas is preserved. A visible badge is now an actual child item, not a painted authorization cue. See [shirt/badge foundation](54-shirt-pockets-and-badge.md), [reader access](56-badge-door-access.md), [pocket contract](55-visible-pocket-contract.md), [mount providers](37-placement-extenders-and-mount-points.md) and the [wiki](../source_material/game-wiki/jacket-badge-clip.html). This is ordinary equipment design, not an SCP canon claim.

## One badge, two providers

`coat / badge-clip` is an exterior `badge-clip` interface at local `(0,2)` on the front, with exposed-access validation and a 100 g descendant limit. It has no loose storage, fastening or mutable compartment state. The coat retains its four independent real pockets. The same existing 20 g badge can move between shirt, jacket, direct hand and storage through normal paid transactions. No badge is spawned or cloned by equipping a garment.

The jacket can be zipped or open while the badge is attached. A front-worn pack can cover the badge: attachment retention remains valid, while manipulation and reader authorization are refused. Moving/removing the loaded jacket retains the badge once as a child, including during paid work and exact continuation. A jacket held in hand does not count as wearing its badge or holding the badge directly.

[The existing credential service](../src/SiteSimulation/Security/DoorCredentialService.cs) requires no jacket-specific exception. Exposed wearing authorizes the bounded reader; otherwise equip the badge itself in hand and press E for paid presentation. Issuer/tier/condition/optional identity and the provisional bearer default are unchanged. This adds neither credential issuance nor OSCP account authorization.

## Pockets and mounts on the same illustration

The enlarged gear view now supports both pocket contours and exterior mounts on one provider. Previously, the presence of any attachment support redirected all interactions to mounting; that would make the jacket pockets unusable.

- Double-click and context menus address the actual pocket hotspot or garment zipper. An attached badge is separately selectable/inspectable/draggable.
- A compatible attachment dropped near its exposed support uses the mount quote. An explicit mount refusal does not fall through into pocket storage.
- An ordinary item dropped on exposed pocket material uses that specific compartment's normal packing action, even near the clip. Unregistered/empty material is not a pocket.
- Opaque exterior badge pixels prevent a drop into the hidden pocket beneath them. Exposed pocket material remains usable; no automatic arrangement occurs from an invalid badge-covered drop.
- Mounted provider contour highlighting shares the exact projection used to draw and pick the sprite. It does not tint the entire jacket when only a pocket is hovered.

These are UI intent rules over existing validated inventory services. Grid drops remain exact, deliberate container drops keep scoped packing, and source retention, access, mass, physical cover, AP/time and commit-time validation remain authoritative. [Independent source views](48-independent-inventory-transfers.md), [ground staging](49-ground-inventory-and-piles.md) and [body inspection](40-body-inspection-and-removal.md) retain their normal behavior.

## Art and placement

The built-in `image_gen.imagegen` precisely edited the existing upper-body atlas to remove the two painted front badges and reconstruct pocket cloth. [The saved source and exact prompt/provenance](../game/Assets/items/worn/clothing/coats/coat/PROVENANCE.md) record input/output paths, hashes and copy policy. The new PNG is copied byte-for-byte with genuine alpha; no raster post-processing was used. Only jacket frame columns 2/3 and the coat loose/enlarged crop consume it. Other bodies/shirts retain their existing sources. [Bundle metadata](../game/Assets/items/worn/clothing/coats/coat/item.art.json) is authoring evidence; existing manifests still load runtime art.

The default jacket attachment plane uses origin `(0.50,0.223)`, 34-pixel cells. The enlarged illustration uses the existing 250-pixel mounted-provider profile, with anchor `(0.45,0.026)` and provider origin `(0.5,0.08)`. Placement is slightly inward on the left chest pocket so the normal back-pack harness does not obscure the badge. The first render placed it beneath a strap; final registration was corrected. New art changes no body fit, pocket polygon or physical storage size.

Both open/closed worn jacket states show the actual badge as a separate layer. The enlarged jacket still uses its closed reference illustration while physical state controls menus/labels; animated zipper/front states remain future work. World wardrobe sprites remain baked, and non-default coat poses/body variants remain outside this bounded reference-art qualification. [Worn-only alpha holes](45-worn-alpha-masks.md) are a separate mechanism; no hole was used to erase the painted card from the garment.

## Identity and evidence

The kit remains **20 definitions/40 instances**, with no new mutable region state or save field. The new immutable support changes kit identity: **start a fresh scene**. Matching continuation remains schema **15**.

[Domain checks](../tests/SpatialChecks/JacketBadgeChecks.cs) cover the separate clip, four pockets, paid mounting/transfers, cover, reader use, loaded removal, direct-hand distinction and exact continuation. [Native checks](../game/Presentation/SpatialScene.JacketBadgeSmoke.cs) exercise both body/enlarged clip drops, pocket menus/storage/tint, separate badge picking, open/zipped coats, pack cover, opaque-drop refusal and reader approach. Run `python3 tools/run_spatial_scene.py --jacket-badge-smoke PNG`; full `--inventory-smoke` includes this fixture. See [actual results](reviews/jacket-badge-runtime-checks.md); wiki generation is not gameplay verification.
