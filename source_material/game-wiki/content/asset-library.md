# Asset organization and item bundles

**Implemented:** a complete native-item taxonomy, representative category art and [browsable item catalog](items.html). **Migration target:** coherent per-item art bundles under `game/Assets/items/<category>/<item-id>/`. Existing atlases have not moved and current runtime loading remains unchanged. See the [authoring and migration standard](doc-docs-46-item-asset-library-md.html).

## Browse and review

Choose a family, subtype and individual item. Categories cover worn clothing, wearable containers, belts/hooks, other equipment, portable containers, consumables and medical supplies. Shoe pages list each authored shoe definition; empty glove, hat and helmet categories are clearly marked. Category artwork comes from an explicitly selected representative item, rather than an invented item or another physical definition.

Each page shows current native statistics, source-art previews, original image path/hash, paperdoll binding/registered frames and exact definition. The [machine-readable catalog](item-catalog.json) supplies the same identities and raw data. Capability views link wearable, storage, attachment and mount-provider items without duplicating their primary category or definition. This is authoring knowledge, not automatic inventory/player/AIC knowledge. Example design cards and SCP reference art are outside the native item list.

## Standardized future bundles

The target places shirts, shoes, socks, gloves, hats and helmets under `worn/clothing/`; backpacks and removable pouches under `worn/containers/`. Each stable item folder keeps its proposed `item.art.json`, loose/gear/paperdoll/world representations, view/state masks and provenance. Manifest references support shared atlases; existing images need not be destructively split. Runtime appearance and physical fit remain separate.

Future PNG masks use representation/view/state-qualified names such as `boots.paperdoll.front.mask.png`; their alpha multiplies the matching source alpha and RGB is ignored. This is a documented naming/format target, **not implemented PNG-mask loading**. The current [worn-only shoe masks](worn-alpha-masks.html) are polygons on existing atlas regions. Loose/drag art never inherits a worn mask by accident.

Editable sources belong outside Godot's runtime import tree, with provenance linked to qualified outputs. The same family/ID/bundle approach can later organize actors, world primitives, UI, effects and audio; their detailed migration contracts are not implemented here.

## Evidence and maintenance

[Catalog generator source](evidence-tools-wiki-items-py.html) validates all current definitions, categories, representative membership, art bindings and source regions. Generated source PNG copies are byte-identical and content-addressed. Build/check and deployment compare source and copied-art hashes, not just HTML links. The [build guide](evidence-source-material-game-wiki-readme-md.html) and [development workflow](development.html) remain mandatory; the [dedicated catalog checks](evidence-tests-test-wiki-items-py.html) are recorded in the authoring standard.

The November 4 verification passed five catalog tests, full wiki build/check, project document checks and a headless Chromium search/render pass. Category overview, work-shoe detail and a 430-pixel empty-type page were visually reviewed. These checks do not certify gameplay.

No native inventory data, save identity/schema, wear/access behavior or runtime atlas path is changed by catalog classification. Moving current assets requires a separate qualified migration.

Related: [item directory](items.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [wearable appearance](wearable-appearance.html), [worn masks](worn-alpha-masks.html), [implementation ledger](status.html), [design library](documents.html).

[Worn waist belt and direct pouch](worn-belt-and-pouch.html) now qualify default-pose front/back belt art and a real direct attachment, with normal access, paid handling and source-backed catalog registration. The [worn hook-chain follow-up](worn-attachment-chains.html) now qualifies the normal front hook → pouch chain.

[Worn attachment chains](worn-attachment-chains.html) now render and alpha-pick the normal front belt → hook → pouch assembly through shared composed frames. Physical access, paid handling, independent trails and loaded ground transfers remain authoritative.

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify four coat pockets, six trouser compartments and backpack main/side/front. [Every depicted pocket is usable](visible-pockets.html); front/back cargo views share storage. Coverage and independent paid fastenings still apply; animation remains future work.

[Shirt pockets and detachable badge](shirt-pockets-and-badge.html) add two buttoned chest compartments and a real clip-mounted badge. Covered retention and manipulation are separate; this changes native kit identity to 20 definitions/40 instances while retaining schema 15. [Badge-controlled access](badge-door-access.html) now implements exposed wearing or equipped-hand presentation at one local reader; production credential administration remains future work.

[Current visible-pocket contract](visible-pockets.html): depicted pockets require real storage; multiple views share one inventory.
