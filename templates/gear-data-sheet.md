# Gear author sheet

Contract: [gear data standard v1](../docs/29-gear-data-standard.md), [JSON Schema](../schemas/gear-definition.schema.json). Copy a matching [example card](../examples/gear/backpack.json); remove inapplicable optional sections. These cards are design fixtures, not yet native-loadable mods.

## Identity and physics

- Namespaced definition ID / human name / description:
- Source, adaptation notes and provenance:
- Empty mass (grams), materials and fractions (sum to one):
- Special classes/traits; what consumers use them:
- Condition scale/default; use/damage response handlers:
- Knowledge policy (what the actor can know):

## Packed forms

For each form: ID, physical width/length/depth in mm, reference cell size, square footprint/mask, inventory sprite, transition action/cost. Storage has only 0°/90° orientations. State the default. A folded shape differs from its worn shape; switching must validate capacity rather than resize through neighbors.

## Compartments and fastenings

For each region: stable ID/name, storage or service-slot/feed kind, grid/cell/depth/mask or interface/count, load limit, default open/access rule, retention (`snug`), access time/AP, default-drop choice if appropriate, spill/function profile. Compartments belong to the item; detachable pouches are child items.

For each closure: ID, target region(s) or `garment-front`, mechanism, current-state verb pair (Zip/Unzip etc), default physical open state, default secure-on-view-close behavior, time/AP. Mutable physical state and the player's per-fastening preference override belong to instances. Do not duplicate authoritative booleans across closures and compartments.

## Worn fit and attachments

- Body family / surfaces / provisional order / front and back geometric fit:
- Fit family / relative bulk / maximum combined underlying bulk / accepted underlying families / excluded underlying traits:
- Six-orientation hex footprint, contact edges and open edges:
- Covers access mask / blocks removal mask / overlap permissions / outermost rule:
- Overlay allowance (base garment counted once), or ordinary primary garment:
- Blocks underlying fastenings / own fastening footprint and surface:
- Which openings expose which pocket mouths or underlying regions:
- Offered attachment interface/surface, transformed anchors, occupied span and load:
- Required attachment interface if this is a child attachment:

Compatibility is directional and independent of current worn depth. Use the [fit-rule foundation](../docs/34-wear-fit-rules.md) for units, evaluation and its current migration boundary. Same-band strap counts and geometric contacts are separate from the fit-bulk budget.

[Physical sublayers](../docs/36-overlay-sublayers.md) count only simultaneous items in the current band at each transformed hex/surface. A later primary garment starts a new band. Required attachment contacts must remain exposed; crossing an occupied contact is refused until its child is moved. These rules are independent of pocket access coverage and of whether an object is known in the equipment list.

## Art and interaction

For [placement extenders](../docs/37-placement-extenders-and-mount-points.md), author required contacts/occupancy and offered points separately in provider-local coordinates. Offered and required interfaces may differ. Name each offered surface, load limit and category restriction; do not invent a loose-item pocket or lid. Composed poses, exposed contacts and shared exterior clearance apply throughout the assembly. Only immediate connector contacts are exempt from collision; parentage grants no extra space.

Inventory sprite; doll front/back; world/pose binding; state art. For each segment: view, sprite, normalized contour, linked compartment/closure/attachment ID and body-surface mapping. Use one transform for drawing and picking. An opaque pixel is not automatically a physical obstruction.

## Use, assembly and extensions

Actions/functions: namespaced handler, requested host capabilities, required traits and parameters. Describe ordinary access vs a service-installed relation. An assembled gun's magazine slot and its magazine's feed are functional containment; crafting/disassembly records are separate. State crafted/deconstructable flags and any recipe/disassembly handler. Namespaced extensions supply extra data; new behavior still needs a supported evaluator.

## Instance and validation checklist

Unique instance ID, definition/form, one actual parent/relation/location/orientation, condition, compartment state, closure state/preferences, provenance and namespaced module state. Count recursive mass once. Preserve identities through transfers. Never reveal hidden anomalous properties automatically.

Validate with `python3 tools/check_gear_data.py`. Native content adapters, art/handler existence, geometric fit, replay and conservation require their own runtime checks; a valid card alone does not implement the behavior.
