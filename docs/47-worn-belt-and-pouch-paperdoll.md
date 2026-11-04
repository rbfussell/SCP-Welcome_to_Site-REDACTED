# Worn waist belt and direct utility pouch

Implemented November 4, 2026. **I → Sprites** now displays an actual utility belt at its qualified normal waist pose, front and back. A utility pouch attached directly to that belt appears on the front at its committed local hex position and sixth-turn rotation. This extends the [full-body fixture](42-full-body-paperdoll.md), [physical belt](32-belt-and-attachment-prototype.md), [mount-point system](37-placement-extenders-and-mount-points.md) and [appearance contract](26-wearable-appearance-pipeline.md). See the [wiki mechanic](../source_material/game-wiki/worn-belt-and-pouch.html).

## Try it

Approach the supply crate and wear its utility belt through normal inventory controls. Take the utility pouch and attach it to an exposed belt point in the enlarged gear view. Switch to **Sprites**. Click visible belt/pouch pixels to select that exact item; double-click or choose Open to inspect its normal gear view. The pouch inspection establishes **belt → pouch** even when another garment was previously being inspected. This is exterior navigation, not a request to open its flap or reveal contents.

Double-click the pouch's flap in the middle gear view to unsnap it through ordinary time/AP. The right-hand contents view remains governed by normal access and closure state. Right-click uses ordinary actions, including Remove → hand. Paused physical work still refuses; opening the interface or looking at the belt does not pay for an action. The paperdoll does not add sprite fitting/drop targets in this slice; use the existing hex/gear tools for mounting and placement.

## View registration and provider coordinates

The [new attachment presentation helper](../game/Presentation/Inventory/InventoryPaperdollArt.Attachments.cs) keeps this logic separate from [atlas/mask drawing](../game/Presentation/Inventory/InventoryPaperdollArt.cs). The ordinary worn graph, committed dressing order, qualified default body pose and mount still select the parent belt. Its manifest binding supplies an `attachmentPlane` with a normalized origin and cell spacing in the 360×800 reference canvas. The first registration is origin `[0.5,0.518]`, 34 canvas pixels per hex step.

`attachments` entries map an attached definition and authored surface to a frame/anchor. The direct pouch has one registered part, front surface, anchor `[0.5,0.12]`. Its actual `AttachmentPose` supplies local Q/R and rotation. The renderer projects the real hex position into that provider plane and rotates the sprite exactly by sixth-turns. It never interprets folded storage width as a worn placement. Rendering, tint and inverse alpha picking share the same transforms, letterboxing and source alpha threshold. Transparent pouch gaps do not gain rectangular hit targets.

Only direct exterior children of a qualified worn provider enter this draw list. They are rendered immediately after that provider, before subsequently worn outer garments; the existing sprite alpha handles visual overlap. Packed contents, held/stored providers, foreign equipment and unqualified body poses never become outfit art. Omission reporting now includes own exterior descendants, without scanning compartments as clothing. The belt's back view keeps its identity while its front-mounted pouch does not appear through the body.

## Validated navigation and physical authority

The [browser](../game/Presentation/Inventory/InventoryBrowser.cs) can trace a directly selected own-worn exterior item through attachment edges to its worn root and construct the middle inspection trail. Every node uses ordinary inspect access, ownership and depth bounds. It does not search or open storage, change closures, create inventory parents or expose another actor's outfit. Existing current-provider trails retain their usual route.

A pending loaded-belt removal retains both worn images until the inventory transaction commits. After completion the held assembly is omitted from the outfit and retains its pouch and contents. Exact saved pending work reconstructs the same presentation from the physical graph. No native definition, physical mask, clearance, kit identity or save field changes; schema remains 14. Visual cutouts never grant physical access or release a restraint.

## Assets and bundle boundary

The belt atlas is a new built-in-imagegen stand-in copied unchanged into [its item bundle](../game/Assets/items/worn/accessories/belts/utility-belt/utility-belt.paperdoll-v1.png). [Exact prompt, source, hash and references](../game/Assets/items/worn/accessories/belts/utility-belt/provenance.md) are retained. [Bundle metadata](../game/Assets/items/worn/accessories/belts/utility-belt/item.art.json) records its current registration; it is not a new runtime bundle loader. The existing paperdoll manifest still loads it, now accepting relative PNG paths contained inside the project's Assets tree. Absolute paths and escapes outside that tree are rejected. Legacy atlas paths remain valid.

The pouch reuses its existing unmodified loose illustration as a bounded front exterior overlay. No second pouch image or source alpha modification is needed. Its flap remains visually static across physical open/closed state; menus and contents show actual state. The [asset standard](46-item-asset-library.md) remains the migration contract. This first bundle does not move old atlases or implement PNG mask loading.

The [item catalog](../source_material/game-wiki/items.html) now distinguishes direct wearable bindings from registered exterior attachments, exposes their registered frame metadata, and links byte-identical worn source sheets. Original and published worn PNGs participate in freshness hashes alongside loose art. Current native counts remain 19 item definitions and 30 catalog categories.

## Bounded scope and verification

Normal waist/default-pose belt and directly mounted front pouch are qualified. A crossed/diagonal belt, nested hook → pouch artwork, arbitrary provider chains, back-mounted pouch art, general garment deformation, closure animation and world outfit composition remain unqualified. Unsupported poses/chains remain explicitly omitted rather than borrowing a waist sprite. Their existing physical mechanics still work in the engineering views.

[Native belt checks](../game/Presentation/SpatialScene.PaperdollBeltSmoke.cs) cover actual identities, front/back, position change, all six committed rotations, alpha selection/resize, direct inspection from another garment, paused/paid flap actions, loaded removal/source retention, exact pending restore and omission of a non-waist fit. The parent paperdoll suite retains shoe masks, clothing, backpack and closure checks. Actual full regression results belong in [the inventory review](reviews/inventory-runtime-checks.md).

```sh
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/worn-belt.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/worn-belt-full.png
python3 -m unittest discover -s tests -p test_wiki_items.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```
