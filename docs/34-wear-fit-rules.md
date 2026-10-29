# Wearable fit-rule foundation

Implemented October 29, 2026. This is the compatibility foundation for the [confirmed worn-stack design](33-fit-ranks-and-worn-stacks.md). It separates fit from order without replacing the current ordering representation in this slice.

## Authoring contract

Every native wearable now has `WearProfile.Fit`, independent of its body/contact `FitProfile`, inventory dimensions, mass and existing `Layer`. The proposed gear cards use the equivalent `wear.fit` section.

```json
{
  "family": "core:light-textile",
  "bulk": 1,
  "maxUnderBulk": 5,
  "fitsOver": ["core:light-textile", "core:light-jacket", "core:flexible-armor"],
  "excludesUnderTags": ["core:rigid-shell"]
}
```

| Field | Meaning |
|---|---|
| `family` | Reusable fit category presented to the item outside it. Namespaces are recommended; matching is exact and case-sensitive. |
| `bulk` | This garment's contribution to the bulk beneath another garment at an overlapping region. |
| `maxUnderBulk` | Maximum combined underlying bulk this garment accommodates. Zero allows wearing it against skin. |
| `fitsOver` | Explicit underlying family whitelist. Empty means no underlying garments; an unknown family is refused. |
| `excludesUnderTags` | Optional trait blacklist, overriding an otherwise accepted family. |

Bulk and clearance use relative integer fit units. They are prototype authoring values, not millimeters, mass, storage cells, permanent balance numbers or the belt's same-band under-limit. Native validation bounds bulk to 1–1000, clearance to 0–1000, and each family/trait list to 32 distinct nonblank tokens. Missing or malformed wearable fit metadata rejects content. The pure evaluator rejects queries above 128 underlying items as a provisional safety bound, rather than reporting that the garment is full.

The native shirt accommodates the light jacket or flexible armor within its clearance. A bulky parka fails the shirt's family whitelist. A family-compatible shell can still fail an explicit exclusion. A high authored layer does not grant any of these fits. An outermost pack cannot be covered even by another pack with enough numerical clearance.

## Evaluation and runtime integration

[InventoryWearFit](../src/SiteSimulation/Inventory/Equipment/InventoryWearFit.cs) is a pure, deterministic check of one outer definition against the supplied underlying definitions. It never chooses their order, mutates inventory, reveals contents or performs work. Compatibility is directional: both shirt-over-jacket and jacket-over-shirt can be admitted by their respective profiles. Each actual garment contributes bulk once, even when several instances share a definition. Combined bulk matters independently of pairwise family compatibility.

The current bridge evaluates each wearable against the lower items overlapping each declared anatomical region. A jacket crossing several regions contributes once per local check, not once per region to a global total. Footwear does not consume torso clearance. Opening a jacket exposes access but does not remove its physical bulk.

The bridge runs within ordinary inventory graph validation. Move previews, paid starts, final commit checks and saved graph validation therefore use it. An invalid fit rejects before time/AP is spent; a stale destination cannot commit. A valid fit still requires anatomical contacts, a legal mount, physical access, unobstructed dressing, mass capacity and normal time/AP. Existing compartments, attachments, ownership and identity remain authoritative.

## Explicit migration boundary

The current bridge still receives order from `WearProfile.Layer`. Same-layer mutual overlap continues to use the existing legacy rule. This slice does **not** persist actual dressing order, implement per-hex bands/subpositions, enforce the belt's base-plus-two under-limit, add multiple belts, or replace the equipment selector. It proves compatibility independently so the later worn-stack projection can supply actual local order without rewriting fit policy. The reversed shirt/jacket proof uses proposed ordering fixtures, not a new native UI action.

The runtime supplies eight native wearable profiles. Flexible armor and the bulky parka are definition-level test fixtures; no extra item art or catalog entries are claimed. The [author sheet](../templates/gear-data-sheet.md), [schema](../schemas/gear-definition.schema.json) and shirt/jacket/backpack example cards carry the matching contract. They remain proposed cards rather than native-loadable mods.

Fit profiles contribute to the immutable inventory content identity. Earlier field-kit saves require a fresh scene. There are no new mutable instance fields, so outer save schema stays 13; a new revision belongs to the later saved worn-order migration.

## Verification

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/wear-fit.png
python3 tools/check_gear_data.py
```

[Domain checks](../tests/SpatialChecks/WearFitChecks.cs) qualify reversed compatible combinations, incompatible bulky gear, cumulative clearance, duplicate physical garments, directional exclusions, outermost packs, invalid metadata, region isolation, ordinary move rejection, paid commit revalidation, combat AP and exact pending-work continuation. Existing native inventory tests remain required because the new rule participates in their shared graph validation.
