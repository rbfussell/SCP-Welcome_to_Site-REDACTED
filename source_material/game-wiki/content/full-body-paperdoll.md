# Full-body paperdoll and base garments

**Implemented bounded fixture, November 2.** **I → Sprites** now shows one front/back reference body with real underwear, socks, trousers, shirt, work shoes, an open/closed coat and a field backpack. This follows the [original upper-body proof](paperdoll-sprite-proof.html). See the [runtime/data guide](doc-docs-42-full-body-paperdoll-md.html), [appearance contract](wearable-appearance.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [implementation ledger](status.html). No SCP canon or anomalous rule is changed.

## One physical outfit, multiple visual parts

[The appearance adapter](file-game-presentation-inventory-inventorypaperdollart-cs.html) selects actual own compatible worn mounts/default poses and committed fastening state. Stored, held, foreign and packed items are excluded. Manifest version 2 registers several PNGs, front/back source rectangles and destination parts on one canvas. A pair of socks or shoes has two independently positioned draw parts, with one physical item/parent/mass.

The [normal waist belt/direct pouch](worn-belt-and-pouch.html) extends this to eight wearable bindings and the original direct attachment binding; the [worn hook-chain follow-up](worn-attachment-chains.html) adds the front hook → pouch chain; other strap poses remain unqualified.

Source alpha, layering, optional per-part front/back selection and the same letterbox transform govern drawing, tint and picking. The backpack harness is drawn behind its mounted-side bag at the same shoulder scale as the opposite view. Transparent gaps/padding do not acquire cell fills or rectangular targets. The fixture's alpha cutoff stays 0.1; it is not a production material policy. Foreground head/neck or hair keeps collars behind the head. Qualified long sleeves use authored base-body cutouts to prevent stand-in arm/shoulder clipping. These suppress only the reference body, never real gear or physical cover, and are not a general transparent/torn-cloth solver.

[The native view](file-game-presentation-inventory-inventorypaperdollproof-cs.html) selects/inspects/opens normal context actions. Shoe removal reveals socks; sock removal reveals feet after paid completion. [Hex fitting](body-fit-cleanup.html), [body drops](body-drag-fitting.html), [inspection/removal](body-inspection.html) and [mount providers](mount-points.html) remain separate authorities. Other body pocket segments, arbitrary attachment artwork/strap poses, all body variants and world wardrobe composition remain future work. Unsupported worn gear is explicitly omitted.

## Base garment and honest visual fallback

`underwear` / `avatar-underwear` is now real cotton boxer briefs: 80 g, 100×100×15 mm packed, no storage, an authored pelvis/upper-thigh fit profile and linked front/back surfaces. It precedes trousers in actual worn order; covered removal refuses normally. The existing handler supplies time/AP, fit/bulk checks, descendant ownership, source retention and mass accounting.

The current base (`full-body-v2.png`) is **shirtless with close-fitting matte tan fabric briefs**. Their compact warm-tan silhouette replaces the original painted tank top/loose shorts without changing real equipment. These pixels are visual reference art, not items, protection or containers. Actual navy underwear can be removed while the engine mosaic/reference remains; the sprite cannot imply it is still equipped. The original PNG is preserved. The earlier featureless mannequin request produced no accepted asset; the user-selected clothed alternative is now accepted, with exact edit prompts/hashes linked from the runtime guide.

At this slice’s qualification, native content had **19 definitions, 39 instances, 9 fit profiles**; the current [shirt/badge kit](shirt-pockets-and-badge.html) has **20 definitions and 40 instances**, with the same nine fit profiles. [Persistence](system-persistence.html) was schema **14** at this slice’s qualification, but immutable content identity changes: older-kit saves need a fresh start. Matching new saves retain exact paid continuation. There is no automatic underwear injection/migration. The native crate is not enlarged; test-only helper-held outer gear provides valid staging for undressing checks without adding a player handoff feature.

## Evidence

- [Underwear domain checks](check-underwearchecks.html): base fit/layers/surfaces, covered access, loaded trousers, paid removal/mass and continuation.
- [Native outfit checks](file-game-presentation-spatialscene-paperdollsmoke-cs.html): source alpha, paired feet, front/back, resize, ordinary shoe/sock work and exposed feet.
- [Native underwear fixture](file-game-presentation-spatialscene-paperdollunderwearsmoke-cs.html): visible real briefs, inspection, paid removal, restore and the non-target modesty base.
- [Recorded runs](doc-docs-reviews-inventory-runtime-checks-md.html): actual results and limits; building this wiki does not run gameplay checks.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [appearance](wearable-appearance.html), [original art proof](paperdoll-sprite-proof.html), [body fit](body-fit-cleanup.html), [body drops](body-drag-fitting.html), [inspection](body-inspection.html), [mounts](mount-points.html), [design library](documents.html).

Presentation follow-up: [engine-rendered paperdoll mosaic](paperdoll-mosaic.html) now covers the base groin beneath real clothing. The current accepted reference is shirtless with close-fitting tan fabric briefs, replacing the painted undershirt/loose shorts. The earlier featureless mannequin request remains unimplemented. Physical equipment and access rules are unchanged.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.

[Worn-only alpha masks](worn-alpha-masks.html) now qualify intact loose-source versus masked worn-shoe appearance, shared drawing/picking and unchanged physical handling.

[Worn waist belt and direct pouch](worn-belt-and-pouch.html) now qualify default-pose front/back belt art and a real direct attachment, with normal access, paid handling and source-backed catalog registration. The [worn hook-chain follow-up](worn-attachment-chains.html) now qualifies the normal front hook → pouch chain.

Current floor/persistence follow-up: [ground inventory and piles](ground-storage.html) now supply persistent clothes staging and top-only pile access through ordinary handling. Current snapshots use schema 15, with matching schema-14 migration; schema-14 statements on this page describe this feature’s earlier qualification. This bounded floor feature supersedes any earlier ground-as-future statement, while world floor-item sprites and general hauling remain unimplemented.

[Worn attachment chains](worn-attachment-chains.html) now render and alpha-pick the normal front belt → hook → pouch assembly through shared composed frames. Physical access, paid handling, independent trails and loaded ground transfers remain authoritative.

[Sprite equipment drops](sprite-equipment-drops.html) now add free body-hex placement, exact exposed belt/hook targets and worn-sprite drag sources through ordinary paid handling. Unsupported poses retain footprint feedback and explicit missing-art disclosure.

[Worn container drops](worn-container-drops.html) now link visible backpack-bag and pouch material to real interiors through ordinary paid packing. Harness-only pixels do not store items; closed/full refusals do not fall through to body fitting.

[Paperdoll pocket segments](paperdoll-pocket-segments.html) now qualify four coat pockets, six trouser compartments and backpack main/side/front. [Every depicted pocket is usable](visible-pockets.html); front/back cargo views share storage. Coverage and independent paid fastenings still apply; animation remains future work.

[Shirt pockets and detachable badge](shirt-pockets-and-badge.html) add two buttoned chest compartments and a real clip-mounted badge. Covered retention and manipulation are separate; this changes native kit identity to 20 definitions/40 instances while retaining schema 15. [Badge-controlled access](badge-door-access.html) now implements exposed wearing or equipped-hand presentation at one local reader; production credential administration remains future work.

[Current visible-pocket contract](visible-pockets.html): depicted pockets require real storage; multiple views share one inventory.

[Badge-controlled test door](badge-door-access.html): exposed worn credentials or equipped-hand presentation use physical inventory and shared security validation.
