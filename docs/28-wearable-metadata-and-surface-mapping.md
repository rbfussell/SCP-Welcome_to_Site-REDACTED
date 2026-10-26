# Wearable metadata, interactions and surface mapping

Status: October 25, 2026. User-confirmed direction: independent overlap, covering, removal blocking, attachment surfaces and coherent front/back appearance. The bounded runtime now separates access covering, removal blocking and declared same-layer overlap. Body-hex placement, continuous attachment surfaces and procedural world clothing remain proposed. The user confirms belt overlap with accessible pockets and blocked underlying removal, and sensible per-fastening securing defaults with player overrides.

Related: [body equipment](24-body-equipment-and-container-navigation.md), [fit standard](25-wear-layer-and-fit-standard.md), [appearance pipeline](26-wearable-appearance-pipeline.md), and [three-view inventory/body fit](27-three-view-inventory-and-body-fit.md).

## Independent wearable interactions

| Property | Meaning |
|---|---|
| Placement overlap | May these items occupy overlapping worn cells at this layer? |
| Covers access | Must this item be opened or removed to reach an underlying location, pocket or fastening? |
| Blocks removal | Must this item be removed before an underlying item can be removed/refitted? |
| Visual occlusion | Which underlying pixels/surfaces are actually hidden in this pose? |
| Attachment compatibility | Can another object mount to an offered point, strip or surface with sufficient fit/load/clearance? |

Access and removal constraints are independent. A covering without a removal block can be opened to permit an underlying item to be reached and changed. An item with a removal block must be removed; merely opening it does not clear that explicit dependency. A jacket can have both. Neither property alone determines whether another item can overlap it or whether an entire underlying garment disappears visually.

Current native metadata appends `CoversAccess`, `BlocksRemoval` and `OverlapTags` to `WearProfile`. Access/removal default to true to preserve the current fixture. Overlap permission must match both profiles; ordinary same-layer conflicts remain rejected. Fastening checks access coverage. Removal blocking persists for the worn item's footprint after opening. Fitting/removing through outer obstructions still uses normal timed/AP-bearing dressing commands.

The flags currently operate over coarse `WearMount.Coverage` regions. Region/surface masks and geometric body-hex overlap remain future work. A single occupied named mount still accepts one item; overlap tags do not create several occupants in that legacy mount. Crossed belts at distinct body placements need the body-shape implementation.

## Belt placement and support

The confirmed rule is local geometric fitting: transform a belt's shape and contact edges with its orientation, then test those transformed contacts against the body. Horizontal and angled placement use the same rule. Two belts across the chest can be possible if their contacts, overlap permissions, worn-stack fit and loads validate. Do not reject placement solely because the object's category is belt or its conventional location is the waist.

Concrete body silhouettes and orientation footprints remain to be qualified. This does not freeze an axis-aligned waist slot or claim arbitrary non-60° rotations of discrete hex masks already work.

Confirmed belt interaction profile: compatible belts may overlap each other, leave underlying pocket access open (`CoversAccess=false`), and prevent removal of clothing they restrain (`BlocksRemoval=true`). This settles the earlier ambiguous “does not block” wording as access rather than removal. A real belt instance/contact footprint and its attachment surfaces still need the body-shape fixture.

## Attachment surfaces

Attachment providers can be points, segments or continuous surfaces. A belt can offer a belt-type interface along its usable length rather than one isolated holster slot. A holster supplies the matching attachment requirement and occupies a valid location/span on it.

Continuous support does not mean infinite capacity: attachment footprint, clip width, occupied span, combined load, clearance, orientation and compatibility still apply. Rotating the belt carries its offered attachment surface and anchors with it. Functional effects inspect the actual support/orientation rather than assume conventional waist placement.

Packs, garments, armor and anomalous objects can provide other interfaces through this contract. Physical parenthood remains singular: a pistol in a holster attached to a belt is not duplicated in another inventory. Attachment capacity, packed interior capacity and body occupancy are separate, and mass is counted once.

## Representations of one item

| Representation | Describes |
|---|---|
| Inventory sprite and packing data | Loose/packed item and square-grid storage envelope |
| Paperdoll sprite and fitting data | Worn form, contact/coverage and linked front/back surfaces |
| World representation | That same worn item transformed for body, facing, pose and animation frame |

A belt's front and back are surfaces of one wrapping object. A backpack's bag, shoulder straps and front-visible strap segments share one item identity. Shirts and jackets need linked front/back/sleeve/shoulder surfaces. Placement, orientation and fastening update every representation from the same state.

Access covering, removal contact, attachment support and visual opacity can have different surface masks. A narrow strap can restrain a shirt without covering its entire pocket. A transparent surface can reveal an item while preventing physical access. Opaque sprite pixels cannot stand in for every physical rule.

## Proposed procedural route from paperdoll art

The next useful experiment is reusable body-surface/pose mapping. Define aligned front/back paperdoll texture coordinates for a body family, then provide a projection map for each world facing/action/frame. That map identifies source surface/coordinates for output pixels, with pose masks and draw depth. Shared maps can be authored once or baked from a rigged source body.

An ordinary garment supplies a fitted paperdoll texture/mask, supported states and template parameters. The compositor projects it through the body's shared pose maps. This could avoid drawing a new directional animation sheet for every shirt color or conventional garment. It is a proposed prototype, not a functioning automatic converter.

Wraparound bands need seam-linked coordinates so belt texture and attachment locations continue around the body. Pack straps use body-following surfaces/anchors, while the bag needs its own bulk/silhouette representation. Near/far arms, collars, straps and attachments need appropriate occlusion/depth ordering.

Body-surface projection alone cannot invent a thick coat's or bag's expanded silhouette. Such items need template geometry, silhouette extensions, authored overlays or custom directional assets. A front-only inventory illustration also lacks missing side/back information. The authoring contract must identify these requirements and provide readable fallbacks.

Qualify one body and one front/back garment across the current six facings/walk frames, then add a wrapping belt and strapped pack. Check seams, silhouette, pixel style, shading, pose alignment, stable anchors and paperdoll/world state agreement. This builds on the existing appearance proposal; it does not require a 3D runtime game or general cloth solver.

## UI dismissal and physical state

**Esc closes the interface only.** It does not zip, latch, button or otherwise secure inventory. Open containers stay physically open unless a physical action changes them. Already committed timed work may continue after the UI closes under the existing scheduler.

**↑/× use each fastening’s authored `SecureOnClose` default or player override.** There is no global setting. A jacket front stays open by default; its pocket flaps can auto-secure independently. The three-view browser prepares child-before-parent close intent through the normal physical action service; every actual closure pays time/AP, can fail, and persists independently of the interface. ↑ applies the closed step’s own preference. Preparing a sequence during pause does not physically close anything. Waiting for tactical AP does not end the player’s turn. Esc still performs no securing.

## Qualification boundary

Runtime checks cover all four access/removal boolean combinations, removal blocking after opening, mutual same-layer overlap, metadata validation and Esc leaving actual backpack/case states unchanged. Existing garment, layer inspection, packing, AP and save checks remain required.

The three-view browser, context menus, illustrated hotspot input, paid opening, automatic securing and saved continuation are now implemented in the bounded native fixture. Hover tooltips report current closure state; animated zipper/flap artwork, anatomical paperdoll compositing, body-local hex fitting, continuous attachments and procedural world clothing remain separate work.

The October 26 gear-view clarification extends attachment surfaces to the exterior of backpacks and other equipment. The middle gear illustration shows those actual mounted items; stored contents stay in the right-hand interior view. A strap's visual segment locates its interaction surface, while authored fit/support geometry and physical constraints decide which item can mount there. The [segmented gear contract](27-three-view-inventory-and-body-fit.md#segmented-gear-illustration-october-26-clarification) records hover contours, child selection priority and the current two-compartment prototype boundary.

Per-compartment obstruction will use the target segment's projected body-local access hexes, with independent access-cover and removal-restraint masks. A jacket over hip pockets therefore leaves lower cargo pockets usable; covering cloth elsewhere never marks every compartment inaccessible. This is the next body-geometry contract. The current coarse regional proof and implemented per-fastening state/behavior fields are qualified separately in the [spatial obstruction and closure section](27-three-view-inventory-and-body-fit.md#spatial-obstruction-and-per-fastening-policy-october-26-clarification).
