# Engine-rendered paperdoll mosaic

November 2, 2026. The [full-body paperdoll](42-full-body-paperdoll.md) now draws a coarse mosaic over an authored groin area on both front and back. Godot synthesizes opaque blocks from quantized base-image color samples. No mosaic is painted into any PNG. The manifest supplies normalized front/back areas and a block size in reference-canvas pixels; the ordinary body letterbox transform carries them through resizing.

## Clothing and physical state

Draw order is base body, mosaic, actual clothing in committed worn order, then foreground head. Underwear or trousers naturally cover the mosaic with their own alpha. Removing real clothing reveals it only after normal paid handling commits. Held, packed or foreign clothing cannot conceal the mosaic as an outfit layer. It is deliberately not a selectable item or an inventory instance, and supplies no warmth, mass, capacity, fit, protection, physical cover or removal restraint. Saves retain the real outfit; restoring it reconstructs this presentation without a new saved flag.

The renderer exposes a read-only visual-exposure query for native checks. It does not decide physical accessibility. Transparent/unrepresented garments do not imply privacy coverage; this is a bounded opaque-clothing fixture, not a general fabric or wardrobe solver.

## Current reference and earlier mannequin request

The current base is **shirtless with close-fitting matte tan fabric briefs**, registered as `full-body-v2.png`. It replaces the painted undershirt/loose charcoal shorts while retaining front/back positions and the existing source/destination registration. Tan briefs remain opaque visual reference clothing, not real equipment. The original PNG is preserved. The engine mosaic is unchanged and is still drawn beneath actual clothing.

The earlier featureless mannequin request was rejected by built-in imagegen (`sexual` output category), producing no accepted asset. That request is not implemented; the user-selected tan-briefs alternative is now the accepted reference. [Exact edit prompts, selected sources and hashes](../game/Assets/paperdoll-proof/FULL-BODY-PROVENANCE.md) preserve both history and the current asset.

## Verification

[The native underwear fixture](../game/Presentation/SpatialScene.PaperdollUnderwearSmoke.cs) exercises covered front/back regions, paused refusal, paid removal/source retention, newly exposed mosaic, non-target picking at a different aspect ratio, unchanged state and reconstruction after restore. It captures front/back after removal. See [actual runtime evidence](reviews/inventory-runtime-checks.md), [appearance contract](26-wearable-appearance-pipeline.md) and [wiki mechanic](../source_material/game-wiki/paperdoll-mosaic.html).

Item definitions, content identity, physical geometry and schema 14 are unchanged from the full-body kit. Matching full-body saves remain compatible. World actors still use baked artwork; this only affects **I → Sprites**.
