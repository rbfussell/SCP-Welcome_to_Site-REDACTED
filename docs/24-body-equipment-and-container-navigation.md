# Body equipment and container navigation

Current follow-up: [front/back body inspection and removal](40-body-inspection-and-removal.md) now qualifies diagnostic worn-material picking and paid body-to-storage drags. The earlier fixed-layer/slot implementation and lack of a flip view described below are historical; final anatomical clothing art remains future work.

Status: October 25, 2026. The user confirms the paperdoll/container interaction and body-region/layer direction below. Navigation is implemented in the bounded [personal inventory prototype](23-personal-inventory-prototype.md). A bounded data-driven clothing-layer, coverage, fastening and front/back-pack slice is now implemented too. Belt attachment content, anatomical/composited paperdoll art and general clothing effects remain future work.

## Looking inside storage

The [three-view inventory and body-fit contract](27-three-view-inventory-and-body-fit.md) replaces the expanding-column browser. The runtime now keeps paperdoll equipment on the left, one gear/clothing illustration in the middle and one compartment/nested interior on the right. Double-click opens; right-click offers context actions. Only deliberately inspected storage appears; the screen starts with empty inspection trails.

The **Layers** selector can show exposed equipment, all known worn equipment or one numbered layer. This lets the player identify covered clothing without uncovering it. Covered objects remain inaccessible for dragging/manipulation and their storage still follows physical access rules. This is an inspection projection of the player's own equipment, not a query into nested storage or another person's hidden outfit. Reopening inventory resets the filter. [Wearable appearance](26-wearable-appearance-pipeline.md) records how this distinction should carry over to the future composited paperdoll and world sprites.

Opening a case replaces the current right-hand contents while retaining its containing compartment for ↑. Middle navigation follows exposed clothing layers; a jacket-owned pocket may remain visible while the middle shows the shirt. Middle ↑ keeps contents whose owner remains in the gear trail and clears contents owned by a removed gear step. A new top-level gear object replaces both trails.

× closes its entire trail, including dependent contents when closing gear, and prepares automatic securing only for fastenings whose per-piece preference enables it. ↑ closes its current step and follows the same policy. Child closures precede parent closures through ordinary time/AP handling. Each fastening has an authored default and a player-adjustable override in its context menu; no global setting is used. Jacket fronts stay open by default while pocket flaps can independently secure. Esc closes only the entire interface and leaves physical closures unchanged. Timed work can continue after the interface hides, and queued securing persists independently of view history. A child cannot be inspected through a physically closed ancestor; covering, movement and fixture reach invalidate stale views.

## The body as equipment root

The body is an equipment-bearing root, consistent with the existing `person:avatar` root. Its equipment locations are specialized mounts, distinct from ordinary packing grids. Worn garments remain children of that root; their pockets remain regions belonging to the garment. Wearing shoes over socks does not make the socks contents of the shoes.

Each worn instance has one physical parent and one identity. Its wear profile can claim several body regions simultaneously. A full-body suit therefore makes multiple occupancy claims without becoming several independently removable items or being counted repeatedly in mass.

Suggested anatomical coverage is head, face, upper torso, arms, hands, lower torso/legs and feet, with left/right subdivision where useful. Front/back are mount locations and presentation faces, not separate people or inventories. The exact region vocabulary is proposed until the equipment slice needs it.

| Example | Coverage / anchor | Layer or attachment rule |
|---|---|---|
| Pair of socks | Both feet | Base layer 0; another conflicting pair cannot occupy it |
| Pair of shoes | Both feet | Layer 1; compatible socks can remain beneath |
| Shirt and jacket | Upper torso and relevant arms | Different compatible layers; neither supplies arbitrary extra body capacity |
| Chemical suit | All regions covered by that particular design | Outer protective layer, for example 3 or 4; integrated hood/gloves/boots must be declared rather than assumed |
| Belt-mounted holster or first-aid pack | A compatible mount on the worn belt | Child of the belt; mount limits and combined mass apply |
| Backpack | Front or back body mount if permitted | Outermost at its mounting location; nothing may cover it while worn. Supported bulk/load and compatible underlying equipment still apply |

Layer numbers describe ordering on overlapping anatomy, not universal compatibility. Profiles also need explicit conflicts, allowed mounts, required support and clearances. A higher number alone must not allow a helmet on a foot, a second pack in the same location or an incompatible harness over a sealed suit. Two backpacks can be supported through distinct front/back mounts when the actor's load and the equipment's bulk/fit rules permit it. View rotation reveals those actual mounts without duplicating any items.

The user further confirms layer 0 as the base position (underwear, socks, base headwear and glasses at their respective locations), with ordered outer headwear/eyewear and construction-dependent fit. [Wear layers and fit standard](25-wear-layer-and-fit-standard.md) records the cap → knit cap → helmet and glasses → compatible goggles/mask examples, the reverse-order rejections and a proposed reusable fit-profile authoring method. Numeric order alone is insufficient. This is the next fit-content contract; current fixture values and runtime compatibility are not silently changed by documenting it.

Attachments use the same hierarchy as storage with specialized mount acceptance. A pistol belongs to its holster, the holster to the belt and the belt to the body. A tourniquet clip or handcuff pouch can have a different mount and access cost from a belt pocket. Equipment capability follows the validated attachment/access path rather than being granted merely by ownership.

## Dressing and access

Equipping or removing a garment must validate all its coverage claims atomically. Rejected changes leave the worn graph untouched. The user confirms that a worn backpack is outermost at its mounting location and cannot be covered by another worn item. Fitting a chemical suit therefore requires this explicit sequence: remove the pack to a valid destination, fit the suit, then refit the pack over it. A suit cannot simply be placed over the worn pack. Front and back packs obey this rule independently; a full-body suit must clear every conflicting pack location before dressing. Ordinary fit, compatibility and load limits still apply after the suit is worn.

These are timed/AP-bearing actions; no automatic, instantaneous undress/redress cycle is implied. A future assisted dressing command can plan those ordinary actions while explaining blocked steps and missing staging space.

Confirmed access rule: physically covered equipment and pockets are inaccessible. The player must expose them before inspecting or manipulating their contents. Supported garments can be opened while remaining worn: opening a jacket reveals the relevant clothing beneath it on the paperdoll and enables access to the newly exposed pockets or mounts. Closing it covers those locations again. If a garment cannot expose the required location by opening, it must be removed or otherwise changed through a supported action.

The paperdoll and access validator must read the same physical coverage/fastening state. Access depends on which region is exposed, rather than granting access to every inner layer because some part of a garment is open. Merely closing a UI view, changing the displayed layer or recalling an item's location does not uncover equipment. Previously open views of newly covered storage must be hidden or dismissed, just as views through a newly closed container are now invalidated.

Opening a worn jacket is a physical clothing action with handling time and combat AP, distinct from opening its pocket or displaying its inspection view. Its definition supplies which regions become exposed in each supported state. Physical covering refers to equipment coverage, not whether the current camera angle or a UI panel happens to draw the object.

Both access/outermost clarifications are now user-confirmed. Outermost applies at the pack's mounting location, rather than forbidding compatible equipment elsewhere on the body. Content field names, layer counts, attachment counts and body-region lists are not frozen schemas by this document.

## Implementation boundary

The body retains named single-item mounts, now supplemented by shared anatomical occupancy and layer validation. Authored profiles support ten body regions and layers 0–9 in this bounded prototype. Access covering (`CoversAccess`), removal blocking (`BlocksRemoval`) and mutual same-layer overlap (`OverlapTags`) are separate metadata, as detailed in [wearable metadata](28-wearable-metadata-and-surface-mapping.md). Existing access/removal defaults remain true; the confirmed belt profile permits compatible belt overlap, leaves pocket access open and blocks removal beneath. Body-hex belt geometry and actual attachment content remain unimplemented. `WearProfile` defines allowed mounts, their coverage, layer, outermost status, supported opening exposure and fastening costs; `StorageRegion.AccessBody` identifies the body regions needed to access a worn pocket. These are native content fields, not a public mod API. Packed envelopes remain separate from worn coverage.

The fixture has a layer-1 shirt/trousers, layer-0 socks, layer-1 shoes, a layer-2 coat, a layer-4 chemical coverall and layer-5 front/back packs. The coat begins open. The supply crate contains the coverall, spare socks and a second backpack. The suit claims its regions as one instance. Load accounting remains the ordinary recursive inventory calculation.

**Open/Fasten selected garment**, **Take selected in hand** and **Wear selected (default mount)** use the common timed/AP-bearing action service. A front pack can be fitted by dragging to that mount. Coverage takes effect only on completed handling; final commit revalidates the request. Compartment lids and garment fastening are independent states. The paperdoll hides fully covered garment objects and labels their slots Covered; partially exposed items can be drawn while inaccessible covered pockets remain locked. Inspecting a remembered item cannot bypass coverage.

Outer save schema 10 persists garment fastening, per-instance/compartment securing preferences, paid continuations and automatic securing intent. Coverage definitions and the expanded loadout change content identity, so earlier inventory fixture saves require a fresh start. Matching scenarios without inventory retain their earlier supported restore paths. Inspection history still remains presentation state.

The engineering paperdoll is a scrollable equipment-slot panel, not yet a composited anatomical figure or a front/back flip view. World actor clothing sprites do not change with equipment yet. The chemical suit currently demonstrates coverage/access/dressing, not chemical protection, air supply or environmental safety. Belt attachments, general clothing bulk/size compatibility, protection/warmth, garment damage and staged multi-action dressing plans remain future work. Numeric layer profiles and current packed dimensions are test content rather than final balance.

[Wear checks](../tests/SpatialChecks/WearChecks.cs) qualify socks/shoes, overlapping layers across different slots, outermost constraints, coverage and exposure, explicit suit/pack ordering, front/back dependencies, paid fastening and detached restore. The native Godot inventory run additionally clicks the actual garment/dressing controls, verifies blocked/hidden descendant views, and renders the fitted suit state. This qualifies the bounded equipment slice without claiming all future clothing or logistics systems are complete.
