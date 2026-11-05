# Placement extenders and mount points: the utility hook

Related interaction: [direct body-fit drops](39-body-drag-fitting.md) fit wearable items onto the engineering body. Enlarged gear surfaces retain their own attachment/container targets and rotation context; the new body target does not arbitrate provider drops.

Follow-up: [body-fit cleanup](38-body-fit-cleanup.md) corrects native body/clothing geometry and adds bounded adjustable belt fitting. Hook/provider contracts below remain; central webbing points retain their local coordinates.

Implemented October 29, 2026. This extends [belt attachments](32-belt-and-attachment-prototype.md), [physical overlays](36-overlay-sublayers.md) and the [nested-provider design](33-fit-ranks-and-worn-stacks.md). The hook is ordinary prototype equipment, not an SCP canon claim. The [game-wiki guide](../source_material/game-wiki/mount-points.html) connects inventory, presentation and persistence.

## Try the fixture

Start a fresh scene: immutable field-kit content identity has changed. There are now **18 definitions, 38 item instances and 27 compartment states**. The supply crate adds `utility-hook` at `(22,13)` and `utility-pouch-2` at `(3,4)`.

1. Equip the belt through normal handling and inspect its exterior.
2. Take the hook into your hand and drag its clip onto the webbing. Preview is read-only; release starts paid mounting.
3. Drop a pouch connector onto the hook's lower point, below the belt. The pouch belongs to the hook, not the belt.
4. Double-click the hanging pouch. The middle trail follows **belt → hook → pouch**; its snapped interior opens separately on the right through paid handling.
5. Use ↑ to return through providers. Dragging/rotating the hook carries its pouch and contents. Taking off the belt carries the whole assembly without rewriting child placements.

The hook's upper contact is `(0,0)` and its offered lower mount is `(-1,2)`, directly downward in the point-up grid. Mounts retain six exact 60° orientations subject to clearance. Inventory storage retains horizontal/vertical rotation. Selecting an orientation never forces a fit through another object.

## Data and physical contract

No hook-only action, extra item parent, loose-item pocket or lid is introduced. An extender is an attachable item that also offers support:

| Native field | Meaning |
|---|---|
| `ItemDefinition.Attachment.Interface` | Connector required to attach this item to its parent. |
| `AttachmentProfile.Contacts` / `Occupancy` | Own required mating cells and exterior collision envelope. |
| `StorageRegion.Attachment.Interface` | Connector offered to a child; may differ from the required connector. |
| `AttachmentSurface.Cells` | Offered points in provider-local coordinates, independent of provider origin. |
| `AttachmentSurface.Surface` | Explicit front/back plane or inherited `mounted` plane. |
| `StorageRegion.MaxMassGrams` | Entire child assemblies' load, excluding the provider's own mass. |
| `ItemPlacement.AttachmentPose` | Parent-local anchor and sixth-turn rotation. |

The native hook accepts and offers `belt-loop`; its lower mount supports **500 g**. Its `placement-extender` tag describes the fixture; interfaces and geometry implement the behavior. It offers no Arrange operation, default interior or invented storage capacity. Future connectors can use different interface IDs and existing category filters. The [author sheet](../templates/gear-data-sheet.md) and [gear standard](29-gear-data-standard.md) retain a separate proposed public card format; this fixture does not implement a public loader.

[Frame composition](../src/SiteSimulation/Inventory/Equipment/InventoryAttachmentFrames.cs) combines every link's position and rotation. Contents do not enter exterior geometry. Separate packed/held assemblies retain their own frames; worn providers on one person share the corresponding body surface. [Shared clearance](../src/SiteSimulation/Inventory/Equipment/InventoryAttachmentClearance.cs) compares attachments across regions, hooks and worn belts. Assigning different parents grants no extra physical space. Opposite surfaces remain separate.

A child may meet its immediate provider only at its required mating contacts; this exception cannot waive collision elsewhere or against cousins/unrelated gear. Compatibility, exposed contacts, fit bands, ancestor access, hierarchy depth and load remain independent checks. Explicit front/back surfaces are inherited through nested providers. Provider movement and clothing closure revalidate the complete graph. Rejection never silently detaches, relocates or opens equipment.

## Views, handling and persistence

The gear view draws and alpha-picks visible exterior descendants in composed coordinates. Drop targeting includes a hook's lower point while viewing its belt. Inspecting a visible descendant adds the intervening provider trail without opening interiors. Preview rotation carries visible descendants but performs no work. The hook's own view uses its mounted registration and the same 44-pixel diagnostic hex spacing.

The original transparent [hook sprite](../game/Assets/inventory-objects/utility-hook.png), [atlas/anchor registration](../game/Assets/inventory-objects/manifest.json) and [exact built-in image-generation prompt](../game/Assets/inventory-objects/attachment-prompts.json) are project assets. Source pixels are unchanged; art size does not define physical fit.

All physical changes use ordinary timed inventory transactions and tactical AP, retain their source until completion, and revalidate at commitment. Each descendant contributes mass once: belt 250 g + hook 80 g + pouch 100 g + dressing 40 g = **470 g**. Matching saves resume exact empty or loaded paid work. Invalid saved contacts or cross-provider intersections fail detached restore atomically.

No mutable field was added: save schema remains **14**. New immutable fixture identity requires a fresh scene.

## Verification and bounds

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/hook.png
python3 tools/run_spatial_scene.py --hook-smoke work/preview/spatial-scene/hook-focused.png
python3 tools/check_gear_data.py
```

[Domain checks](../tests/SpatialChecks/HookChecks.cs) cover six composed rotations, narrow mating exceptions, cousin/cross-belt collisions, opposite surfaces/inheritance, recursive load, ancestry, time/AP, saved continuation and invalid restore. [Native input checks](../game/Presentation/SpatialScene.HookSmoke.cs) exercise dragging, lower targets, recursive preview/rotation, trails, closure, occupied-point refusal and loaded detachment. Actual results belong in the [runtime evidence](reviews/inventory-runtime-checks.md); the wiki build does not execute gameplay tests.

This is coarse two-dimensional exterior clearance per surface. Arbitrary 3D depth offsets, swinging/sagging, garment/material volume collision, latching hooks, keyring content, dynamic assembly packing envelopes and world clothing composition remain future work. The native body silhouette and known diagonal limitation are unchanged. An isolated authored geometry fixture qualifies cousin clearance without substituting its shapes for native art.

Earlier November 4 presentation slice: [worn waist belt and direct pouch](47-worn-belt-and-pouch-paperdoll.md) qualifies reference-body front/back belt art, direct local-position/six-turn pouch art and own-worn exterior inspection. Nested hook art, other strap poses and a per-bundle JSON runtime loader remain unqualified.

Current transfer follow-up: [independent source/destination views](48-independent-inventory-transfers.md) allow dragging a stored hook or pouch directly onto the belt/provider without staging it in the single hand. Choosing destination gear retains the accessible source compartment; source checks, costs, fit and loaded continuation remain unchanged. The original hand-staged procedure above remains valid.

Current November 4 follow-up: [worn attachment chains](50-worn-attachment-chains.md) qualify the normal front belt → hook → pouch art, shared composed transforms, distinct alpha targets and loaded ground handling. Earlier direct-only/nested-art limits above describe the prior slice. Other strap poses and world clothing remain unqualified; current saves use schema 15.

Current [shirt badge follow-up](54-shirt-pockets-and-badge.md) distinguishes an item allowed to remain covered (`CanBeCovered`) from a support requiring exposed manipulation (`RequiresExposedAccess`). Defaults preserve existing hook/pouch behavior; no door credential system is added.
