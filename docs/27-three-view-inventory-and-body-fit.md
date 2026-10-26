# Three-view inventory, closures and body-shape fitting

Status: October 25, 2026. This records the user's revised interaction and metadata direction. It supersedes the earlier expanding-column inventory proposal and Esc-as-Back behavior. The current runtime still uses that earlier engineering browser; this document does not claim the three-view UI, illustrated hotspots or hex body fitting are implemented. Two implementation questions remain at the end.

Related: [physical inventory](23-personal-inventory-prototype.md), [body equipment](24-body-equipment-and-container-navigation.md), [wear layers and fit](25-wear-layer-and-fit-standard.md), and [appearance pipeline](26-wearable-appearance-pipeline.md).

## Exactly three views

| View | Shows | Navigation |
|---|---|---|
| Left: paperdoll | Equipped character and chosen layer inspection | Permanent top level while inventory is open |
| Middle: gear | One enlarged top-level object or the current garment in a worn-layer trail | Replaces its displayed object as the gear trail changes |
| Right: contents | Current compartment or nested container in a physical containment trail | Replaces its displayed contents as the trail changes |

There are never extra columns for further nesting. Opening a gum packet inside a jacket pocket replaces the right-hand pocket view with the packet view. Its parent remains in the contents trail for Up navigation.

Opening another top-level object from the paperdoll starts a new gear trail and clears the prior gear/contents trails. For example, selecting the backpack replaces the jacket view and dismisses its contents view. The paperdoll remains visible.

Each object view uses its actual object sprite with authored interactive regions. The backpack's compartments appear in the backpack illustration. Jacket pockets and the central fastening appear on the jacket illustration. The current state's flap, zipper or opening should be represented on that object. Grid overlays identify actual interactive/packing areas, rather than replacing the entire object with unrelated inventory cards. Animation is future presentation of these physical actions, using the same authoritative action state.

## Two kinds of ancestry

The middle trail describes layered wearing: jacket → shirt → T-shirt. These items retain their individual physical parents in the equipped graph; the shirt does not become contents of the jacket. Each downward navigation link must refer to an actually exposed lower garment at the relevant body location.

The right trail describes physical containment: jacket pocket → gum packet interior, or backpack compartment → secure case → ammunition box. Structural compartments may remain regions of their owner rather than portable item instances. Every displayed step retains its owner ID, region ID, expected placement and actual parent relationship.

The right trail is anchored to a particular object in the middle trail, not merely whichever garment is currently drawn there. Thus the jacket's pocket may remain visible while the middle view has descended to the shirt or T-shirt. Being retained in a trail never bypasses current physical access checks.

## Up, × and Esc

Both middle and right views have **↑** and **×** controls.

- **↑ in the middle:** remove its current gear step and display the preceding step. If the trail becomes empty, hide the gear view and its dependent contents. If contents belong to an object removed from the gear trail, clear those contents. Contents anchored to a retained outer garment remain.
- **↑ on the right:** remove its current contents step and show its containing step. If the trail becomes empty, hide the contents view.
- **× in the middle:** dismiss the entire gear trail and dependent contents trail.
- **× on the right:** dismiss the entire contents trail, retaining the gear trail.
- **Esc:** close the entire inventory screen. It is not a per-view Back action.

Examples required by the user:

| Gear trail before action | Contents owner | Action | Result |
|---|---|---|---|
| Jacket → shirt | Jacket pocket | Middle ↑ | Jacket returns; jacket pocket remains |
| Jacket → shirt | Shirt pocket | Middle ↑ | Jacket returns; shirt contents close |
| Jacket → shirt → T-shirt | Jacket pocket | Middle ↑ | Shirt returns; jacket pocket remains |
| Backpack | Bullet box inside a pocket/case | Middle × | Gear and entire contents trail close |
| Backpack | Bullet box inside a pocket/case | Right ↑ | Containing pocket/case returns on the right |
| Backpack | Bullet box inside a pocket/case | Right × | Entire contents trail closes; backpack remains |
| Jacket with pocket open | Jacket pocket | Open backpack from paperdoll | Backpack replaces jacket; old contents trail closes |

These are view-lifetime rules. Whether dismissing a view also triggers physical securing is an explicit unresolved question; it must not accidentally become a free zip/latch operation.

## Object actions and context menu

The proposed final input mapping is:

- **Left double-click:** Open the targeted object/compartment.
- **Left hold/drag:** Move an object through validated inventory handling.
- **Right-click:** Show a context menu immediately to the right of the pointer, clamped to the screen when necessary.

The context menu exposes actions appropriate to its target:

| Entry | Meaning |
|---|---|
| Open | Inspect the object or enter a selected compartment; perform a required physical opening first when that target is secured |
| Zip / latch / buckle / button / close / other authored secure verb | Secure that specific physical fastening or compartment through the ordinary timed/AP action service |
| Remove | Remove/pick up the object through a valid physical destination; invalidate its dependent views when removal takes effect. It does not delete the item |

The gear object's inspection surface and its physical closures are distinct targets. Opening a backpack's enlarged gear view does not unzip every compartment. Opening a pocket targets that pocket alone. An unfastened/open pocket can be displayed without a new fastening action; a secured pocket requires its own opening work. A container with no fastening exposes contents by inspection when access permits.

Opening the jacket's center fastening exposes the lower clothing in the paperdoll and allows the middle view to descend to the relevant underlying garment. A pocket opened on the right retains its jacket ownership if the jacket remains in the gear trail and that pocket is still physically accessible. Choosing among multiple exposed garments should use authored body-region/hotspot relationships rather than an arbitrary global item order.

If opening takes time or AP, contents are not exposed immediately. A pending view transition must check that its target still exists, remains accessible and belongs to the same inspection context when the physical work completes. Changing to another top-level object must not cause a late action completion to reopen the old object's UI.

Remove must report an unavailable destination, occupied hands, insufficient AP, covering equipment or other real blocker. It must not teleport an item away, silently discard it or grant free dressing changes. Exact destination selection is a UI implementation choice to resolve against the current single-hand prototype.

## Physical securing and presentation

Securing is a physical state change: zip, latch, buckle, button, fold a flap closed, close a lid, or another authored mechanism. It takes the mechanism's authored time/AP and affects access in all views and simulation services. Secured does not automatically mean locked or access-authorized; future locks/keys/permissions are separate constraints.

Closure animations consume action progress/completion. A visually closed flap must correspond to its physical closure state, and opening an illustration must not independently mutate gameplay. Each relevant state has an appearance binding or a readable diagnostic fallback until animation is supplied.

Garment fastenings and pocket fastenings remain independently addressable. Zipping a jacket's front changes coverage of the shirt/belt beneath. Fastening one pocket affects that pocket. A backpack's main zipper and side-pocket flap have separate states. Outer closure changes can invalidate inaccessible descendant views without altering the item hierarchy.

## Standard object metadata

Separate gameplay metadata from appearance registration while giving every item an explicit route to all of its applicable representations. Field names below describe the contract; they are not a newly frozen serialization schema.

| Group | Information |
|---|---|
| Identity | Definition ID, instance ID, display name, category/tags and content version |
| Physical quantity | Mass and, where applicable, material amount/condition; recursive contents counted once |
| Packed form | Physical envelope, square-grid occupancy/mask, thickness, allowed packing rotations and container capacity |
| Worn form | Body profile, body-surface hex footprints, allowed orientations, edge constraints, fit/clearance/stretch profile, layer and coverage |
| Support/attachments | Required contact/support regions, attachment anchors and functional requirements |
| Compartments | Stable region IDs, interior square grids, load limits, actual access paths and coverage requirements |
| Fastenings | Target/owner, closure kind, open/secure action verbs, costs, physical state and applicable access effects |
| Appearance | Loose-item art; gear-view sprite, paperdoll/world bindings, per-state interactive hotspots and animation states |

"Inventory size," "paperdoll size" and sprite dimensions are different concepts. Packed square cells describe storage occupancy. Worn body hexes describe fitting and contact. Sprite pixel dimensions describe rendering. Increasing an illustration's resolution changes neither packing capacity nor wearing fit.

Illustrated hotspots link to actual compartment or fastening IDs. Their shapes locate click targets on each relevant illustration/state; they do not create duplicate pockets or decide capacities from image pixels.

## Body hexes and directed boundary constraints

The user chooses **hexes for the body/worn-placement grid** while ordinary storage remains square. The body grid is a body-local shape model, independent of the terrain/navigation hex grid and its scale.

A worn footprint needs occupied cells and typed boundary edges, rather than only width/height or a wearable slot name. The proposed basic edge meanings are:

| Edge kind | Requirement |
|---|---|
| Closed/contact boundary | Must meet the corresponding exterior body boundary or an explicitly declared supported fitting boundary |
| Open boundary | Body may continue through it; examples include neck, arm, leg and garment-bottom openings |
| Attachment/support edge | Must match a declared support/contact surface, with the equipment's applicable compatibility rule |

For a simple exact-fit body mask, a closed boundary on an occupied garment cell must align with a body boundary on that same transformed cell/hex direction. If body cells continue outward through that closed edge, the placement fails. A required contact cannot float outside the body mask. An open boundary does not require an exterior edge: it permits the body to extend through the opening. Stretch/clearance tolerances, where supported, must be explicit profile data rather than silently changing the mask.

This explains the shirt example: closed shoulder and side boundaries must line up with the body's corresponding silhouette. Sliding the shirt down so the shoulders continue above its closed shoulder boundary is invalid. Neck/arm openings and an open bottom permit continuation without forcing those edges to become body boundaries. Image transparency alone is not enough to distinguish an intentional opening from an unregistered empty area.

A belt can declare its side contacts and leave other boundaries open. A rotated placement can be permitted if its transformed contacts and other fit rules still validate. Do not reject an unconventional orientation merely by checking the item's name. Placement validity and useful function are separate: attachments and effects must follow the actual orientation and their required support, rather than receiving a conventional belt position automatically.

Geometric fitting must be checked together with layer overlap, the complete worn stack, allowed fit allowances, physical coverage, total carried load and outermost equipment constraints. The intended direction is shape-driven placement, not simply a larger list of named shirt/pants slots. Body landmarks/support features may be needed where a coarse silhouette cannot distinguish two anatomically different locations.

Front/back body surfaces, sleeve/shoulder continuity and wraparound items need explicit mappings or seam/contact links. A front-only two-dimensional shape is not proof that a full shirt or belt fits the complete body. Body-family variants should share the standard but provide their actual shape/contact maps.

## Questions before implementation

1. **View dismissal and securing:** Are ×, ↑ and Esc navigation/dismissal only, with Secure as the physical action, or does closing a view also physically secure that container? If dismissal secures, its time/AP, multi-container ordering, partial completion and interruption behavior must be defined rather than making closure instantaneous.
2. **Orientations:** Exact rotations of an axial hex mask are multiples of 60°. A literal 90° vertical placement is not one of those six rotations. Should additional orientations such as 90° use explicitly authored/validated alternative footprints, or should the initial body-grid implementation use only the six exact hex rotations?

## Bounded implementation and acceptance

Implement view navigation separately from physical closure commands and from shape fitting. First qualify the three-view state machine with the jacket/shirt/T-shirt and pocket/case/packet examples above. Bind context actions and per-compartment closure metadata through validated simulation commands. Then qualify illustrated hotspot bindings and closure-state sprite swaps. Body-hex fitting needs a separate small mask/contact fixture before replacing the current mount validator.

Checks should cover exactly three view slots, both Up controls, both × controls, Esc closing inventory, top-level switching, retained outer-garment contents, removed-owner invalidation, physically blocked descendants, late-action UI cancellation, paid opening/securing and save/restore. Shape checks should include aligned/misaligned shoulders and sides, open neck/arm/bottom edges, translated/rotated belt contacts, unsupported orientations, full-stack fit and unchanged square-grid packing.

No earlier prototype limit, optional three-dimensional authoring pipeline or old slot implementation should silently override this revised design. Conversely, recording this contract is not evidence that the running browser, physical closures or body placement have already been replaced.
