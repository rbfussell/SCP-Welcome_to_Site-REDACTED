# Engine-rendered paperdoll mosaic

November 2, 2026. The [full-body paperdoll](42-full-body-paperdoll.md) now draws a coarse mosaic over an authored groin area on both front and back. Godot synthesizes opaque blocks from quantized base-image color samples. No mosaic is painted into any PNG. The manifest supplies normalized front/back areas and a block size in reference-canvas pixels; the ordinary body letterbox transform carries them through resizing.

## Clothing and physical state

Draw order is base body, mosaic, actual clothing in committed worn order, then foreground head. Underwear or trousers naturally cover the mosaic with their own alpha. Removing real clothing reveals it only after normal paid handling commits. Held, packed or foreign clothing cannot conceal the mosaic as an outfit layer. It is deliberately not a selectable item or an inventory instance, and supplies no warmth, mass, capacity, fit, protection, physical cover or removal restraint. Saves retain the real outfit; restoring it reconstructs this presentation without a new saved flag.

The renderer exposes a read-only visual-exposure query for native checks. It does not decide physical accessibility. Transparent/unrepresented garments do not imply privacy coverage; this is a bounded opaque-clothing fixture, not a general fabric or wardrobe solver.

## Requested mannequin: unresolved asset

The requested adult smooth, featureless mannequin is **not implemented**. The built-in image generator rejected its output with category `sexual`, despite the nonsexual mannequin/no-detail specification. No accepted asset was produced. The earlier painted tan undershirt/charcoal shorts reference therefore remains underneath the new engine mosaic. It is still labeled reference art, not real clothing. See [the exact attempt/provenance](../game/Assets/paperdoll-proof/FULL-BODY-PROVENANCE.md). This limitation must not be described as a finished Ken-doll body. Replacing that base still requires a suitable approved asset; the mosaic renderer is ready to sit above it.

## Verification

[The native underwear fixture](../game/Presentation/SpatialScene.PaperdollUnderwearSmoke.cs) exercises covered front/back regions, paused refusal, paid removal/source retention, newly exposed mosaic, non-target picking at a different aspect ratio, unchanged state and reconstruction after restore. It captures front/back after removal. See [actual runtime evidence](reviews/inventory-runtime-checks.md), [appearance contract](26-wearable-appearance-pipeline.md) and [wiki mechanic](../source_material/game-wiki/paperdoll-mosaic.html).

Item definitions, content identity, physical geometry and schema 14 are unchanged from the full-body kit. Matching full-body saves remain compatible. World actors still use baked artwork; this only affects **I → Sprites**.
