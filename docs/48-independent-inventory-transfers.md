# Independent inventory source and destination views

Implemented November 4, 2026. This revises the view-lifetime rule in the [three-view contract](27-three-view-inventory-and-body-fit.md): choosing another gear object no longer discards the accessible source compartment. See the [game-wiki mechanic](../source_material/game-wiki/inventory-transfers.html), [mount providers](37-placement-extenders-and-mount-points.md), [physical inventory](23-personal-inventory-prototype.md) and [body inspection](40-body-inspection-and-removal.md).

## Using the three panes

The paperdoll remains on the left. The middle shows destination gear; the right can retain a previously opened source compartment, even when it belongs to another garment or backpack. The source breadcrumb includes its actual owner and each inspected compartment. There is still only one contents pane; selecting another compartment explicitly replaces its current branch, while inspecting a nested container descends within it.

For example, open the backpack side pocket, then inspect a worn belt. Drag a compatible hook directly from the right grid onto the middle belt webbing. Open the backpack main section, inspect the belt again, then drag a stored pouch onto the hook's lower point. This works with an occupied hand: no intermediate hand placement is required by the UI. Ordinary fit, reach, load, hierarchy, access, time and tactical AP still apply. Previewing or releasing while paused grants no free work; a paid transfer retains its source until commitment and can resume from a matching save.

Switching gear is inspection, not an instruction to secure the retained source. Switching during a pending physical opening still cancels that opening's old UI callback; the physical action can complete without replacing the retained source or destination views.

## Navigation, closing and physical changes

Both trails retain their own Up and × controls. Closing unrelated destination gear leaves the source visible and physically unchanged. Closing a gear provider on which the displayed source actually depends dismisses those dependent contents and requests ordinary per-fastening securing. This dependency follows physical ancestry, including belt → hook → pouch, rather than requiring the source owner to appear in the current gear breadcrumb. Returning from shirt to jacket still retains a jacket-owned pocket.

Right-hand Up returns through source containment and applies the departing compartment's securing preference. Right-hand × dismisses that source trail, keeping the destination gear. Child closures remain paid and ordered before parent closures. Closure history is scoped to the current gear or retained source; closing an unrelated belt cannot secure previously visited backpack compartments.

Every displayed source step still checks its expected placement and normal access. Securing its ancestor, covering its pocket, moving its owner, or leaving fixture reach removes the invalid step and its descendants. Invalidating destination gear alone does not discard an independently valid source. Esc/I/Close hide the interface without changing physical closures; reopening starts with empty inspection trails.

## Implementation and verification

The [browser](../game/Presentation/Inventory/InventoryBrowser.cs) retains the source across gear changes and scopes closure history to physical branches. Existing [grid input](../game/Presentation/Inventory/InventoryGrid.cs), [exterior targeting](../game/Presentation/Inventory/InventoryGearSurface.Attachments.cs) and simulation transactions perform the transfer. Source and destination remain real graph locations, not a temporary cursor inventory or extra storage.

```sh
python3 tools/run_spatial_scene.py --transfer-smoke work/preview/spatial-scene/source-transfer.png
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/source-transfer-full.png
```

[The native transfer fixture](../game/Presentation/SpatialScene.TransferSmoke.cs) exercises occupied-hand direct hook/pouch drags, paused refusal, source retention, exact pending restore, independent provider navigation, nested source Up, paid closure, provider dependency, reach and coverage invalidation. Actual run results are recorded in [runtime evidence](reviews/inventory-runtime-checks.md); wiki generation does not execute gameplay checks.

At this slice’s qualification, simulation definitions, physical dimensions, packing rules, artwork and save schema were unchanged (schema 14). This slice did not add nearby ground storage, another hand, simultaneous views of two contents grids, a general transfer planner or sprite-paperdoll drop targets. Clothing staging on the ground remains a separate planned slice. Existing gear surfaces and engineering controls remain available for exact destinations.

Current ground follow-up: [the permanent current-tile dock and physical piles](49-ground-inventory-and-piles.md) now provide clothes staging without a spare hand. Covered floor items and their contents require moving covering items first. This supersedes earlier ground-as-future statements for this bounded feature; current saves use schema 15 and migrate matching schema-14 inventory saves. The earlier slice’s schema/content claims describe that slice’s qualification. Floating internal inventory windows remain a future production-UI consideration.
