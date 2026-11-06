# Implementation ledger

This ledger describes the inspected source snapshot. Existing checks and reviews are evidence sources, not a fresh certification of every game feature.

| Area | Current implementation | Remaining work |
|---|---|---|
| SCP-650 | Native module; human/AI/any profile fixtures; direct sight; relocation/poses/RNG; complacency flags; witnesses; bounded recovery | Live human-attended cameras, general fear/doctrine, public sandbox, complete M1 acceptance |
| SCP-1499 | Design and art references | Mask interaction, domains, physical/perceived contexts, linked consequences, procedural population |
| Movement/perception | Hex routing, physical interpolation, doors, facing, lighting, acquired evidence and movement interruption | Broader swept/sub-tick qualification, general hearing and cross-zone effects |
| Operations/employment | Delivered voice/device orders; trolley; receiving conditions; real posts, relief, bounded overtime | Full professions, needs, hiring, calendar shifts and general logistics |
| OSCP | Native app registry, workstation/phone modes, acquired records, prepared phone orders, device access gates | General SCiPnet, production applications, resident AIC execution and provider integration |
| Inventory | [Current-tile floor piles](ground-storage.html), physical graph, nesting, closures, timed/AP actions, exact/scoped packing, [symmetric body fit and bounded spans](body-fit-cleanup.html), [direct stored-clothing fitting drops](body-drag-fitting.html), [front/back inspection and removal](body-inspection.html), [full-body sprite qualification and real base garment](full-body-paperdoll.html), pocket coverage, worn overlays, [hook/provider chains](mount-points.html) and shared exterior clearance | General crafting, material accounting, spillage, assembly packing envelopes, arbitrary depth offsets, [character-matched clothing/silhouette presentation](wearable-appearance.html) and protection effects |
| Security | [One badge-controlled test reader](badge-door-access.html), exposed worn or equipped-hand validation, issuer/tier/optional identity, paid presentation and continuation | Production credential administration, general reader planning, power circuits and OSCP account authorization |
| Combat | Local turns/AP, training equipment, autonomous training opponents, saved pending actions | Full injuries/death, ordinary hostility/factions, lethal SCPs, general weapon content and aftermath reveal |
| Facility utilities | Debug lighting and physical map/doors | Electrical circuits, batteries, HVAC, heat/air transport, utility maintenance |
| Persistence | Content-identified detached validation, native subsystem continuations | General module migration/removal, cross-domain continuation and broader compatibility |
| Regional game | Requirements and bounded intake fixture | Autonomous GOIs, network economy, world-map travel, production offsite simulation |

## Documentation drift found during research

Current snapshots use schema 15 for ground pile order and paid continuation. Matching schema-14 inventories migrate without ground placements; inventory saves below 14 remain rejected. Earlier prototype guides retain schemas 4–14 as historical context. The full-body follow-up added underwear (19 definitions/39 instances/9 fit profiles) and changed native kit identity; older-kit saves still need matching content. Ground storage adds no native items or new immutable kit identity. The wiki's current-state statements follow code; historical qualification reports remain available with their original context.

Older SCP-650 and code-map text lists shifts, inventory devices, or belt attachments as future work. Later employment/inventory source and guides implement bounded versions of these. A future-work sentence in an early document must be read alongside the later system page.

## Decisions that remain open

AIC identity, authority and canon/timeline; final Technical Services terminology; automatic complacency measurement; production utility routing UI; broad combat tuning; public scripting security and capabilities; production art coverage; cross-zone effects and transfer policies. This wiki records these questions without choosing new game rules.

Presentation follow-up: [engine-rendered paperdoll mosaic](paperdoll-mosaic.html) now covers the base groin beneath real clothing. The current accepted reference is shirtless with close-fitting tan fabric briefs, replacing the painted undershirt/loose shorts. The earlier featureless mannequin request remains unimplemented. Physical equipment and access rules are unchanged.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.

November 4 footwear follow-up: [worn-only alpha masks](worn-alpha-masks.html) qualify source-preserving front/back shoe cutouts. Rendering and picking share derived worn alpha; packing/drag art and physical access remain independent.

November 4 authoring follow-up: [the item catalog](items.html) indexed the then-current 19 native definitions under 30 type categories with representative artwork, current stats, explicit missing-art coverage and raw data. [Asset bundle organization](asset-library.html) documents future per-item directories and view/state mask names; runtime atlases have not moved.

November 4 belt follow-up: [waist belt/direct pouch sprites](worn-belt-and-pouch.html) add an eighth direct wearable binding and a bounded exterior attachment binding. Normal provider coordinates, six rotations, validated inspection and loaded paid removal remain authoritative; non-waist fits remain omitted. The [worn hook-chain follow-up](worn-attachment-chains.html) now adds the front nested chain. The item catalog now exposes qualified attachment art and worn source sheets.

November 4 transfer follow-up: [independent source/destination views](inventory-transfers.html) retain a backpack or pocket source while choosing other gear. Native direct hook/pouch transfers use normal paid handling with an occupied hand; reach, covering, closure and provider dependencies still invalidate unavailable sources. The later [ground-pile follow-up](ground-storage.html) now supplies bounded current-tile clothes staging.

November 4 ground follow-up: [ground storage and piles](ground-storage.html) add a permanent current-tile dock, square packing with overlapping ordered items, top-only access through nested ancestors, physical drop/pickup and schema-15 continuation with matching schema-14 migration. Floating parent-contained inventory windows remain a proposed production-UI direction.

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify four coat pockets, six trouser compartments and backpack main/side/front. [Every depicted pocket is usable](visible-pockets.html); front/back cargo views share storage. Coverage and independent paid fastenings still apply; animation remains future work.

[Shirt pockets and detachable badge](shirt-pockets-and-badge.html) add two buttoned chest compartments and a real clip-mounted badge. Covered retention and manipulation are separate; this changes native kit identity to 20 definitions/40 instances while retaining schema 15. [Badge-controlled door access](badge-door-access.html) now implements exposed wearing or equipped-hand presentation at one test reader; production credential administration remains future work.

[Current visible-pocket contract](visible-pockets.html): depicted pockets require real storage; multiple views share one inventory.

[Badge-controlled test door](badge-door-access.html): exposed worn credentials or equipped-hand presentation use physical inventory and shared security validation.
