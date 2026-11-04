# Worn-only alpha masks

November 4, 2026. The [full-body paperdoll](42-full-body-paperdoll.md) supports authored visual holes in a worn sprite without cutting the original PNG. The first fixture removes the empty shaft/back-rim pixels of both work shoes on front and back views, allowing the underlying trousers, socks or reference leg to show through. This is presentation, not SCP canon or a new physical fit rule. See the [wiki mechanic](../source_material/game-wiki/worn-alpha-masks.html).

## Loose object versus committed worn appearance

The intact source artwork remains available for loose/drag representation. Existing packing grids and drag ghosts still use the independent `InventoryArt` illustrations, with no worn mask applied. The fixture does not yet unify those illustrations with the paperdoll atlas, add sprite-view drag/equip controls or produce world animation layers. Reusing a compatible source for both contexts is supported by deriving the worn mask from an unchanged source, rather than baking a damaged inventory illustration.

The paperdoll draws only committed worn items. Pending removal retains the masked worn shoe until paid handling completes; afterward its loose representation remains intact. Moving an item, fastening gear, access permissions, AP/time, physical fit, condition, mass and saved identity continue to come from normal inventory services. No mask is derived from inventory-grid occupancy or allowed to change it.

## Authored metadata and rendering

Each `PaperdollPart` optionally defines `frontWornCutouts` and `backWornCutouts`: arrays of closed polygons expressed as normalized coordinates within that side's original source rectangle. Omitted/empty lists preserve the complete original alpha. Several cutouts combine as a union. Each list has at most eight polygons, with 3–32 distinct finite, normalized vertices, nonzero area and triangulatable geometry; invalid geometry fails the manifest load. The base-body frame cannot use garment cutouts. These are bounded fixture metadata, not a public mod contract.

[PaperdollWornMask](../game/Presentation/Inventory/PaperdollWornMask.cs) clears alpha at pixel centers on a private cropped copy. It preserves RGB and surviving source alpha. [InventoryPaperdollArt](../game/Presentation/Inventory/InventoryPaperdollArt.cs) caches those small images/textures per part and side until disposal. Source PNGs, full source textures and the loose-art cache remain unchanged. A mask is authored for its registered view; there is no automatic visual segmentation, hidden-surface reconstruction, cloth simulation or feathering requirement in this slice.

Rendering, hover/selection tint, alpha picking and visual exposure queries share the same derived worn pixels and normal letterbox transform. Holes do not retain invisible shoe hit targets. Existing foreground-head, body cutout and dressing-order behavior remains. These cutouts are distinct from base-body cutouts, physical access coverage and removal restraints. A visible sock through an artistic hole does not grant access through a physically blocking shoe.

## Verification

The [native mask fixture](../game/Presentation/SpatialScene.PaperdollMaskSmoke.cs), invoked by the existing paperdoll suite, checks front/back on both feet, retained shoe pixels, unmodified loose source, exposed underlying selection, alternate aspect ratio, invalid masks, source RGB/alpha isolation and unchanged saved simulation state. The parent suite checks paid shoe/sock removal and continuation. Actual runs and any limits belong in the [inventory review](reviews/inventory-runtime-checks.md).

```sh
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/worn-mask.png
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

Related: [appearance contract](26-wearable-appearance-pipeline.md), [backpack presentation](44-worn-backpack-paperdoll.md), [body fit](38-body-fit-cleanup.md), [inspection and removal](40-body-inspection-and-removal.md).
