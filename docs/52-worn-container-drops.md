# Dropping into worn containers

Implemented November 5, 2026. **I → Sprites** accepts item drops on registered backpack-bag and utility-pouch material. These enter their real compartments through the existing [scoped packing contract](29-gear-data-standard.md), extending [sprite equipment drops](51-sprite-equipment-drops.md), [worn backpacks](44-worn-backpack-paperdoll.md) and [attachment chains](50-worn-attachment-chains.md). See the [wiki mechanic](../source_material/game-wiki/worn-container-drops.html).

## Visible material, explicit interior

`PaperdollPart.dropRegion` names one existing ordinary interior. The native bag part links to `main`; the utility pouch links to `inside`. Harness-only parts have no registration. Back-mounted packs therefore offer storage on the rear bag, while front-mounted packs offer it on the front bag. Their opposite-side straps remain selection/inspection targets without becoming storage. Transparent holes, the base body and unregistered garment pockets do not invent interiors.

Only the topmost alpha-picked item and its actually hit registered part offer a target. Drawing and picking share the same source crop, worn masks, side and composed transform. Green/red tint is applied during the ordinary outfit draw order, preserving occlusion by later layers. It does not paint a hidden bag on top of clothing or expose packed contents. Registration affects UI intent only; capacity, reach, coverage and closures remain physical facts.

## Intent and paid work

The first-version UI arbitration is **exact mount point → registered container → free body fitting**. This preserves existing mount work and direct-container packing semantics. A wearable dropped on bag material requests storage there; a refused container drop never becomes an unintended equip/refit action. This arbitration was proposed to the user as an optional preference question; unless subsequently confirmed, it remains the implementation's provisional UI choice, not a permanent design constraint. Free body fitting remains available away from registered container material and in the diagnostic body view.

The target shows its item/compartment name and the ordinary packing quote. The compartment must already be physically accessible/open. A closed bag refuses without opening, searching another pocket or revealing contents. Full/incompatible interiors, self/ancestor insertion and unavailable sources use normal service refusals. No successful-looking fallback to body fitting or another compartment is attempted.

Release recomputes the target and invokes existing AutoPlace. It uses free placement first, then scoped rearrangement only if needed and feasible, with normal time/AP for the paid plan. Items retain their exact source until commitment. The target does not change the middle/right inspection trails, so an open source grid remains available while transferring into the worn pouch. It does not require a spare hand.

R over this target leaves body and mount rotation drafts unchanged; the packing service chooses a legal storage orientation. Precise square-grid drops retain their existing manual placement/rotation and never trigger implicit rearrangement. Paused release, cancellation, closing/moving a required ancestor and changed access use ordinary handling rules. Esc still dismisses the interface without securing physical containers.

## Source, persistence and scope

[Storage target projection](../game/Presentation/Inventory/InventoryPaperdollArt.Storage.cs) shares per-part alpha with the outfit renderer. [The sprite input surface](../game/Presentation/Inventory/InventoryPaperdollSurface.Drop.cs) reuses `PreviewContainerDrop` / `StoreInContainer`; no new packing algorithm or simulation-domain behavior is added. Optional part metadata and transient UI previews do not change native item definitions, source PNGs, immutable kit identity or schema **15**. Matching paid work saves/resumes through existing inventory persistence.

[Native checks](../game/Presentation/SpatialScene.WornStorageSmoke.cs) cover bag-only material versus straps, closed/paused/full/self refusals, exact paid continuation/source retention, retained source views, pouch transfer, front/back mounting and refusal without fitting fallback. They run alone with `--worn-storage-smoke` and inside paperdoll/full inventory regressions. Actual results and warnings belong in [runtime evidence](reviews/inventory-runtime-checks.md).

Direct body pocket segmentation, on-sprite closure controls/animation, arbitrary-body registration, item/compartment selectors, new packing rules and world clothing composition remain future work. The enlarged gear view still supplies explicit main/side pocket choice and physical closure controls.

```sh
python3 tools/run_spatial_scene.py --worn-storage-smoke work/preview/spatial-scene/worn-storage.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/worn-storage-full.png
python3 -m unittest discover -s tests -p test_wiki_items.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

November 5 pocket follow-up: [paperdoll chest-pocket segments](53-paperdoll-pocket-segments.md) add one authored front contour in both jacket states, with normal paid opening, pocket-specific fastening and existing packing. Earlier body-pocket-as-future statements are superseded for this one segment; other pockets and animation remain unqualified.

Current trouser extension: [paperdoll pocket segments](53-paperdoll-pocket-segments.md#trouser-extension) also register all four existing front hip/cargo compartments. Snug/unsealable hips and paid buttoned cargo retain ordinary coverage; the zipped jacket blocks hips while leaving lower cargo accessible. This supersedes earlier chest-only presentation limits; other unregistered pockets and animation remain future work.
