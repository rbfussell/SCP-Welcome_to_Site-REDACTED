# Three-view inventory, closures and body-shape fitting

Status: October 26, 2026. The bounded runtime now implements three fixed engineering views, separate gear/contents trails, illustrated compartment hotspots, context actions, paid opening and automatic securing. Esc dismisses only the interface. ↑/× use each fastening’s authored default or player override; jacket fronts default to staying open. The [first body-local mask/contact fit slice](30-body-fit-prototype.md) now runs alongside existing mounts; the [mapped pocket access slice](31-pocket-coverage-prototype.md) now qualifies hip/cargo obstruction, while animated/composited appearance remains future work.

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

These view-lifetime rules are distinct from physical handling. **Esc only closes the interface and leaves physical containers unchanged. ↑/× consult each affected fastening’s `SecureOnClose` preference.** The authored default is overridden only for the specific gear instance/compartment the player changes. There is no global settings switch. A jacket front defaults to staying open during shirt/lower-layer navigation; its buttoned pocket can independently default to securing. Child closures run before parent closures through ordinary time/AP-bearing handling; no UI close supplies a free fastening. [Wearable metadata and surface mapping](28-wearable-metadata-and-surface-mapping.md) records the later clarification and separates overlap, access covering and removal blocking.

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

## Remaining body-shape qualification

**Body-shape qualification:** The user clarifies the rule: transform local edge contacts with orientation and accept placement wherever they fit the body, potentially including crossed chest belts. Concrete body shapes and extra orientation footprints will be worked out with that geometry; do not freeze a conventional belt slot or promise an already implemented arbitrary-angle solver.

## Bounded implementation and acceptance

View navigation and physical commands now live in separate components. The native fixture qualifies jacket → shirt and pocket → case/packet navigation; it does not yet contain a third T-shirt garment. Context actions and per-compartment secure verbs use validated simulation commands. The enlarged object illustration uses hand-traced source-image polygons, with only the hovered part highlighted. Compartment state appears in the hover tooltip/right-hand view; closure-state sprites/animations remain future art. The paperdoll is still a scrollable equipment-slot view, not composited body artwork. The separate [mask/contact fixture](30-body-fit-prototype.md) now validates authored body placement alongside named mounts. Replacing mount occupancy and implementing geometric coverage/attachments remain later slices.

Checks should cover exactly three view slots, both Up controls, both × controls, Esc closing inventory, top-level switching, retained outer-garment contents, removed-owner invalidation, physically blocked descendants, late-action UI cancellation, paid opening/securing and save/restore. Shape checks should include aligned/misaligned shoulders and sides, open neck/arm/bottom edges, translated/rotated belt contacts, unsupported orientations, full-stack fit and unchanged square-grid packing.

No earlier prototype limit, optional three-dimensional authoring pipeline or old slot implementation should silently override this revised design. Conversely, recording this contract is not evidence that the running browser, physical closures or body placement have already been replaced.

## Current securing execution

[InventorySecuring](../src/SiteSimulation/Inventory/Transactions/InventorySecuring.cs) stores an explicit close sequence independently of UI lifetime. × and ↑ prepare the sequence and dismiss the affected views; an existing manipulation/sequence must finish or be canceled before another sequence can be accepted. An entire gear dismissal includes its compartments and containers actually inspected in that gear session. It does not inspect an unseen item hierarchy. Top-level switching and Esc dismiss views without manufacturing a physical closure.

Each closure revalidates placement/access and purchases its normal time/AP. Prepared work waits through manual/local holds and for the player’s tactical turn/AP; it never ends a turn automatically. The progress line reports waiting work and Cancel handling clears it. An impossible per-step AP cost is rejected. Failed or interrupted work stops remaining closures without reopening already secured objects or refunding elapsed time. Explicit successful handling replaces unstarted automatic work. Pending handling, remaining close intent and per-fastening player overrides persist in outer save schema 12. Closing the interface does not discard them. The context menu’s “Secure on view close” checkbox applies only to its targeted fastening; it is not a global option.

## Segmented gear illustration (October 26 clarification)

The gear view depicts the equipment's exterior and physically mounted attachments. Loose items stored inside its pouches appear in the contents view after opening the appropriate compartment. The external view should not display all packed equipment or unrelated square click boxes.

A pouch, flap, zipper or top opening has a traced visual segment. Hovering highlights that segment's silhouette over the original material texture. Double-click opens its target; right-click offers that target's context actions. Pointer selection and highlight use the same source-image coordinates and the same crop, scale, padding and orientation transform as the sprite. Transparent margin and polygon bounding-box corners are not click targets. Only the hovered segment has an outline/tint; other parts retain their normal artwork.

The bounded implementation uses hand-authored polygons in [hotspots.json](../game/Assets/inventory-objects/hotspots.json), with source-sprite alpha clipping. The existing backpack has two functional regions, main and side. The side segment now follows the visible right-hand pouch; the main segment follows the exposed upper panel. The pictured front pouch, additional side pocket and straps do not acquire storage or attachment behavior merely from their appearance. Their gameplay definitions require separate authoring. Coat pockets/fastening, trouser pockets, case latch and packet use the same image-space registration.

Vision-assisted authoring can propose contours/label locations, followed by human review of the exact image and physical target binding. Runtime selection uses saved geometry. Pixel masks can replace/refine polygons for detailed silhouettes without changing physical container identity. Per-open/secured illustration variants should bind their own visible contours to the same stable physical targets.

External attachment surfaces are the next distinct target type. A strap can offer usable support geometry, compatibility, orientation, clearance and load limits. A grenade, light stick or injector mounts as a real child item through shared fitting/access/handling rules, then renders at that attachment's registered position in the enlarged gear illustration. Its own sprite takes selection priority where it occludes the supporting strap. Enlarging an illustration never enlarges physical mounting capacity. Continuous attachment fitting, attached-item rendering and world/paperdoll synchronization remain proposed; this pass implements the compartment/fastening selection surface.

## Spatial obstruction and per-fastening policy (October 26 clarification)

The names remain `CoversAccess` and `BlocksRemoval`. Their eventual geometry uses transformed body-local hex masks, not a whole-item covered flag or UI pixel dimensions:

| Target | Relevant overlap | Consequence |
|---|---|---|
| Trouser hip pocket | Closed jacket cover overlaps that pocket's required access cells | Expose the jacket opening before reaching the pocket |
| Trouser leg/cargo pocket | Its own access cells lie below the jacket | Pocket remains reachable, even while some trouser material is hidden |
| Shirt removal beneath a belt | Belt restraint overlaps required removal/support cells | Remove the belt before removing the shirt; adjacent pockets can remain reachable |

Each compartment's segmented position maps to an access footprint in its worn owner's body coordinates. That footprint follows the garment's placement, orientation and linked front/back surfaces. An active outer cover blocks the action where it intersects required access hexes at the same surface. Fabric pixels elsewhere do not make every pocket inaccessible. Removal restraints use a separate action footprint. Opening a jacket changes its active cover shape; removing a restraint clears the corresponding removal block. The visual compositor occludes actual overlapping surfaces while still drawing exposed leg pockets. None of these rules grant access merely because a layer-inspection filter draws a known covered item.

The current prototype still uses coarse `WearMount.Coverage` and per-compartment `AccessBody` regions. A synthetic hip/leg-pocket check confirms those regions already distinguish covered and uncovered pockets. Basic body/garment mask-contact fitting now runs in the [first fit slice](30-body-fit-prototype.md). The [coverage slice](31-pocket-coverage-prototype.md) now adds actual four-pocket trousers, transformed access mouths and closed/open cover masks. Partial clothing rendering and geometric removal remain future work.

Two different booleans must stay separate. `GarmentOpen`/`RegionState.Open` is physical state, changed only by normal time/AP handling. `SecureOnClose` is convenience behavior: author a sensible default on the fastening and allow a nullable instance/compartment override. A preference change performs no physical fastening and does not invalidate a paid action. Turning off a preference drops only that target's unstarted automatic closure, preserving explicit orders and already paid work. Explicit Zip/Button/Latch always remains available regardless of automatic preference.

The rule depends on purpose, not mechanism. A button-up jacket front can stay open by default exactly like a zipped jacket front. A backpack main zipper can default to securing while a different zipper does not. Defaults for a garment front are false and compartment closures true in the native profile; authored data can override either. One jacket's override does not change another jacket or its independent pocket flaps.

Menu text is derived from actual state: zipped → **Unzip**, unzipped → **Zip**; buttoned → **Unbutton**, unbuttoned → **Button**. `OpenVerb` and `SecureVerb` supply these labels. An open menu refreshes its action when physical work completes; a stale label cannot blindly repeat an already completed close. Generic top-level Open is inspection, while a segment's closure action addresses that exact fastening. Both choices still use ordinary access validation and time/AP rules when physical work is needed.
