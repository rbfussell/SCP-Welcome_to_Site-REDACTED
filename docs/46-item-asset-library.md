# Item asset library and browsable catalog

November 4, 2026. The user requested a concise type-based asset structure and a visual item wiki for art, detail and balance review. The authoring taxonomy and generated catalog below are implemented. Relocating the current atlases, per-bundle runtime loading and PNG mask loading remain future migration work. This is development documentation, not SCP canon or automatic actor knowledge.

## One item identity, several representations

Use one stable definition ID and one primary authoring category per item. Keep artwork, registration, view/state masks, attribution and source references together. An item's folder is not its permission to be worn or used; native definitions and validated services retain that authority. A jacket may be primary clothing and offer storage, while a pouch may attach to a belt. Capability cross-links must not duplicate the item definition or require a second copy of the same art.

The [bundle entry point](../game/Assets/items/README.md) explains the current boundary. The current [registry](../game/Assets/items/catalog.json) classifies every definition in the native field kit exactly once and selects representative category artwork by item ID. It is explicitly `authoring-catalog`, not a runtime replacement for [gear cards](29-gear-data-standard.md) or [native definitions](../src/SiteSimulation/Inventory/Content/field-kit.json). It references current loose/worn manifests. Existing IDs, atlas paths, inventory content identity and save schema are unchanged.

## Target bundle convention

New qualified item sets should converge on this layout through an explicit migration; this example is not a claim that the current files have already moved:

```text
game/Assets/items/
  catalog.json
  worn/
    clothing/
      shirts/<item-id>/
      shoes/boots/
        item.art.json
        boots.inventory.png
        boots.paperdoll.front.png
        boots.paperdoll.front.mask.png
        boots.paperdoll.back.png
        boots.paperdoll.back.mask.png
        provenance.md
      socks/<item-id>/
      gloves/<item-id>/
      hats/<item-id>/
      helmets/<item-id>/
    containers/
      backpacks/<item-id>/
      pouches/<item-id>/
    accessories/
      belts/<item-id>/
      hooks/<item-id>/
  equipment/<type>/<item-id>/
  containers/<type>/<item-id>/
  consumables/<type>/<item-id>/
  medical/<type>/<item-id>/
```

Coats, trousers, underwear and suits also have clothing categories. Empty glove/hat/helmet categories are reserved authoring types; they do not promise playable content. Directory names use lowercase kebab-case, with stable IDs independent of display names. One folder contains the item's supported art set, not one folder per physical instance.

`item.art.json` is the proposed bundle manifest: stable item ID, definition reference, art version, explicit representation/state/facing/frame bindings, source rectangles/registration, mask references, segmented regions, shared-source references and provenance. Optional filenames describe roles; the manifest is authoritative, so code must not guess behavior from directory names or parse filenames to invent missing animation states. Current runtime manifests remain active until a reviewed adapter/migration replaces them.

Typical roles are `.inventory.png`, `.gear.closed.png`, `.gear.open.png`, `.paperdoll.front.png`, `.paperdoll.back.png` and `.world.walk.ne.png`. Supported variants/states are explicit metadata, not a requirement to manufacture every imaginable file. Shared atlases remain valid: reference the same image plus its region rather than splitting or duplicating it merely to match a name. Category representatives can reference existing art rather than requiring another thumbnail asset.

## Mask convention and authoring sources

A future PNG mask is paired to the exact representation/view/state it modifies, for example `boots.paperdoll.front.mask.png`. It must have matching registered source dimensions; **alpha multiplies source alpha** (0 hides, 255 keeps, intermediate alpha partially retains; RGB is ignored). An opaque black pixel therefore does not hide anything. Unrelated representations never inherit a mask just because they share an item ID. Packed/drag art remains intact. Runtime support for these PNG mask files is not implemented yet; current [worn masks](45-worn-alpha-masks.md) remain authored polygons on existing atlas regions.

Original editable art, renders and generation prompts may live under `art-source/items/<category>/<item-id>/` outside the Godot import tree; the runtime bundle links their provenance and qualified outputs. Preserve original bytes, author/licensing notices, exact generation/source identity and registration choices. Do not derive physical dimensions, capacity or protection from image dimensions or category labels.

The same bundle principle should guide future actors, world tiles/props/buildings, UI, effects and audio: group by family and stable content ID, then record representations/variants in a manifest. Their precise loaders and file standards remain to be specified when those systems are migrated. No mass folder move is part of this slice.

## Generated visual catalog

[The item wiki](../source_material/game-wiki/items.html) browses top-level family → subtypes → individual definitions. Each category's `representativeItem` is an author choice; categories with no assigned art display an honest placeholder. A new shoe type becomes another item entry under shoes, rather than a new handwritten HTML page.

[The generator](../tools/wiki_items.py) reads the native field kit and existing art manifests. It creates category pages, individual item pages, global search entries, capability views and `item-catalog.json`. Item pages show exact empty mass and packed dimensions, explicitly authored base handling values, tags, storage/mount geometry, wear/fit data, artwork provenance/hashes, supported reference-body bindings and registered worn frame/mask metadata. Omitted fields remain labeled; material/condition/combat effects are not inferred from art. Handling totals still depend on access/action context. Raw native definitions are included for balance review; proposed example gear cards and SCP illustrations are not silently listed as implemented items.

PNG copies in the generated wiki are byte-identical source files, content-addressed and deduplicated. SVG source-region previews display them without image editing or re-encoding. These source-art previews are not in-game screenshots or proof that all wearable views are qualified. The public site and offline copies use local assets, without depending on private desktop paths or external image hosting.

The build validates complete definition/category/art coverage, parent categories, representative-item membership, unique URLs, PNG dimensions/source regions and path containment. Freshness hashes include catalog data, native definitions, art manifests, generator and original/copied PNG bytes. Both local `--check` and Pages deployment reject changed artwork without a refreshed snapshot. Source/hash validation does not establish semantic agreement or run gameplay tests.

The November 4 [catalog test source](../tests/test_wiki_items.py) run passed all five dedicated checks: complete native-data/page coverage and byte-identical PNGs, missing classification, wrong representative family, escaped paths and invalid source regions. A headless Chromium pass rendered the catalog, shoe category, work-shoe page and a 430-pixel empty helmet category; search produced the native work-shoe result. Overview/detail/mobile captures were visually reviewed, and the initial full wiki build/check passed 1,983 pages with 120 unchanged C# files. This is documentation tooling evidence; gameplay suites were not rerun because no game definition or runtime loader changed.

## Workflow and migration gate

1. Add/edit native item data through its current validated authoring path.
2. Add or update art/registration/provenance in the current manifest; register its primary category in `catalog.json` and optionally choose category art.
3. Rebuild the wiki, inspect the category/item pages and source previews, and run the checks below.
4. Commit the data/art, affected wiki sources and generated output together. Push the prototype branch to update the public catalog automatically.

A later file migration must update all runtime references, generators, prompts/provenance links, tests, wiki mappings and deployment inputs together; retain stable item IDs, preserve source hashes or explicitly qualify new art, and test saves and real loose/worn/drag rendering. Directory cleanup alone is not permission to change physical item definitions.

```sh
python3 -m unittest discover -s tests -p test_wiki_items.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

Related: [wiki build guide](../source_material/game-wiki/README.md), [wearable appearance](26-wearable-appearance-pipeline.md), [worn mask metadata](45-worn-alpha-masks.md), [asset provenance](../game/Assets/inventory-objects/README.md), [public publishing](12-development-workflow.md#public-wiki-publishing).
