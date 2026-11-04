# Worn backpack and harness paperdoll

Implemented November 4, 2026. **I → Sprites** now displays the actual worn field backpack. A back-mounted pack shows its harness in front and a bag over the rear shoulder harness behind; front-mounted equipment moves the bag to the front over that same harness. The bag no longer appears attached directly to the coat without straps. Two packs retain two item IDs and inventories. This extends the [full-body fixture](42-full-body-paperdoll.md); the [wiki mechanic](../source_material/game-wiki/worn-backpack.html) cross-links source and evidence.

## Selection and existing segmented storage

Click visible bag or strap pixels to select that actual pack. Double-click opens its existing enlarged gear illustration in the middle; right-click offers the ordinary context actions. Only the actual sprite alpha is a target. Gaps between harness straps select exposed clothing beneath them rather than a rectangular pack region. The foreground head remains above equipment.

The enlarged gear view retains its authored main and side pocket segments. Double-click a segment to inspect/open that compartment on the right. Inspecting the backpack alone does not reveal packed contents or open lids. Zip/Unzip and Buckle/Unbuckle retain physical state, per-fastening preferences, pause refusal and normal time/AP. No new pockets are inferred from decorative artwork. Direct pocket targeting on the small body illustration is not part of this slice.

## Mount-aware presentation metadata

`PaperdollBinding.MountColumns` optionally maps an actual root `MountSlot` to a qualified frame. The field backpack maps `back` to frame 8 and `front` to 9. Each draws the harness on both sides at the same authored shoulder scale, then draws the bag over it only on its mounted side. `PaperdollPart.Side` optionally selects `front` or `back`; omitted means both. Rendering and alpha picking share the same part filter. The single base-body part must remain on both sides. Existing sources/destinations and the normal front harness are retained. Missing mount variants are omitted; they do not silently borrow an incompatible view. Existing default-pose qualification remains in force. This remains a bounded presentation manifest, not a public mod format.

The [appearance adapter](../game/Presentation/Inventory/InventoryPaperdollArt.cs) filters actual own worn items and orders them by committed dressing order. Foreign, stored and held packs are excluded. Both sides and all visual parts keep one physical item, parent, contents and recursive mass. Paid removal leaves the pack drawn until completion; moving it carries the loaded assembly normally. Pack/strap art does not add physical coverage, loosen restraints or grant access. Transparent visual regions are separate from body-fit and access masks.

The [new atlas](../game/Assets/paperdoll-proof/backpack-v1.png) is copied unchanged from built-in imagegen. [Exact prompt, references, source identity, hash and registration](../game/Assets/paperdoll-proof/BACKPACK-PROVENANCE.md) are preserved. The enlarged gear illustration is unchanged. Worn zip/flap animation, general cloth deformation, arbitrary fitted poses, belt/attachment body art and world outfit composition remain future work. The drawn bag is static across lid states; the existing menus/contents report physical closures.

## Persistence and checks

No simulation definition, physical fit geometry or saved field changes. Inventory content identity, 19 definitions/39 instances/9 fit profiles and schema 14 remain unchanged from the full-body kit. Matching saves reconstruct mount-aware appearance and exact paid continuation.

[The backpack native fixture](../game/Presentation/SpatialScene.PaperdollBackpackSmoke.cs) exercises front straps/rear bag, actual identity and independent segmented contents, paused/paid closures, loaded removal, exact pending restore, held-pack omission, front mounting and two independent packs. The original [paperdoll checks](../game/Presentation/SpatialScene.PaperdollSmoke.cs) now find visible clothing outside the newly rendered pack alpha; they retain their original selection/closure assertions. See [actual results](reviews/inventory-runtime-checks.md).

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --paperdoll-smoke work/preview/spatial-scene/worn-pack.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/worn-pack-full.png
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

Current footwear follow-up: [worn-only alpha masks](45-worn-alpha-masks.md) add source-preserving per-part/view holes for shoes, with shared visual picking and unchanged loose/drag art and physical rules.
