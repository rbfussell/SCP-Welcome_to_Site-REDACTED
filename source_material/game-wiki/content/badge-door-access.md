# Badge-controlled door access

**Implemented bounded fixture, November 6:** an exposed worn valid badge can open the test reader automatically during realtime approach. Otherwise it must be **equipped directly in hand** and presented with E. Stored or covered badges do not count. This is the user's selected game rule, not an SCP canon assertion. See the [current guide](doc-docs-56-badge-door-access-md.html) and [actual results](doc-docs-reviews-badge-door-runtime-checks-md.html).

## Physical presentation

`door-7-5-u` requires issuer `site-test`, clearance 1. The existing separate personnel badge supplies that policy and starts in the spare backpack. [Shirt clip mounting](shirt-pockets-and-badge.html), [body coverage](doc-docs-31-pocket-coverage-prototype-md.html), [independent transfers](inventory-transfers.html) and [ground staging](ground-storage.html) remain normal physical transactions. Painted badge pixels, open views and an accessible pocket grant no entry.

The [credential service](file-src-sitesimulation-security-doorcredentialservice-cs.html) checks actual worn attachment ancestry and exposed occupancy, or direct actor-root hand placement. It validates issuer, tier, positive condition and optional assigned actor identity. Bearer mode is the **provisional test default**, pending the user's identity-binding choice; both modes are supported. The same service supports NPC queries without player authority overrides.

## Real time and tactical time

A moving realtime wearer in physical reader reach can open it without stopping. Idle actors do not repeatedly reopen a just-closed door. E with a worn badge uses ordinary door timing: immediate in realtime, 2 AP/300 local ms in combat. E with a held badge buys **500 local ms/2 AP** presentation through [normal item handling](system-inventory.html); AP is spent only in combat. The item remains in hand until completion. [Tactical input holds](system-combat.html) never grant free automatic opening.

Other walls/cabinets, paused/held local time, coverage, damaged/wrong credentials, inadequate tier and unpowered reader can refuse access. A closed reader reports denial through status. Once open, it is ordinary shared space; no reservation or per-person collision exemption is invented. Closing uses existing obstruction checks.

## Continuation and scope

[Schema-15 persistence](system-persistence.html) saves the existing door set and paid Inventory work. Appended `PresentBadge` uses optional request `TargetId`; detached restore checks the reader and canonical costs. Completion rechecks access, hand/display state, policy, power, condition and interruption. Map/kit immutable identity changes; use a fresh scene.

This specific reader's authored power flag refuses opening when off. Working circuits and device-specific power-failure policies remain [infrastructure design](doc-docs-17-facility-infrastructure-md.html). No production credential issuance/revocation, expiration, reader orientation/lighting model, NPC reader planning, escort policy, SCiPnet or OSCP account authorization is implemented. Current world wardrobe sprites remain baked and do not determine credential eligibility.

[Domain checks](check-badgedoorchecks.html) cover policy, presentation, NPC queries, AP/time, interruption and exact continuation. [Native checks](file-game-presentation-spatialscene-badgedoorsmoke-cs.html) exercise real inventory drag/E/approach. The [item catalog](item-personnel-badge.html) exposes the immutable credential field.

Related: [security service](system-security.html), [actors](system-actors.html), [world geometry](system-world.html), [presentation](system-presentation.html), [engine](engine.html), [ledger](status.html), [OSCP boundary](system-oscp.html), [mount points](mount-points.html), [appearance](wearable-appearance.html), [every depicted pocket](visible-pockets.html), [design library](documents.html).

[Real jacket badge clip](jacket-badge-clip.html) now combines four usable pockets with exterior mounting, badge-free source art and the existing reader checks.
