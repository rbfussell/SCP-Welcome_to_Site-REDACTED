# Item asset catalog and future bundles

`catalog.json` is the implemented authoring taxonomy and category-art selection for native items. It references current definition/loose/paperdoll manifests without replacing runtime loaders or moving existing atlases.

Read [the bundle naming/migration standard](../../../docs/46-item-asset-library.md) before adding art. Browse [the item catalog](../../../source_material/game-wiki/items.html) or its [public site](https://rbfussell.github.io/SCP-Welcome_to_Site-REDACTED/items.html).

Target layout: `<category>/<stable-item-id>/item.art.json`, qualified loose/gear/paperdoll/world representations, view/state masks and `provenance.md`. Existing registrations may share atlases. The first `worn/accessories/belts/utility-belt/` reference bundle now holds a qualified worn atlas and provenance, referenced by the existing paperdoll manifest. Its `item.art.json` is authoring metadata, not a runtime bundle loader. Other bundle migrations and PNG mask loading remain future work. Physical definitions and inventory services remain authoritative.
