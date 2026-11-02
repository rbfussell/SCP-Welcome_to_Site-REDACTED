# Full-body paperdoll and base garments

Current follow-up: [engine-rendered mosaic](43-paperdoll-mosaic.md) adds front/back privacy blocks beneath real clothing. The requested featureless mannequin asset was rejected by imagegen; the painted reference below is retained.

Implemented November 2, 2026. This extends the [original upper-body proof](41-paperdoll-sprite-proof.md) into one complete front/back reference body with six supported wearable definitions: underwear, socks, trousers, shirt, work shoes and open/closed field coat. The [game-wiki mechanic](../source_material/game-wiki/full-body-paperdoll.html) links the implementation and evidence.

## Controls and scope

Open **I → Sprites**. Click a visible garment to select its actual instance, double-click to inspect it in the middle gear view, and right-click for existing actions. Zip/Unzip still uses normal paid jacket handling. Removing shoes reveals socks; removing socks reveals the reference feet. Moving clothing into hand or storage removes its worn sprite only when the physical transaction commits. Held/stored clothing is never an outfit layer.

The full hex diagnostics remain the place for body fitting, drag sources and attachments. This view adds no sprite-drop equipping or direct pocket segments. Packs, belts/attachments, suits, alternate bodies and non-default fitted poses remain unqualified and explicitly omitted. The world character still uses baked artwork; the current paperdoll does not recolor the existing loose-item illustrations or generate world animations.

The new body is a **modest development reference**, with a painted tan undershirt and charcoal shorts underneath real equipment layers. These base pixels are not inventory items, storage, insulation, armor or physical obstructions, and they cannot be selected or manipulated. Removing the actual underwear can therefore leave the clearly disclosed visual modesty base. A real cotton boxer-briefs layer is navy and has its own identity; the reference shorts are not evidence that it is still worn. The initial unclothed asset request was rejected by the image generator; the accepted clothed reference is recorded explicitly rather than presenting it as an anatomy-complete production body.

## Registered parts, alpha and body cutouts

[The appearance adapter](../game/Presentation/Inventory/InventoryPaperdollArt.cs) reads the same own worn graph, actual dressing order, default body poses and committed closures as before. [Manifest version 2](../game/Assets/paperdoll-proof/manifest.json) supports multiple images and authored parts per frame. Each part has front/back source rectangles and normalized destinations on one 360×800 reference canvas. Frame/binding references are validated from data rather than hardcoding this fixture's six bindings/eight frames; this remains presentation metadata, not a public mod loader. Back destinations can differ. Drawing and picking share these exact transforms and letterboxing.

The existing shirt/coat sheet is retained; new body and lower-clothing PNGs are registered alongside it. Socks and shoes each use two draw parts so their left/right feet align independently. Both parts retain **one item ID, parent, condition and mass**. This is visual registration, not extra inventory instances or a new pair-splitting mechanic. Physical occupancy and fit do not derive from image dimensions.

Hover/selection tint the existing sprite pixels; transparent margins and leg/garment gaps do not gain cell fills or rectangular targets. Higher garment pixels occlude lower tint. Nearest filtering and the existing fixture-only alpha > 0.1 cutoff remain shared by rendering and picking. The front head/neck and back hair foreground parts keep collars behind the head without painting a skin rectangle over the back collar.

The two long-sleeve bindings also declare **base-body cutout rectangles**. When a qualified shirt/coat is actually drawn, these suppress the reference arms/shoulder edges underneath the opaque sleeve silhouette, preventing differently proportioned stand-in skin from poking through. They do not tint or create a rectangle, affect worn garment alpha, change physical body/cover masks, or hide a real inventory item. Their partitioned drawing is an authored stand-in qualification, not a general solution for torn/transparent fabric, all anatomy or arbitrary garment fitting. Unsupported poses are omitted before these cutouts are chosen. General material and rig/pose fitting remain future work in the [appearance contract](26-wearable-appearance-pipeline.md).

## Real underwear and persistence

The native kit adds `underwear` / `avatar-underwear`: cotton boxer briefs, authored packed envelope **100×100×15 mm**, mass **80 g**, no compartments, and the existing underwear body mount. Base handling is 1000 ms / 2 AP plus ordinary source/destination access costs; actions use the normal quote, never a special art cost.

The symmetric `underwear` body-fit profile covers pelvis/upper-thigh rows 11–13 with linked front/back material. It is generated by [the existing fixture author](../tools/author_body_fit_fixture.py). Actual wearing order places it before trousers. Trousers cover it and block access/removal through them. Clearing the real outer garments exposes the same underwear item on both sides. Shape/contact fitting, layering, bulk, actor ownership, loaded descendants and normal time/AP remain shared inventory services.

Native counts are now **19 definitions, 39 instances and 9 fit profiles**. No mutable save field was added; outer schema remains **14**. The new immutable kit/body-fit content changes inventory identity: **start fresh for saves from the preceding kit**. No silent item injection or migration is added. Saves made with matching new content preserve pending dressing/removal exactly; appearance is derived after restore rather than saved as another outfit.

The native crate remains its real size. Loaded coats and trousers need adequate staging space; opening the sprite view does not make them fold or fit into a full crate. Underwear checks prepare valid helper-held outer gear and free a crate pack position for a paid trouser move. That is test setup, not a new player handoff command, automated undressing or enlarged native container.

## Verification and assets

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/fullbody-layers-final.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/fullbody-full.png
python3 tools/author_body_fit_fixture.py --check
python3 tools/check_gear_data.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

[Domain checks](../tests/SpatialChecks/UnderwearChecks.cs) cover identity, base layers, fit/surfaces, covered refusal, loaded trousers, paid source retention, mass, exact continuation and atomic missing-item rejection. The [main native check](../game/Presentation/SpatialScene.PaperdollSmoke.cs) covers the full outfit, both feet/front-back targets, alpha overlap, paid shoe/sock removal and bare feet. The [underwear native fixture](../game/Presentation/SpatialScene.PaperdollUnderwearSmoke.cs) checks the exposed navy layer, actual inspection, paid removal and non-interactive modesty base. Multi-item packing waits against its declared remaining simulation duration with a wall-clock timeout, not a fixed count of render frames. See [actual results](reviews/inventory-runtime-checks.md).

Both new PNGs are copied unchanged from built-in imagegen. [Full prompts, sources and hashes](../game/Assets/paperdoll-proof/FULL-BODY-PROVENANCE.md) preserve provenance. Source rectangles, part registration and cutouts are authored presentation metadata, not destructive image edits or a generalized procedural clothing generator.
