# Front/back body inspection and worn-item drag sources

Art follow-up: [full-body sprite qualification](42-full-body-paperdoll.md) adds a separate optional alpha-based view; this guide still describes the complete diagnostic hex interaction.

Implemented October 30, 2026. This completes the first body-to-storage interaction alongside [stored-to-body fitting](39-body-drag-fitting.md), [body geometry](38-body-fit-cleanup.md) and the [three-view browser](27-three-view-inventory-and-body-fit.md). The [game-wiki guide](../source_material/game-wiki/body-inspection.html) connects source, mechanics and evidence.

## Controls

Open **I → Body fit · hex view**, then enable **Inspect worn**. This is an engineering interaction mode; its colored hex material is not the final [sprite paperdoll](26-wearable-appearance-pipeline.md).

- **Click exposed material:** select that actual worn item. The target is the uppermost exposed item at that hex on the viewed surface.
- **Double-click:** inspect that item in the existing middle gear view. Compartments still require deliberate opening in that view.
- **Right-click:** show its existing context menu, including normal physical actions and fastening preferences.
- **Drag:** move the picked item into a visible storage grid, container target or other supported destination through ordinary handling. A restrained source cannot start a drag merely because some material is visible.
- **Front / Back · flip:** change the inspection side. Back mirrors the screen horizontally while retaining anatomical/body coordinates. It neither moves gear nor changes its mount, order, facing, time or AP. Flipping cancels an unsubmitted drag.

Turn **Inspect worn** off to retain click-to-position fitting, Default, six-step R and Fit/equip. These fitting controls are disabled during inspection, so selection does not reposition clothing. Incoming body drops retain the existing exact-fit request; inspection refuses a one-sided mount on the wrong viewed surface. View choice does not invent a new mount: stored packs still default to their authored back mount, and an existing front pack retains its front mount. Explicit front equipping remains available through the existing mount controls.

Reopening inventory resets inspection to off and the viewed side to front. These are transient UI choices. Esc retains its existing interface-only behavior. Browser switching/dismissal and physical context actions retain normal closure preferences, access checks and time/AP; selecting a body item does not open all of its pockets.

## Projection and physical authority

`InventoryBodySurface(surface)` projects only the player's worn items for explicit `front` or `back`. It visits actual dressing order from outer to inner, uses each item's active open/closed surface mask and returns only cells not already occluded. `InventoryBodyPick(surface, cell)` identifies that unique exposed target. Bare body, outside positions and unsupported surfaces return no item.

These read-only projections include instance IDs and normal removal-access feedback. They exclude held gear, packed contents, nearby stored objects and other employees. A wrapping belt remains one source identity across both surfaces; front and back packs remain distinct items. An open jacket reveals its lower clothing; a closed jacket becomes the top target only when paid fastening completes.

The diagnostic material masks are not a final alpha/cloth visibility solver. Final sprite picking must refine visual silhouettes/occlusion without treating sprite opacity as physical permission. Ordinary access/removal declarations and fit geometry remain authoritative. Exterior attachments still use the enlarged gear view, rather than separate body targets. Dragging their worn provider carries its real descendants through the normal graph; it does not duplicate them or change their local placements.

Removal uses the same Move command as grid handling. The item stays worn until paid completion; exact square placement and two packed orientations remain independent of the old body pose. Invalid destinations, covered/strained gear, manual pause and insufficient combat AP refuse normal work. Commitment revalidates the graph, access and source revision. No automatic undressing, repacking, mount swapping or free hand transfer is added.

## Persistence and verification

No immutable content, mutable equipment field or save schema changed. Schema stays **14**; saves matching the body-cleanup content remain compatible. Accepted paid requests preserve exact continuation. Removing gear clears its worn pose/order through the existing transaction. Inspection side, hover and pressed-source state are not physical save state.

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --body-drop-smoke work/preview/spatial-scene/body-inspect.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/body-inspect-full.png
python3 tools/author_body_fit_fixture.py --check
python3 tools/check_gear_data.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

The focused body launcher now runs both incoming fit drops and inspection/removal fixtures. [Domain checks](../tests/SpatialChecks/BodyInspectionChecks.cs) qualify exposed masks, no contents/foreign leakage, open/closed jackets, front/back packs, shared belt identity, blocked removal, source retention, loaded-provider children/mass, AP and deterministic continuation. [Native input checks](../game/Presentation/SpatialScene.BodyInspectionSmoke.cs) use actual pointer positions, mode/flip controls, selection, double-click, context menus and drags into exact storage cells. Existing fitting/drop suites remain required. See [recorded results](reviews/inventory-runtime-checks.md) for actual evidence.

[InventoryBodyInspection](../src/SiteSimulation/Inventory/Equipment/BodyFit/InventoryBodyInspection.cs) owns the bounded query. [InventoryBodyFitSurface.Inspect](../game/Presentation/Inventory/InventoryBodyFitSurface.Inspect.cs) owns hover/picking/source input and diagnostic tints; [the panel](../game/Presentation/Inventory/InventoryBodyFitPanel.cs) owns modes and mirrored projection. The pressed item and cell are retained across Godot's drag threshold, then revalidated before beginning a drag.

The complete character-matched paperdoll, general wardrobe alpha presentation, world clothing composition, body attachment targets and general visual/material transparency remain future work. The separate full-body sprite proof qualifies only its explicitly documented reference-body/default-pose subset. This slice adds no SCP canon or anomalous behavior.
