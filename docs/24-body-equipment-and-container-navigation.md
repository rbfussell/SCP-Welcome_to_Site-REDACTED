# Body equipment and container navigation

Status: October 25, 2026. The user confirms the paperdoll/container interaction and body-region/layer direction below. Navigation is implemented in the bounded [personal inventory prototype](23-personal-inventory-prototype.md). General clothing layers, attachment compatibility and front/back mounting remain design work; they are not implemented by this UI change.

## Looking inside storage

The paperdoll is the permanent top level. It shows what is worn or held, rather than exposing the contents of every garment and bag. Right-clicking trousers, a jacket, a shirt with pockets or a backpack opens that container's compartments to the right. Only actually inspected storage appears. Empty inspection history is the default whenever the inventory screen opens.

Opening a case from an inspected backpack adds the case view beside its parent. Closing the case view returns attention to the backpack. Closing a parent view closes its descendant views; the paperdoll remains. Independently inspected worn containers can coexist so items can be dragged between trousers and a backpack. The engineering view uses columns with horizontal scrolling confined to the storage area, keeping the paperdoll fixed. A later narrow layout can replace the current container view and use Back to restore its parent instead.

Closing an inspection view does not fasten a zipper. Conversely, shutting a physical compartment hides its contents and invalidates descendant inspection views. Opening, shutting, retrieving and repacking still follow authoritative access, time and combat AP rules. A child cannot be inspected through a physically closed ancestor. Moving an inspected container to another placement or leaving a fixture's reach dismisses its stale view. No inspected-container history is saved as physical inventory state.

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
| Backpack | Front or back body mount if permitted | One instance in one location, supported bulk/load and compatible underlying equipment |

Layer numbers describe ordering on overlapping anatomy, not universal compatibility. Profiles also need explicit conflicts, allowed mounts, required support and clearances. A higher number alone must not allow a helmet on a foot, a second pack in the same location or an incompatible harness over a sealed suit. Two backpacks can be supported through distinct front/back mounts when the actor's load and the equipment's bulk/fit rules permit it. View rotation reveals those actual mounts without duplicating any items.

Attachments use the same hierarchy as storage with specialized mount acceptance. A pistol belongs to its holster, the holster to the belt and the belt to the body. A tourniquet clip or handcuff pouch can have a different mount and access cost from a belt pocket. Equipment capability follows the validated attachment/access path rather than being granted merely by ownership.

## Dressing and access

Equipping or removing a garment must validate all its coverage claims atomically. Rejected changes leave the worn graph untouched. If fitting a chemical suit requires removing a backpack, the dependency must be explicit: remove the pack to a valid destination, fit the suit, then refit a compatible pack. These are timed/AP-bearing actions; no automatic, instantaneous undress/redress cycle is implied. A future assisted dressing command can plan those ordinary actions while explaining blocked steps and missing staging space.

Two clarifications are pending the user's answers:

- Whether covering garments can block access to inner pockets until opened or removed. Recommended design: author this per garment and its current fastened/sealed state, since an open coat and a sealed suit behave differently.
- Whether “nothing can go on that” means a backpack is the outermost item at its own front/back location, or the suit disallows outer equipment except explicitly permitted items. Both can be expressed by the same compatibility rules, but should not be silently treated as the same rule.

These choices do not block container navigation. They must be settled before implementing the general dressing/access validator. Content field names, layer counts, attachment counts and body-region lists are not frozen schemas by this document.

## Implementation boundary

The current body has the earlier single-item mount slots and issued trousers, coat and back-mounted backpack. The UI replaces automatic pocket/pack columns with explicit inspection and parent/child navigation. It does not yet implement coverage layers, garment overlays on a rendered body, a flip control, front mounting, belt attachment definitions or dressing dependencies. Existing physical capacities, mass, equipment readiness, manipulation scheduling and save identity are unchanged.

The future equipment slice should verify conflicting same-layer coverage, compatible socks/shoes, atomic multi-region suit fitting, blocked dressing dependencies, front/back distinction, recursive attachment load, inner-access policy and save/restore. It should also verify that both paperdoll and world sprites read the same equipped graph. Adding this design does not expand the current milestone's required content.
