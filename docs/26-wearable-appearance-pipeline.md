# Wearable appearance: paperdoll and world sprites

Current interaction follow-up: [body inspection/removal](40-body-inspection-and-removal.md) now supplies exposed front/back targets and paid drag sources in the engineering view. This does not implement the character-matched sprite or alpha-silhouette compositor described here.

Status: October 25, 2026. User-confirmed goal: any supported wearable should have a coherent appearance on the paperdoll and on the character in the world, with procedural authoring where practical. The production approach below is proposed, not a completed renderer or asset-generation tool. Existing world characters remain baked artwork.

This extends [sprite presentation](06-sprites-and-placeholders.md), [body equipment](24-body-equipment-and-container-navigation.md) and [wear layers and fit](25-wear-layer-and-fit-standard.md).

The subsequent [surface-mapping proposal](28-wearable-metadata-and-surface-mapping.md) adds an experiment that reuses aligned front/back paperdoll textures through shared body-pose projection maps. It records continuous belt wrapping, backpack straps and independent access/removal/visual masks. Both that route and rig-generated sprite layers remain proposed; the actual world actors still use baked artwork.

## One outfit, several representations

### Confirmed paperdoll presentation (October 30)

The [game-wiki appearance guide](../source_material/game-wiki/wearable-appearance.html) connects this requirement to current body fitting, provider interactions and source evidence.

The finished paperdoll depicts the controlled character's actual appearance: matching supported body proportions, skin/head/hair and appearance variants, with worn clothing sprites aligned to that body. The equipped graph supplies the same item identities, colors, variants, placement and fastening states used for the character's world appearance. A universal mannequin or the packed inventory illustration is not the final worn representation.

The normal paperdoll hides its fitting grid. Body/gear hexes remain underneath for occupancy, contact, layering, attachment and access checks; engineering diagnostics can still expose them. Hiding the grid changes presentation only.

Hover/selection and valid/invalid placement feedback follow the garment's visible cutout. Clip tint and outline to the current worn sprite's silhouette/alpha and its visible portions after layering. Transparent padding, neck/arm openings and cutout gaps must not be filled by hexes, overlapping squares or a rectangular highlight. Drawing and visual picking share the same body, pose, scale, orientation, closure-state and occlusion transforms. An open jacket uses its actual open silhouette, with the exposed shirt visible beneath it. Retain the detailed pixel-sprite treatment; following a rounded garment contour does not call for smoothing away authored pixel structure.

Visual silhouettes, fitting occupancy, access cover and removal restraints remain separate. Alpha cutouts do not resize the physical grid footprint, grant access through a transparent barrier or clear an obstruction. Final rendering/picking masks need art qualification; no new alpha threshold or production baking method is frozen here.

**Implementation boundary:** the current [body-drag fixture](39-body-drag-fitting.md) still draws diagnostic cells and contacts. Enlarged gear segments already have alpha-clipped tint/picking in `InventoryArt`/`InventoryGearSurface`; their polygon outlines and body diagnostics do not qualify the final silhouette compositor. Inventory-driven matching body/clothing sprites and their silhouette highlights remain confirmed requirements, not implemented artwork.

The authoritative equipped graph determines what a character wears. A separate appearance catalog maps wearable definitions to compatible bodies, garment representations, supported fastening states and attachment points. The paperdoll and world renderer resolve that same outfit; they must not keep independent copies of equipped state.

| Representation | Purpose |
|---|---|
| Inventory object illustration | Readable loose item in a packing grid |
| Paperdoll appearance | Garment fitted to the chosen body in front/back inspection poses |
| World appearance | Garment on the moving character, matching facing, action and animation phase |

These representations can share source geometry, materials, colors and identity while using different cameras and resolutions. A loose jacket icon is not an animated worn jacket. Appearance metadata remains separate from physical dimensions, fit, capacity, AP and containment rules; replacing art must not change gameplay state.

## Recommended production route

Use shared body rigs and reusable garment/attachment templates as source assets. Fit a garment to a supported body family, animate it on the common rig and render its appearance into sprite layers. Generate paperdoll front/back poses from the same source. Godot continues to present the resulting 2D sprites in the established Fallout-inspired style; this proposal does not replace the game with a runtime 3D world.

The current catalog uses six facings (NE, E, SE, SW, W, NW), with state/frame totals declared by each rig. The authoring pipeline should consume those declarations, stable ground anchors and timing contracts. It must not independently invent a garment's movement phase or let sprite frames drive simulation time.

Template families can cover common shirts, trousers, jackets, suits, hats, masks, glasses, shoes and packs. Procedural parameters can handle supported cuts, body-size variants, lengths, colors, materials and attachments. An unusual wearable supplies custom geometry or explicitly authored per-view layers while using the same appearance contract. Every wearable can have an authoring route without claiming a single flat image contains enough information to reconstruct every hidden side, fold and animation pose.

A 2D authoring route remains valid: aligned garment layers for the required body/pose/facing keys, with anchor and masking metadata. Rigged source geometry is the recommended way to reduce repeated production work across many views and body variants, rather than a requirement that every modder use one particular modeling tool.

## Fitting, layering and animation

Each appearance profile should identify:

- Compatible body/rig families and explicit size or fitting variants. Unsupported anatomy needs an override or a diagnostic fallback rather than silent distortion.
- Body-region coverage, wearable/attachment anchors and links to the corresponding equipment definition.
- Source garment/template or authored sprite layers, materials and recolorable regions.
- Supported open/fastened states. Opening a jacket changes visible garment shape as well as exposing the clothing underneath.
- Required action/facing/frame bindings, common ground anchor and attachment transforms.
- Occlusion masks and split draw parts where necessary: a far arm, torso, near arm, straps and back-mounted pack do not always share one whole-image draw order.

The [actual dressing order](35-actual-worn-order.md) and [physical surface intersections](36-overlay-sublayers.md) establish the worn stack; visual compositing also depends on facing, anatomy, pose, transparency and surface visibility. Drawing an inner object through a transparent visor must not grant physical access through the visor. The current engineering views do not qualify that final material/occlusion behavior.

Procedural visual fitting must not override the fit validator. If a cap does not fit over a knit cap, rendering a stretched cap is not permission to equip it. Conversely, a compatible item must resolve to a usable appearance or an explicit stand-in rather than disappearing. The same equipped instance must retain its color, variant and fastening state in both paperdoll and world views.

## Scope and performance

Bake reusable garment/body/action layers and combine them as needed. Do not generate every possible full outfit in advance; combinations grow too quickly. Cache repeated layers and, where useful, composed frames with a bounded budget. Appearance keys should account for body variant, wearable art variants, fastening state, facing, action/frame and catalog version. Unobserved characters need no per-frame sprite generation.

Missing artwork should fall back to the existing readable diagnostic style: named wearable/body category, facing and animation-state information. It should remain possible to inspect a missing binding, identify its item/rig/state and replace the art without editing inventory logic. Final frame/body-family limits require an art qualification pass and are not frozen here.

The first qualification should use one body family, a base garment and an openable jacket: show both on the paperdoll, equip/fasten/open them through ordinary item actions and display the matching outfit across the current world facings and walk frames. Then add a pack to check straps, front/back mounting and occlusion. Compare the visual outfit with the authoritative equipped graph and verify unchanged movement, observation and save behavior. Full wardrobes, procedural cloth simulation and a general reconstruction system are outside that first slice.

## Layer inspection

Historical October 25 inspection UI: the selector described below was superseded by the [grouped equipment list and actual worn order](35-actual-worn-order.md), followed by physical sublayers. Preserve its distinction between knowing an item and having physical access when implementing the final sprite paperdoll.

The paperdoll now has a **Layers** selector for exposed equipment, all known worn equipment or one numbered layer. Layer inspection reveals known own equipment for identification; it is a presentation filter. Covered objects remain marked and cannot be dragged or manipulated through the covering. Opening their storage still requires the normal physical access path. The filter does not reveal items inside pockets or cases and does not change what other characters see in the world.

Changing this view costs no simulation time/AP, modifies no item/fastening state and is not saved as physical equipment state. Closing/reopening inventory resets it to exposed equipment. This inspection feature is implemented in the engineering slot UI; a future composited paperdoll should preserve the same access distinction.
