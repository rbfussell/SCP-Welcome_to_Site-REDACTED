# Badge-controlled test door

Implemented November 6, 2026. **User-confirmed rule:** an exposed worn badge can be read automatically; otherwise it must be **equipped in hand** before use. An accessible pocket, open backpack, inspection window or painted badge does not count as presentation. This extends the [physical badge/clip](54-shirt-pockets-and-badge.md), [coverage](31-pocket-coverage-prototype.md), [inventory transactions](29-gear-data-standard.md) and [tactical scheduler](20-event-log-and-tactical-training.md). See the [wiki](../source_material/game-wiki/badge-door-access.html).

## First playable fixture

The existing `door-7-5-u` is now a reader-controlled doorway. Its authored reader requires issuer `site-test` and clearance 1; the separate personnel badge supplies that issuer/tier. The badge remains inside the spare backpack at startup. Its kit definition now has immutable credential metadata; no new item, PNG or mutable credential registry is added. The other two doors remain ordinary doors.

The test uses provisional **bearer access**. Optional `BadgeCredential.SubjectId` can require a matching actor ID; both modes are checked. The user has been asked whether identity binding should be the default. Until resolved, bearer mode is a fixture choice, not settled facility security design. Clearance 1 and denied tier-2 variants are local test policies, not a comprehensive Foundation authorization model or SCP canon claim.

## Use and costs

| Situation | Result |
|---|---|
| Walking near a powered reader with an exposed worn valid badge | Door opens automatically during realtime approach, without an inventory action or movement stop. |
| Standing with exposed worn badge, press E | Ordinary door operation. Realtime remains immediate; tactical door use costs 2 AP and 300 local ms. |
| Badge directly equipped in the actor's hand, press E | Purchases a 500 local ms, 2 AP presentation action. Outside combat AP is quoted, not spent; tactical use pays it. Door opens only on completion. |
| Badge stored, covered, damaged, wrong issuer/identity or insufficient clearance | Closed reader refuses; status explains the requirement. Nothing is extracted automatically and no AP is charged. |
| Door already open | Ordinary close, subject to normal obstruction checks; no credential is required to close this test door. |

Automatic realtime reading applies only while the actor is moving and locally active. Standing idle does not repeatedly reopen a door just closed. Tactical input waits do not cause free opening; use E as a paid action. Range is the existing 1.35 world-unit interaction reach, with other solid walls and blocking cabinets checked. The reader itself is excluded from its own access-ray obstruction. This is a proximity test, not final optical reader orientation/range tuning.

Worn presentation requires a real attachment chain to the acting person's worn equipment and the badge's entire authored attachment occupancy exposed under the physical body-surface model. A badge clipped to a shirt sitting in a pack or hand is not worn. Closure windows and camera/alpha settings do not establish credentials. Physical exposure is authoritative; general optical visibility, reader facing, lighting and item-condition thresholds need later qualification. The current badge must have condition greater than zero.

The same credential service validates any actor's issuer, tier, physical wearing/hand and optional identity. The native fixture exercises player E input, shared NPC credential queries and automatic scanning of moving actors. Full NPC reader operation/planning, reader queues and escort/tailgating policies are not implemented. Once open, the doorway is ordinary shared physical space; the reader does not confer collision immunity or reserve the doorway for one person.

## Authority and continuation

`WallDefinition.Reader` optionally names `DoorReader(Issuer, Clearance, Powered)`. Power is an immutable test flag, not connection to a working electrical circuit. This specific fixture refuses opening without power. [Infrastructure design](17-facility-infrastructure.md) still requires device-specific fail modes and physical maintenance before production utilities; do not generalize this test flag to all doors.

`ItemDefinition.Credential` optionally names `BadgeCredential(Issuer, Clearance, SubjectId)`. It is immutable fixture content, separate from item tags/art and condition. The native profile currently validates tiers 0–5. Production credential issuance, revocation, lost/stolen status, expiration, remote identity verification and OSCP account authorization remain future work.

`InventoryActionKind.PresentBadge` and optional `InventoryRequest.TargetId` reuse existing paid handling, interruption, tactical Inventory scheduling and detached save validation. The badge stays in hand throughout. Completion rechecks physical reach, actor position, door state, power/policy and badge condition/presentation. Changing any required fact can abort without opening. A normal tactical Door action also rechecks the displayed badge; it cannot silently substitute a newly held badge for paid presentation.

Schema remains 15. The new enum value is appended and null TargetId is omitted. Pending presentation, open-door state and exact tactical/local time use existing snapshots. Invalid target/cost continuations reject atomically. Map and badge content identity change: **start a fresh scene**; old mismatched kits/maps reject rather than gaining access silently.

## Verification and limits

[Domain checks](../tests/SpatialChecks/BadgeDoorChecks.cs) cover visible/covered/stored/hand presentation, paused refusal, both clearance variants, issuer, power, damage, identity/bearer modes, NPC queries, interruption, normal and manual tactical costs, commitment rechecks and exact save/load. [Native checks](../game/Presentation/SpatialScene.BadgeDoorSmoke.cs) use real hand drag and E input, then inspect successful/denied states and automatic approach. Run:

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --badge-door-smoke work/preview/spatial-scene/badge-door.png
```

Full `--inventory-smoke` includes the reader fixture. [Actual results](reviews/badge-door-runtime-checks.md) distinguish passed checks from remaining limitations. World wardrobe art is still baked; physical credential wearing is read from the actual item graph despite that presentation limitation. No general SCiPnet, AIC, faction, door power network or final security UI is implied.

## Current jacket badge follow-up — November 6

[The real jacket clip](57-jacket-badge-clip.md) replaces the painted stand-in in new versioned art, accepts the same physical badge as the shirt and preserves all four pocket interactions alongside mounting. Existing reader policies remain authoritative. Earlier painted-badge/future-jacket statements describe their historical slices.
