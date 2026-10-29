# Fit compatibility and actual worn stacks

Status: October 27, 2026. User-confirmed replacement direction. This supersedes the fixed authored-layer assumption in the earlier [wear standard](25-wear-layer-and-fit-standard.md). The [fit-rule foundation](34-wear-fit-rules.md) separates compatibility from order, and [actual worn order](35-actual-worn-order.md) now implements saved paid dressing, local primary depths and the grouped equipment list. The [physical overlay slice](36-overlay-sublayers.md) now implements per-hex/surface bands, crossed straps and exposed contacts; this document remains the broader design contract.

## Separate fit from position

An item's ability to fit over other equipment is not its layer number. The player’s actual dressing order and chosen placement determine the stack.

- A shirt against skin occupies actual layer 0. A backpack over that shirt can occupy actual layer 1 despite being high-ranked outer gear.
- Shirt → uniform → jacket → backpack can occupy actual layers 0, 1, 2, 3.
- A sufficiently accommodating shirt can go over a light jacket; that does not authorize the same shirt over a parka or heavy jacket.
- Body armor can be above or beneath a compatible light jacket. The profiles and total fit must validate the chosen arrangement.
- Underwear over trousers is possible when its actual fit permits it. Its category does not permanently assign it to layer 0.

“Over” therefore describes accommodation/compatibility. It can use reusable fit families, bulk/clearance and rank ceilings, plus explicit exclusions. A single numeric sort cannot express all these relationships and must not decide rendering order. The example ranks (chemical suit 8, backpack 9) are authoring examples, not actual worn depths or finalized balance values. Overlapping backpacks cannot be stacked just by giving them another actual layer. Distinct front/back packs remain possible when their independent footprints, support and combined load permit them.

A valid final combination does not authorize inserting gear through an obstruction. An already worn outer pack prevents slipping a jacket underneath it. Remove the pack, dress, then put it back on. Choosing an actual stack position must not silently reorder or temporarily remove existing items. Every physical step uses normal paid handling and AP.

## Under-limit and same-layer subpositions

The user clarified that an under-limit of **3 includes the base garment once**. It counts simultaneous occupancy in the same local layer/band, not every garment deeper in the entire outfit.

| Same body hex and local band | Occupancy count | Belt limit 3 |
|---|---:|---|
| Trousers + one belt | 2 | Fits |
| Trousers + two belts | 3 | Fits |
| Trousers + three belts | 4 | Refused |
| Three belts against bare skin | 3 | Fits |
| Four belts against bare skin | 4 | Refused |

Belts can share the primary layer of the garment they lie over. Within that band their actual order is recorded as `layer_N`. Proposed display: trousers at `1`, first overlaid belt at `1_1`, second at `1_2`. Bare skin consumes no item slot; three belts against skin can use `0`, `0_1`, `0_2`. The spelling of these labels is a UI convention, not a storage capacity multiplier.

Limits apply **per overlapping hex and body surface**, not to the number of belts owned or worn anywhere on the body. Two chest belts crossing in an X over a shirt consume three positions at the crossing. A waist belt and one arm of that X over trousers consume three at their separate waist intersection. Three belts can therefore be worn without creating a four-item intersection. If all three belts overlap the same clothed hex, the count becomes four and fails. Front and back must be evaluated independently through their linked surfaces.

The under allowance belongs to the same-band overlay/fit rule being used. The base garment consumes one place; a base garment’s ordinary clothing-stack defaults must not accidentally veto all external straps. Applicable overlay allowances and compatibility must all hold when heterogeneous straps share a band. Combined bulk/load checks remain independent of the occupancy count.

One wrapping item can cross differently dressed regions. The runtime must resolve its position against each actual local band instead of assuming one named mount or one scalar depth describes every covered body hex. This is why the migration needs a worn-order/support representation and a per-surface stack query, not a relabeling of the current `Layer` field.

## Covering, restraint and exposed support

Access cover, removal restraint, visual occlusion and interference with a fastening remain distinct relationships.

- A crossed belt can leave cloth visible while restraining removal.
- A belt across a jacket’s opening can prevent opening it, even where it does not cover a pocket. Fastening-interference masks must be distinct from pocket access masks and ordinary removal flags.
- Only exposed support hexes may receive attachments. A lower belt’s covered contact cannot be used merely because its item is known in the equipment list.
- Existing attachments cannot be silently hidden inside an invalid support overlap. A proposed overlay that would cover an occupied required support contact must be refused until the attachment is moved/removed, or an explicit supported mechanical relationship is chosen.

These checks use transformed physical surface/contact masks and actual stack order, not sprite opacity or default ranks. Existing closed/open jacket masks and independent pocket mouths remain useful inputs. Opening or removing the obstruction exposes the support only after that physical action commits.

## Hooks, loops and nested providers

An attachment may itself provide support. A hook on a belt is a separate actual item with one parent, its own contact, and an offered attachment hex. A pouch hanging from the hook belongs to the hook; a pouch directly on the belt belongs to the belt. Their support stacks are distinct from the body’s clothing band, although their physical clearance can still conflict.

A key loop can offer an interface restricted to `keyring`, with its own secure state. A hook/loop does not create arbitrary loose-item inventory capacity. Connection compatibility, support span, occupied shape, load, closure and access are separate requirements.

Local support coordinates compose through the provider chain. Mass counts each descendant once. Moving the belt carries the hook and its contents without rewriting child identities. Future hook art must include the offset/depth needed to show the hanging pouch separately; merely assigning different parents must not permit physical interpenetration. Covered support, cycles, excess depth, unsupported load and global clearance still fail normal validation.

## Equipped view

Replace the current exposed/all/layer selector with an own-equipment list grouped by body location, showing actual primary layers and subpositions, for example:

```text
Chest
  2    Jacket
  1    Uniform shirt
  0    T-shirt
Lower body
  1_2  Overlaid belt
  1_1  Belt
  1    Trousers
  0    Underwear
Feet
  1    Shoes
  0    Socks
```

This is a proposed example; it does not add these extra garments or belts to the native catalog. A wrapping belt may appear in more than one location group while retaining one item identity and one mass contribution. Front/back links must identify the same object.

Known clothing can appear in this list even when covered. Entries distinguish “known but inaccessible” from exposed gear and explain the blocking item. They do not expose pocket contents or grant access through an outer layer. Selecting an accessible item opens its exterior in the middle view; compartments remain in the right view. The eventual anatomical paperdoll is another view of this same equipment graph.

Dropping onto an existing object addresses that object’s supported function. An open backpack’s default region receives a normal container drop; a compatible pouch dropped onto a belt requests free exterior support. Equipping/repositioning clothing on the body is a separate fit/order intent, with an expected default body position and an explicit choice when more than one position is possible. A future item with both an interior and multiple exterior surfaces needs explicit segment/target selection when the drop is ambiguous.

## Runtime slice and acceptance boundary

The October 27 follow-up fixes mounted sprite anchors and exterior drops from the equipped view. Drawing/picking now share an authored source anchor and six-direction transform; dropping onto the belt quotes a deterministic free compatible support at the chosen orientation, with ordinary timed movement and no rearrangement of existing children. The enlarged gear view still allows precise support selection. Existing open-backpack drops use the normal packing service.

The replacement rank/stack runtime should next qualify one small authoring set: shirt, compatible light jacket, incompatible bulky coat, outer pack and multiple belts. It needs:

- Fit compatibility separate from actual worn order, including reversed valid combinations.
- An explicit same-layer band/overlay relationship and local subpositions; limits count a base garment once.
- Crossed-belt checks at both surfaces and at each overlap, including an actual four-item refusal.
- Coverage/removal/fastening/support checks against actual stack order; occupied support cannot be covered for free.
- Normal time/AP, interruptions, mass and atomic invalid-action rejection.
- Persistent worn/support order and detached restore validation; immutable fit definitions contribute to content identity. Adopt a new save revision when these instance fields exist, with an explicit migration/fresh-save policy.
- The grouped equipment list backed by the same authoritative stack projection, preserving knowledge/access boundaries.

The current fixed-layer runtime, its selector and its save fields remain qualified by their existing tests until that replacement slice passes. Three belts, hooks/key loops, variable clearance, full body drag-fitting and procedural world clothing are not implemented by the alignment/drop fix.

## October 29 foundation boundary

The [first fit-rule slice](34-wear-fit-rules.md) implements directional families, relative bulk, aggregate clearance and explicit exclusions, including a bridge into the existing ordered equipment graph. It does not yet replace the order representation, derive local bands/subpositions, add crossed belts, change the layer selector or enable dressing in arbitrary order through the UI. Those remain the next staged migration. The pure fit evaluator accepts a proposed outer/under relationship without consulting `Layer`, so the future per-surface worn-stack projection can reuse it.

## October 29 primary-order migration

The [primary-order slice](35-actual-worn-order.md) supersedes the fixed-order/runtime-selector statements above: saved dressing precedence now drives coverage, fit, access, removal, underlayer browsing and the equipment list. It uses local anatomical regions for primary depth. Per-hex same-band counts and `layer_N`, multiple crossed belts, fastening-interference masks and attachment-support exposure are still separate work. Schema 14 requires a fresh inventory scene rather than inventing past dressing order.

## October 29 overlay migration

The [physical overlay slice](36-overlay-sublayers.md) supersedes the pending-band statements above. Bands now count the base once at each hex/surface; native crossed straps, a symmetric reference X, mixed allowances, exposed mounting contacts and fastening interference are qualified. Normal primary depths and fit also use physical intersections. The native body silhouette, hook art, global cross-provider clearance and world outfit compositing remain outside this slice.
