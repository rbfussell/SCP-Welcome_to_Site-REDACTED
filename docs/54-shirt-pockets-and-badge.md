# Shirt chest pockets and detachable badge

Implemented November 5, 2026: two real shirt compartments and a separate physical badge on a left-chest clip. Door credential access is **planned**, not implemented. See [pocket segments](53-paperdoll-pocket-segments.md), [mapped pocket coverage](31-pocket-coverage-prototype.md), [mount providers](37-placement-extenders-and-mount-points.md), [asset bundles](46-item-asset-library.md) and the [wiki](../source_material/game-wiki/shirt-pockets-and-badge.html).

## Pockets and clip

The native `shirt` definition now supplies independent `chest-left` and `chest-right` compartments. Left means the wearer's left, viewer-right on the front sprite. Each is a provisional 3×4 grid of 25 mm cells, 20 mm deep, 300 g maximum, buttoned, initially closed and eligible for per-fastening secure-on-view-close. Both mouths occupy authored front body cells; zipped coats obstruct them, open coats expose their physical mouths. Sprite alpha remains a separate gate: opaque jacket panels do not become transparent when opened. Use the enlarged shirt view when a physically accessible pocket is hidden on the small body illustration.

The distinct `badge-clip` region offers one `badge-clip` attachment contact above the wearer's left pocket. It has no loose-item inventory or lid. The 50×75×5 mm, 20 g `personnel-badge` is a separate item with a compatible connector, six attachment orientations, normal access, paid movement and source retention. It starts inside the spare backpack's open main compartment. Pocket contents and badge remain children of the shirt when the shirt moves. Shirts have two button menus; the badge has ordinary item actions, not an invented pocket fastening.

## Coverable attachments

`AttachmentProfile.CanBeCovered` defaults false. True permits a valid attached item to remain in the graph when clothing covers its contact; the badge uses it. It does not waive interface, support, collision, load, hierarchy, time/AP or restore checks. Existing hooks/pouches retain their previous exposed-contact graph rule.

`AttachmentSurface.RequiresExposedAccess` defaults false. The shirt clip sets it true: normal source/destination and ancestor access must find every offered contact exposed before manipulating that support. A badge cannot be attached through a zipped jacket, and a covered badge cannot be removed or used through the clip. These immutable fields extend the native DTOs, not the proposed public gear-card schema. No core code tests a badge ID to grant access.

## Art and presentation

A new generated two-pocket burgundy shirt sheet replaces only the shirt's paperdoll and loose/gear art registrations. Original atlas/source PNGs remain preserved. A separate generated badge sprite supplies loose and attached views. [Shirt provenance](../game/Assets/items/worn/clothing/shirts/work-shirt/PROVENANCE.md) and [badge provenance](../game/Assets/items/worn/accessories/badges/personnel-badge/PROVENANCE.md) record exact built-in imagegen prompts, sources and hashes. Source bytes/alpha are copied unchanged; metadata source crops and contour polygons provide the view registration.

`attachmentIllustrations` optionally supplies normalized `providerOrigin`; the enlarged gear surface uses it for both drawing and picking. The shirt's authored origin aligns its physical `(0,2)` clip to the illustrated left pocket. Omitted origins preserve the previous centre origin. Paperdoll composition uses its independent authored attachment plane and normal dressing order: outer jacket pixels hide a badge. Rendering never grants credentials. World clothing remains baked art.

## Badge access decision — selected design, not door behavior

The user selected **visibly worn badge required for automatic entry**. A covered or stored badge does not automatically authorize entry; a carried badge needs manual presentation. The shirt clip supplies physical wearing, not authorization. Later work must define readers/security observation, credential identity and validity, local permissions, presentation time/AP, denied access, offline/power behavior and who can actually observe a badge. Do not infer credentials from art, a `badge` tag, or possession alone. The existing painted badge on jacket art remains decorative and must never count as a real credential. Adding a jacket clip or replacing its painted badge is separate work.

## Continuation and evidence

The kit now has **20 definitions and 40 instances**, with two new mutable pocket states. Immutable content identity changes; start a fresh scene for this kit. Matching continuations use schema **15** with no new mutable save field; older mismatched kits reject rather than silently losing state.

[Domain checks](../tests/SpatialChecks/ShirtBadgeChecks.cs) cover independent pockets, capacity, coverage, covered retention/access, paid mounting, source retention, exact pending/covered continuation and loaded shirt removal. [Native checks](../game/Presentation/SpatialScene.PaperdollShirtSmoke.cs) use real pocket clicks, context menus, storage drags and badge-to-clip input with actual sprite picking and jacket occlusion. Run alone with `--shirt-pocket-smoke` or in pocket/full inventory modes. [Actual results](reviews/inventory-runtime-checks.md) record test runs, initial registration/fixture failures and warnings; wiki building does not run gameplay tests.

## Current visible-pocket contract

[Every depicted pocket is usable](55-visible-pocket-contract.md) supersedes earlier decorative-pocket limits and records the current region set, aliasing, art agreement and kit identity.
