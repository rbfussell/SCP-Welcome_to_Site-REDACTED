# Physical overlay bands and belt sublayers

Implemented October 29, 2026. This extends [actual worn order](35-actual-worn-order.md) with per-hex/surface bands, the agreed base-inclusive allowance, and exposed attachment contacts. It does not add crafting, spillage, hook art or world outfit compositing.

## Try the native fixture

Follow-up: [placement extenders and mount points](37-placement-extenders-and-mount-points.md) add the hook, composed exterior descendants and shared clearance. Counts and hook exclusions below describe this overlay slice's original fixture; the later guide records the current field-kit counts.

Start a fresh scene. The supply crate now contains four instances of the existing utility belt definition; the existing transparent sprite is reused. There are **17 definitions, 36 instances and 26 compartment states**. No fictitious lid was added to a belt. The pouch is now at crate cell `(10,9)`; the belts occupy `(10,13)`, `(16,13)`, `(10,17)` and `(16,17)`.

Put the worn pack aside before fitting straps across the chest: a belt cannot cover an outermost pack. Take a belt into the hand, select **Body fit · hex view**, then set its anchor/orientation and apply normal paid fitting. Useful native poses, in dressing order:

| Belt | Anchor | Rotation | Role |
|---|---|---|---|
| First | `(-5,11)` | 0° | Waist |
| Second | `(-3,9)` | 120° | Diagonal crossing the waist |
| Third | `(-4,9)` | 0° | Chest strap crossing the diagonal elsewhere |

These three fit because no cell contains more than the latest base garment plus two straps. A fourth strap on the chest crossing fails. Lower intersecting straps must be freed before removal/refitting; an action never inserts them through a later garment. The equipment list shows `layer_N` and local ranges. The body diagnostic shows other worn straps in gold and numbers at crossings. **Engineering slot controls** remain temporary drag widgets; use the list/body selector to distinguish belts sharing that mount.

The deliberately crude nine-shoulder native body currently admits one diagonal belt line with the fixed five-cell strap/contact shape. The live demonstration uses that diagonal and horizontal straps. A separately authored symmetric reference body qualifies both diagonal directions and a true X. This feature does not silently alter the native body silhouette or weaken boundary contacts to make every orientation fit.

## Band contract

[InventoryWornStack](../src/SiteSimulation/Inventory/Equipment/InventoryWornStack.cs) projects worn masks into `(body surface, hex)` coordinates and visits each cell in saved dressing order.

- Ordinary clothing starts a new primary band at that cell, with subposition 0 and one base item.
- An item with `WearProfile.OverlayLimit` joins the current band instead of increasing its primary depth.
- Against bare skin, the first overlay starts `0`, then further overlays use `0_1`, `0_2`.
- Over a base garment at primary depth 1, belts use `1_1`, `1_2`.
- Every applicable overlay's limit remains binding. A later permissive strap cannot bypass an earlier tighter one.

An allowance of **3** admits garment + two belts, or three belts on bare skin. Garment + three belts and four naked belts fail. Deeper garments belong to earlier bands and are not counted again as the current base. Limits apply only at actual intersections on the same surface. Three belts elsewhere on the body do not consume a global three-belt budget. Bulk/clearance, mass, shape contacts and paid handling remain independent checks.

Primary depths and fit bulk now also use actual overlapping surface cells, replacing the previous broad-region approximation. A garment may have several local primary/sublayer positions; the UI reports a range. The initial footwear order and boot clearance were adjusted to accommodate the existing sock/trouser overlap in the placeholder masks. This is provisional fit tuning, not a new permanent clothing rank.

The current conservative physical footprint is the closed worn mask, with fitted-shape fallback for unmapped legacy gear. Opening clothing changes visibility/access through its open mask; it does not erase its fit bulk or create another primary band. Anatomical zone rows in the body definition label the resulting cells for the equipment list. Validation requires every front/back body cell to have exactly one zone. The band solver itself uses hex/surface coordinates, not zone names or sprite pixels.

## Geometry, access and attachments

The belt has authored front and back masks. Its single offered webbing region uses `AttachmentSurface.Surface`, defaulting to the provider's mounted side, or front when no mounted side is declared. This native view still offers one front webbing surface; arbitrary multi-surface gear-view authoring is not implemented here.

[Contact exposure](../src/SiteSimulation/Inventory/Equipment/InventoryAttachmentExposure.cs) composes the item's provider chain into the worn body frame and checks each required contact against higher clothing/straps. A non-covering belt can leave pockets accessible while physically occupying a mounting contact. On the native waist belt, the diagonal covers local contact `(0,0)` while another contact can remain usable. Mounting does not require removing every overlaid strap when the chosen contacts are clear.

A proposed overlay or garment closure cannot cover an already occupied required contact. The entire candidate graph fails before work starts; no child is detached, relocated or hidden inside an invalid arrangement. Existing ancestry still gates packed/closed providers. Nested providers use the same composed frame, but hooks, multi-provider pouch clearance and their art remain separate qualification work.

Access cover, visual occlusion, removal restraint and fastening interference remain distinct. Strap-to-strap restraint uses actual intersections regardless of legacy region labels. Normal garment removal retains its existing explicit coarse restraint declarations; a full cloth/hem restraint model is not claimed. Outermost gear also checks physical overlap, so a waist belt does not falsely restrain a nonintersecting pack just because both mention torso regions.

`WearProfile.BlocksFastenings` and an underlying garment's `FasteningFootprint` independently prevent operating a covered zipper/button opening. The native jacket has a front zipper mask. Closing/opening garments now validates the resulting whole graph, including mounting contacts, through ordinary handling. Opening a view or selecting a known lower item never performs that work.

## Authoring and persistence

New native fields:

| Field | Meaning |
|---|---|
| `WearProfile.OverlayLimit` | Optional same-band participant limit, including the base once. Omit for primary clothing. Native authoring bound: 1–16. |
| `WearProfile.BlocksFastenings` | Whether its physical footprint can obstruct an underlying fastening. |
| `WearProfile.FasteningFootprint` | Own garment-local surface masks for the operated opening. Separate from pocket mouths. |
| `AttachmentSurface.Surface` | `front`, `back` or `mounted` for projecting contacts. |
| `BodyMask.Zones` | Bounded nonoverlapping row/surface labels for equipment groups. |

Wearables require a physical body shape; explicit closed masks cannot be empty. Overlay masks must be nonempty and authored against the body shape. Fastening masks must belong to the owner; `mounted` masks require an explicit mount side. The [author sheet](../templates/gear-data-sheet.md) and proposed gear-card schema include the matching wearable fields; the card format still is not the public mod loader.

Sublayers are derived from existing `WornOrder`, poses and immutable masks rather than stored twice. Save schema remains **14**. The new fixture instances/definitions change content identity, so earlier field-kit saves need a fresh scene. Matching saves reconstruct identical bands and resume pending paid work. Invalid overfull saved bands fail detached restore atomically.

## Verification

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/sublayers.png
python3 tools/check_gear_data.py
```

[Overlay domain checks](../tests/SpatialChecks/OverlayChecks.cs) qualify native crossings, the authored symmetric X, bare/clothed thresholds, deeper bands, mixed allowances, opposite surfaces, independent bulk, geometric restraint, occupied/covered contacts, zipper interference, closure revalidation, tactical AP and save continuation. [Native input checks](../game/Presentation/SpatialScene.OverlaySmoke.cs) exercise actual covered-contact dragging, body fit rejection, sublayer labels, diagnostic crossings and exact restore. Existing packing, employment, awareness, combat and all earlier inventory suites remain required.
