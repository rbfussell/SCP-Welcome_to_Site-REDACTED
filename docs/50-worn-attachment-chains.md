# Worn attachment chains: belt → hook → pouch

Implemented November 4, 2026. **I → Sprites** extends the [waist belt/direct pouch proof](47-worn-belt-and-pouch-paperdoll.md) to the existing [hook/provider chain](37-placement-extenders-and-mount-points.md). The normal reference-body waist belt, its utility hook and the hook's utility pouch appear as distinct physical items. See the [wiki mechanic](../source_material/game-wiki/worn-attachment-chains.html), [appearance contract](26-wearable-appearance-pipeline.md) and [full-body proof](42-full-body-paperdoll.md).

## Try it

Wear the utility belt through normal handling. In the enlarged gear view mount the hook on belt webbing, then the pouch on the hook's lower point. [Independent source/destination trails](48-independent-inventory-transfers.md) allow these transfers directly from accessible storage without borrowing the hand. Switch to **Sprites**. Click hook or pouch pixels to select that item; double-click to inspect it. Selecting the pouch from another garment establishes **belt → hook → pouch**. Opening the pouch's contents remains a separate paid fastening action.

Move/rotate the loaded hook in the enlarged gear view. The paperdoll follows the exact committed frame of both items. A front chain stays off the rear view. Drop the belt onto [current-tile ground](49-ground-inventory-and-piles.md) and pick it up again: its hook, pouch and contents retain their parents and local placements. Pending work retains the worn assembly until commitment.

## One composed frame

[Attachment drawing](../game/Presentation/Inventory/InventoryPaperdollArt.Attachments.cs) traverses only actual attachment-region children, with the normal hierarchy depth bound. Each child frame uses [InventoryAttachmentFrames.Compose](../src/SiteSimulation/Inventory/Equipment/InventoryAttachmentFrames.cs), exactly as physical provider geometry and the enlarged gear view do. Positions and sixth-turn rotations compose from parent to child in the qualified worn root's plane. Explicit surfaces override the inherited plane; `mounted` inherits it. No extra item parent, fit exception, rotation offset or capacity is introduced.

A registered ancestor is required before rendering its descendants, preventing a floating pouch when provider artwork is unsupported. Parent art draws before children; later worn garments retain existing precedence. Drawing, tint and inverse alpha picking use the same frame and source pixels. Packed contents never enter this traversal or become outfit art. Unsupported own exterior gear remains listed in omission feedback.

The existing manifest adds a front utility-hook attachment frame using the unchanged `utility-hook.png` crop `(357,225,422,1091)`, anchor `(0.36,0.1)` and 68 reference-canvas pixels of height. This matches the existing gear-view 88-pixel height at its 44-pixel hex step, scaled to the paperdoll's 34-pixel step. The existing pouch art/registration is unchanged. The [item catalog](../source_material/game-wiki/items.html) automatically exposes both exterior bindings and original source artwork. There is no new generated image or destructive asset edit.

## Authority, qualification and limits

Selection uses real item IDs and existing validated inspection. Normal access, coverage, provider clearance, load, time/AP and paid source retention remain authoritative. Exact pending saves reconstruct the same visible assembly; no save fields, native definition/kit identity or physical dimensions change. Current saves remain **schema 15**, with matching schema-14 migration as documented in the ground feature.

[Native chain checks](../game/Presentation/SpatialScene.PaperdollHookSmoke.cs) exercise all six hook orientations, composed nested position/rotation, distinct alpha selection/inspection, resizing, rear exclusion, loaded paid ground drop/pickup, exact pending restore, packed-content exclusion and unsupported root omission. They run inside the normal paperdoll and full inventory suites. Actual run results are recorded in [runtime evidence](reviews/inventory-runtime-checks.md).

Only the normal waist root and registered front hook/pouch chain are qualified. Back attachment artwork, chest/crossed-belt art, arbitrary provider art, swinging/depth, sprite-drop equipping, closure animation and world outfit composition remain future work. The traversal supports bounded registered descendants; this is not a claim that every possible equipment chain has suitable artwork.

```sh
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/worn-hook.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/worn-hook-full.png
python3 -m unittest discover -s tests -p test_wiki_items.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```
