# Actual worn order and primary layers

Implemented October 29, 2026. This migrates primary clothing order from fixed definition numbers to actual paid dressing, using the [fit-rule foundation](34-wear-fit-rules.md). The [full stack design](33-fit-ranks-and-worn-stacks.md) still defines the later per-hex overlay work.

## Physical contract

`ItemPlacement.WornOrder` is a positive, unique precedence number within a person's worn outfit. It is not the displayed layer, a fit rank or a clothing category. Native starting outfits now explicitly author their initial sequence. `WearProfile.Layer` remains a legacy definition field; runtime ordering, coverage, access, removal, underlayer navigation, fit and securing no longer read it.

- Newly worn equipment goes outside the current outfit at the latest order. The ordinary fit validator decides whether it can fit there. An outermost pack still prevents putting overlapping clothing over it.
- Moving/repositioning an already worn item on the same person preserves its precedence. Body refitting and front/back pack positioning cannot secretly insert it beneath another item or raise it for free.
- Taking equipment off clears its worn-only order. To change relative order, physically remove the obstructing clothing and dress again. Existing garments are never silently reordered.
- Requests may omit the order; the host resolves it before paid work begins. An explicit order must match the same canonical result. Pending work saves this resolved request. The source stays unchanged until normal handling completes, including tactical AP and interruption behavior.

A roomy shirt can therefore be worn over the light jacket, while the same shirt's family/bulk rules still reject an incompatible bulky garment. A lower authored number does not force the shirt back beneath the jacket. Opening or removing clothing exposes the appropriate paths only when the physical action commits.

## Local primary depth

[InventoryWornOrder](../src/SiteSimulation/Inventory/Equipment/InventoryWornOrder.cs) derives primary depths separately for each declared anatomical region. A shirt on skin is depth 0; the jacket over it is 1; a pack over both is 2. A pack over just the shirt is 1. Socks and shoes remain 0 and 1 independently of the chest outfit.

Removing clothing compresses the derived display depths without rewriting any remaining item's precedence. A garment crossing differently dressed regions can have a range, such as 0–1. Front/back regions remain separate. A wrapping item appearing in several groups is still one physical item with one parent and one mass contribution.

These primary depths currently use declared anatomical coverage regions, as did the fit-rule bridge. Mapped pocket mouths/cover masks still use their actual body hexes, now sorted by the real order. This slice does **not** claim per-hex same-band counts, `layer_N` overlays, crossed-belt capacity, fastening-interference masks or exposed attachment-support validation. The current single belt receives an ordinary primary depth until the overlay migration. Those distinctions are preserved for the next slice rather than approximated by a global belt count.

## Equipment interface

The fixed exposed/all/layer selector is replaced with a known-equipment list grouped as Head/face, Chest, Lower body, Feet and Held. Rows show current depth or range, name and exposed/covered/restrained state. Tooltip explanations use actual access checks. This projection includes own known clothing, never the items hidden inside its pockets.

Click selects a row. Double-click inspects gear only if normal inspection access allows it. Right-click uses the existing actions. Selection itself is inspection and performs no physical work. Covered equipment remains selectable for identification; selecting it cannot grant dragging, removal or container access. Existing gear/contents trails, closures, Esc and paid actions remain authoritative.

The **Engineering slot controls** toggle retains the temporary illustrated slot grids for drag/drop and existing development tests. The list is the default view; the final front/back anatomical paperdoll and direct body dragging are still future presentation work. Clothing art and world outfit compositing are unchanged.

## Save and validation boundary

Outer save schema is **14**. Worn items require bounded, unique precedence; held, packed and attached items must not carry it. Missing/negative/duplicate orders, worn-only state in storage and noncanonical pending dressing requests fail detached validation. Matching saves resume the same paid dress action with the same result.

Earlier inventory schemas require a fresh field-kit scene; no dressing history is invented from old definition ranks. Earlier matching **non-inventory** prototype schemas retain their existing migrations. Initial outfit changes also update inventory content identity. The SCP-650 module schema is unchanged.

## Verification

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/worn-order.png
```

[Worn-order checks](../tests/SpatialChecks/WornOrderChecks.cs) execute paid opposite-order dressing, local layer derivation, changed pocket cover and fastening/removal restrictions, pending-work continuation, free-order rejection and malformed-save rejection. The existing wear, fit, body fitting, coverage, packing, attachment, time/AP and identity checks remain required. Native checks use actual equipment-list clicks and double-clicks, inspect covered rows without mutating physical state, and retain the complete drag/dress/closure fixture through the engineering controls.
