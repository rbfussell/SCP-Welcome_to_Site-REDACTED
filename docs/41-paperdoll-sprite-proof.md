# Upper-body paperdoll sprite qualification

Historical October 31 qualification. The current native view is superseded by [full-body paperdoll and base garments](42-full-body-paperdoll.md), including its new content identity, paired visual parts and explicit modesty reference. The implementation and limits below describe the earlier slice.

Implemented October 31, 2026. This is the first bounded 2D art/compositing experiment for [wearable appearance](26-wearable-appearance-pipeline.md), alongside the existing [body inspection/removal](40-body-inspection-and-removal.md). See the [game-wiki mechanic](../source_material/game-wiki/paperdoll-sprite-proof.html) for connected source and evidence.

## Try the proof

Open **I → Sprites** beside **Body fit · hex view**. The normal equipment list and full hex diagnostics remain available. This optional view shows one reference-based upper body, its worn work shirt and field coat. **Front / Back · flip** selects separately authored artwork. It does not move gear, mirror physical coordinates or change actor facing.

Click a visible garment to select its actual instance, double-click to request the normal middle gear inspection, or right-click for the existing context actions. Visible does not mean removable: the starting shirt remains restrained by outer equipment. The jacket button reads the actual Zip/Unzip state and submits ordinary paid garment handling. The sprite changes when handling commits, rather than on button press. Manual pause supplies no free fastening; normal time, AP, access and pending-work rules apply. This interface does not automatically pause the game.

This proof has no body-drop or drag source. Use the full hex view for fitting, worn-item dragging and attachments. It explicitly lists worn items without qualified artwork as **omitted**. The pack, trousers, shoes, socks, suits, belt/attachments and non-default garment poses are not silently represented by the shirt/jacket. This is an upper-body qualification, not a complete outfit view or reliable way to determine access through omitted equipment. Reopening inventory resets the transient view to front.

## Registration, layering and picking

[The appearance adapter](../game/Presentation/Inventory/InventoryPaperdollArt.cs) reads the existing own worn/held query, accepts only actual compatible worn mounts, and filters to the `avatar` actor and `human-nine-shoulder` body. Held garments, stored items, foreign outfits and packed contents supply no layers. Shirt and coat bindings require their authored default body pose; unsupported fittings are reported as omitted. Worn order controls garment drawing; a closed outer coat covers the inner shirt, while its open front reveals the shirt through actual alpha.

The [atlas manifest](../game/Assets/paperdoll-proof/manifest.json) registers front/back source rectangles, a common display canvas and a head/neck foreground cutout. The generated sheet required authored rectangle/anchor registration, not equal automatic atlas slicing. The foreground skin part occludes the far collar; otherwise a whole-jacket draw would put that collar across the face. Picking uses that same head occlusion and never treats the head as an item. Only the front needs foreground neck skin; the back cutout ends at the hair to retain the collar over the neck.

Drawing and picking share the exact letterbox/source transform. Hover and selection modulate the existing sprite pixels, then higher layers cover that tint. No hex fill, rectangular hit box or polygon outline is drawn in transparent margins or garment holes. This fixture uses nearest filtering and an experimental **alpha > 0.1** cutoff for both visible fragments and picking, discarding low-alpha generation fringe. This is a local art qualification threshold, not a new production material rule. It does not change physical fit, access, removal or pocket masks.

The atlas is a new generated stand-in inspired by the existing player reference. It is not a generalized body/wardrobe generator or a finished likeness pipeline. Its burgundy shirt/charcoal jacket do not yet recolor the loose inventory illustrations or replace the world's baked outfit. Multiple body identities, procedural fit, recoloring, all world facings/actions, full-body outfits, split attachment occlusion and general transparent materials remain future work. The proposed rig/baking and surface-mapping routes remain proposals.

## Assets and authority

The repository-local [asset provenance and exact prompts](../game/Assets/paperdoll-proof/README.md) identify the built-in imagegen output, reference, revision and immutable PNG hash. No script edits the generated image's pixels. Registration and alpha cutoff are presentation operations. Appearance metadata is not inventory content and cannot resize a pocket or grant a free clothing change.

No item definitions, physical state fields, save schema or content identity changed. Inventory stays schema **14**. Paid fastening continues through the same saved request and authoritative commitment validation; art is recomputed from restored equipment rather than saved as another outfit state.

## Verification

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/paperdoll.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/paperdoll-final.png
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

[The native qualification](../game/Presentation/SpatialScene.PaperdollSmoke.cs) checks actual worn IDs/order, open/closed alpha overlap, transparent margins, foreground head, resizing, sprite selection/inspection, unchanged blocked removal, paused refusal, source state during paid fastening, exact pending restore, back artwork and reopen reset. It participates in the full inventory suite; source checks alone do not establish visual quality. See [recorded results](reviews/inventory-runtime-checks.md) for actual runs and limitations.
