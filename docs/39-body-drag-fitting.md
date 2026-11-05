# Dragging stored clothing onto the body

Implemented October 30, 2026. This adds direct fitting drops to the [symmetric body fixture](38-body-fit-cleanup.md), [three inventory views](27-three-view-inventory-and-body-fit.md) and existing paid equipment commands. The [game-wiki article](../source_material/game-wiki/body-drag-fitting.html) connects mechanics, source and evidence. This is still engineering geometry, not final anatomical artwork.

## Controls

Final presentation requirement: [character-matched clothing and silhouette highlights](26-wearable-appearance-pipeline.md#confirmed-paperdoll-presentation-october-30) will hide the fit grid in normal use and clip feedback to worn garment cutouts. The cells and contacts in this slice are engineering diagnostics, with unchanged physical rules.

1. Open **I**, inspect an accessible container with wearable gear, and open its compartment. The supply crate holds belts; use its contents scrollbars to reach them.
2. Switch the left view to **Body fit · hex view**. Gear/contents views remain available.
3. Drag the actual item from its visible grid onto a body hex. That hex is the authored fitting anchor. The complete worn mask/contact preview appears in its exact proposed position. Green permits fitting; red explains a geometric or physical refusal. Blank margins and spaces between limbs do not snap to another anchor.
4. During a drag, **R over the body** cycles six orientations. **R over square storage** toggles two, even while body mode is visible. Exterior support retains six attachment turns. These independent drafts do not alter the stored item or the click/Apply fit draft.
5. Release on a valid fit to begin normal timed handling and source-dependent combat AP. Invalid release leaves the item in its exact source position. It never searches another position, rotates, repacks, swaps clothing, removes blockers or opens closures.

The panel reports the dragged item, anchor, angle, time/AP and adjustable strap span/range. The packed ghost is suppressed over the body because that surface draws the exact worn footprint. Apply is disabled during dragging; release submits the drag. Existing click/Default/Apply fitting for own worn/held gear remains available outside a drag.

Manual pause allows inspection/preview but rejects physical fitting on release. **Esc** closes the interface and cancels the drag without securing gear or submitting delayed work. Leaving the body clears its drop overlay. Once handling has started, hiding the interface follows ordinary paid continuation rules.

## Authority and knowledge

`PreviewBodyFitDrop` addresses one specific item and requires current inspection access. It can preview clothing in an open, reachable container; closed/inaccessible ancestors, another employee's gear, unknown items and non-wearables expose no geometry. It does not enumerate stored clothing or broaden `BodyFitItems`. The selector still lists only own worn/held equipment.

Both preview entry points share one request builder. An existing front-worn pack keeps its mount when refitted; incoming stored clothing uses its authored default mount. This slice adds no automatic front/back choice or anatomical default-mount selection. Named mounts, family/bulk fit, boundary contacts, cover, outermost gear, restraints, overlay bands, occupied support contacts, shared clearance and recursive loads still validate the whole proposed graph.

The source stays in its container until paid completion. The request carries the exact previewed pose through the existing `Move` transaction; cost includes the actual source access path. Start and commitment revalidate access and graph constraints. Access lost after preview/start can reject or abort the move rather than transfer through a closed ancestor. Tactical work spends normal AP once.

No immutable inventory content, save schema or mutable item field changed. Schema stays **14**; saves matching the preceding body-cleanup content remain compatible. Drag/draft UI state is transient. Accepted paid work saves and resumes through the existing inventory codec.

## Source and verification

| Source | Responsibility |
|---|---|
| [InventoryBodyFit](../src/SiteSimulation/Inventory/Equipment/BodyFit/InventoryBodyFit.cs) | Explicit access-bounded drop query and shared ordinary request builder |
| [InventoryBodyFitSurface.Drop](../game/Presentation/Inventory/InventoryBodyFitSurface.Drop.cs) | Shared click/drop picking, read-only preview and release submission |
| [InventoryBodyFitPanel](../game/Presentation/Inventory/InventoryBodyFitPanel.cs) | Mask/contact feedback, drop status, selector and paid-result selection |
| [InventoryPanel](../game/Presentation/Inventory/InventoryPanel.cs) | Drag lifecycle and separate body/storage/attachment rotation contexts |
| [InventoryGrid](../game/Presentation/Inventory/InventoryGrid.cs) | Existing accessible drag sources and square placement; duplicate-ghost suppression |

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --body-drop-smoke work/preview/spatial-scene/body-drop.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/body-drop-full.png
python3 tools/author_body_fit_fixture.py --check
python3 tools/check_gear_data.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

[BodyDropChecks](../tests/SpatialChecks/BodyDropChecks.cs) exercise explicit visible sources, unchanged selector bounds, exact requests/costs, invalid geometry, worn-pack interference, invalid hand targeting, pause, source retention, pending continuation, ancestor access loss, front-mount preservation and tactical AP. [Native checks](../game/Presentation/SpatialScene.BodyDropSmoke.cs) use real grid drags, anchor hover/release, R, Esc and reopening. Valid/invalid/blank targets, rotation isolation, paused refusal, source retention and saved pending work are checked. The focused launcher mode runs this fixture alone; full inventory mode includes it alongside existing suites. See [recorded results](reviews/inventory-runtime-checks.md) for actual evidence.

The [body-inspection follow-up](40-body-inspection-and-removal.md) now adds exposed front/back inspection and worn-item drag sources. Clicking composited garments, final sprite front/back views, body/provider target arbitration and world outfit composition remain future work. Existing [gear-view mount drops](37-placement-extenders-and-mount-points.md) and container-target packing retain their interactions. No SCP canon or anomaly behavior changed.

Current November 4 follow-up: [sprite equipment drops](51-sprite-equipment-drops.md) add free body-hex fitting, exposed provider targets and worn-sprite drag sources. Earlier sprite-drop-as-future statements describe the earlier slice. Normal physical checks and schema-15 paid continuation remain authoritative; arbitrary fitted artwork remains unqualified.
