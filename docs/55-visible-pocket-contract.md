# Every depicted pocket is usable

Current contract, November 5, 2026: **if item artwork depicts a pocket or pouch, it must have usable storage**. This supersedes the decorative-pocket limitations in earlier [pocket slices](53-paperdoll-pocket-segments.md). It is an equipment authoring rule, not an SCP canon claim or automatic image inference.

## Current reference kit

| Item | Real compartments | Additional views |
|---|---|---|
| Field coat | `chest`, `chest-left`, `lower-left`, `lower-right` | Open/closed front share the same four inventories. Existing `chest` ID and contents remain intact. |
| Trousers | `left`, `right`, `cargo-left`, `cargo-right`, `rear-left`, `rear-right` | Front/back cargo contours address the same two cargo inventories. Rear seat pockets are separate. |
| Backpack | `main`, `side`, `front` | Bag-side segments follow front/back mounting; straps never become storage. |
| Shirt | `chest-left`, `chest-right` | Existing independent shirt pockets; the badge clip remains an attachment support. |

New coat chest-left and lower pockets are buttoned. Lower pockets have provisional 5×5 cells at 25 mm, 40 mm depth and 1000 g limits. Rear trouser pockets have 4×4 cells at 25 mm, 30 mm depth and 600 g limits, with buttoned closures. Backpack front pouch has 6×7 cells at 25 mm, 60 mm depth and 2000 g limit, with Buckle/Unbuckle controls. All new compartments start secured on every physical instance. These capacities are provisional authored kit values, not final balance.

Coat loose/enlarged art now references a metadata crop of the existing closed worn coat sheet, eliminating its different pocket layout without modifying source PNGs. New worn contours and enlarged hotspots link to real region IDs. Enlarged views offer explicit Open buttons for any compartment not represented by that view's hotspots, including rear trouser pockets. Every worn-side target still requires actual visible opaque pixels. Covered pockets remain inaccessible through ordinary physical coverage services.

## Authoring and interaction

A depicted pocket needs an ordinary region definition, physical dimensions/load limits, correct front/back access footprint, independent fastening/default state and visual segment/hotspot linking to that region. Add a new compartment only for genuinely separate storage. Multiple illustrations or mounting sides must reuse the existing item/region identity. Review the [gear standard](29-gear-data-standard.md), [asset library](46-item-asset-library.md), [mapped coverage](31-pocket-coverage-prototype.md), [worn drops](52-worn-container-drops.md) and [shirt/badge boundary](54-shirt-pockets-and-badge.md).

Hover and inspection do not grant storage access. Opening, moving, arranging and securing retain normal time/AP, pause refusal, source retention and commit-time validation. Grid drops remain exact; deliberate container drops use scoped packing. Opening one pocket never opens its siblings. Moving the owning item carries all contents once. Painted badge pixels never grant credentials.

The kit remains 20 definitions/40 instances; immutable content identity changes because new region definitions and initial states were added. Start a fresh scene. Matching saves remain schema 15; old mismatched kits reject atomically. No new mutable save field or image segmentation service is introduced. The enlarged coat illustration uses closed reference art even when its front is physically open; state labels and commands remain authoritative. World wardrobe art, general body variants, fastening animation and final capacity balance remain future work.

## Verification

[Domain checks](../tests/SpatialChecks/VisiblePocketChecks.cs) cover complete region sets, closed refusal, independent paid storage, rear surfaces and exact loaded continuation. [Native checks](../game/Presentation/SpatialScene.VisiblePocketSmoke.cs) exercise all four worn coat contours, both coat states and rear trouser/cargo input. [Backpack checks](../game/Presentation/SpatialScene.PaperdollPackPocketSmoke.cs) include all three compartments on both mounts. Run `python3 tools/run_spatial_scene.py --check`, `--pocket-smoke PNG` or full `--inventory-smoke PNG`. Actual results belong in the [runtime review](reviews/inventory-runtime-checks.md); a wiki build does not run gameplay checks.

See the [current wiki contract](../source_material/game-wiki/visible-pockets.html) for linked subsystem context.
