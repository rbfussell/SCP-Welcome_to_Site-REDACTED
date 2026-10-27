# Belt and attachable utility pouch

Implemented October 26, 2026. This slice adds one utility belt and one utility pouch to the existing Godot inventory fixture. It qualifies physical mounting, not the complete wearable authoring system.

## Try it

Start a fresh scene. Approach the supply crate, open **I**, and inspect **Nearby supply crate**. Both objects are in its lower rows. Select the belt and **Wear selected**, then take the pouch in hand. Select the worn belt and inspect it.

- Drag the pouch from the hand onto the belt's webbing. The exterior uses hidden local hexes; the preview shows the exact snapped pose. **R** cycles six mounting orientations while over the support. Release starts paid handling.
- Double-click the mounted pouch to inspect its exterior in the middle view. Double-click its flap to unsnap it through normal timed handling. The right view then shows its 3×4 square interior.
- Place a dressing packet or another physically fitting object inside. Square storage still has only horizontal/vertical rotation. A failed precise grid drop never rearranges contents.
- **↑** on the pouch returns to the belt. Its default auto-secure preference queues paid snapping; **Esc** still only hides the interface.
- Drag the mounted pouch onto another supported belt position to reposition it, or into the empty hand to detach it. Its contents remain its children.
- In **Body fit · hex view**, select the belt. Its default anchor is `(-5,11,0)`; `(-3,9,2)` is a valid diagonal reference pose. Apply buys normal fitting time/AP. Gold cells show attached occupancy following the draft; actual state changes only on completion.

The existing slot boxes remain engineering controls. Finished equipment access still targets the front/back paperdoll.

## Data and physical ownership

[Native definitions](../src/SiteSimulation/Inventory/Content/field-kit.json) now contain **17 portable definitions, 33 item instances and 26 compartment states**. The belt's exterior support deliberately has no compartment closure state. The pouch has its own independent flap and interior.

| Field | Contract |
|---|---|
| `StorageRegion.Attachment` | Marks an exterior support rather than square storage: connector interface plus supported local hex cells. Existing mass/access fields remain authoritative. |
| `ItemDefinition.Attachment` | Required connector interface, occupied local cells, and contact cells which must actually land on support. All contacts belong to occupancy. |
| `ItemPlacement.AttachmentPose` | Parent-local anchor and sixth-turn rotation. Requires an actual item support, compatible connector and legal contacts. Cannot mix square coordinates/rotation or direct body pose. |
| `WearProfile.RemovalRegions` | Optional coarse removal-restraint regions, separate from visible/access coverage. This belt can restrain shirt/trousers without claiming to cover their pockets. |

[InventoryAttachments](../src/SiteSimulation/Inventory/Equipment/InventoryAttachments.cs) validates connector compatibility, bounded six-direction coordinates, support contacts and collisions with other children on that support. The region's load limit sums complete child assemblies. Existing hierarchy validation prevents self-containment, cycles, excess depth and excess carried mass.

A mounted pouch has exactly one physical parent: the belt. Moving the belt does not copy or rewrite child locations. Its effective body position composes the parent body pose with the local attachment pose. Taking off or storing the belt carries its children. Contents and attachment mass count exactly once. A stored or held belt gives its pouch no worn body position.

Mounting, repositioning, detaching, opening and storing all use ordinary validated inventory actions. They charge source/destination handling costs, preserve the source until completion, recheck access and fit at commit, and consume AP during tactical combat. Pause/holds grant no free attachment. Inventory ancestry still enforces fixture reach, ownership, covered support, closed outer containers and equipment readiness.

The belt's `CoversAccess=false` and `BlocksRemoval=true` are independent. Current removal restraint remains a coarse declared clothing relationship, including the shirt's restraint at the belt; it is not inferred from an unfinished cloth mesh. Body contact geometry nevertheless checks the belt's actual transformed endpoints instead of hardcoding a horizontal waist position.

Square Arrange/AutoPlace, Open/Close and closure preferences cannot turn a support into a bag. The pouch interior uses the existing packing and closure systems normally.

## Presentation and save boundary

[Attachment presentation](../game/Presentation/Inventory/InventoryGearSurface.Attachments.cs) draws and alpha-picks mounted object sprites over the support. Child picking takes precedence over the belt's segment. Exterior attachments descend through the middle gear trail; interior containers continue through the right trail. There remain exactly three views.

[Body-fit content](../src/SiteSimulation/Inventory/Content/body-fit.json) adds the belt's five-cell strap and two end contacts. The nine-shoulder diagnostic body is unchanged. New transparent belt/pouch art uses the built-in image tool; original PNGs, alpha-preserving atlas registrations and [exact prompts](../game/Assets/inventory-objects/attachment-prompts.json) are committed. There is no full world outfit compositor or fastening animation in this slice.

Outer save schema is **13**, preserving local attachment poses and pending work. Matching saves resume mid-mount with identical cost and outcome; malformed positions and forged costs fail detached restore without changing the running world. The fixture content identity changed, so earlier field-kit saves require a fresh scene. SCP-650 module schema remains 2.

## Verification and bounds

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/inventory-attachments.png
```

[Domain checks](../tests/SpatialChecks/AttachmentChecks.cs) cover interfaces, independent body/contact references, six mounting orientations, invalid mixed coordinates, collision, recursive loads, normal access, paid timing/AP, continuation, cancellation, invalid restores and loaded parent movement. [Native checks](../game/Presentation/SpatialScene.AttachmentSmoke.cs) use real mouse dragging, R, flap clicks, square-grid drops, ↑, paid diagonal body fitting and loaded detachment. Existing wear, packing, body-fit, coverage and inventory suites remain required.

This fixture has one belt mount, one exterior support and one pouch. Multiple crossed belts, dynamic assembly packing envelopes, attachment-induced coverage of underlying pockets, six-direction world-clothing compositing, arbitrary support illustration calibration and drag-fitting the body remain separate slices. The rendered exterior uses a bounded stand-in layout; atlas pixels never define physical compatibility. No spillage, crafting or weapon-part system is introduced here.

## October 27 alignment and equipped-target follow-up

The pouch source now declares its mounting-loop anchor, rendered height and baseline direction under `attachmentIllustrations` in the inventory art manifest. The actual source image aspect ratio is preserved. Drawing, alpha picking and all six rotations share that same mounting anchor/transform. The later zero-angle correction below supersedes the initial diagonal baseline. Preview outlines now use the radius implied by hex-center spacing, so neighboring cells meet exactly. Source PNGs and physical fit metadata are unchanged.

A compatible pouch dropped directly onto the belt in the equipped view selects free support at the requested sixth-turn orientation. This quotes a normal paid Move; it never relocates existing attachments or changes orientation to force a fit. A full/covered/inaccessible support rejects with its physical reason. Precise gear-surface drops retain their exact authored contact position. Open-backpack drops still use normal interior packing. Save schema/content identity are unchanged by this follow-up. The separate [rank/stack redesign](33-fit-ranks-and-worn-stacks.md) records the user’s replacement wear contract; the current layer selector remains until that runtime migration.

## October 27 zero-angle correction

The initial alignment follow-up added a -30° art offset to align the pouch with a diagonal occupancy axis. That made the displayed 0° already tilted and shifted all six visible headings by half a step. The pouch registration now uses a **0° source offset**: mount 0° is the original upright illustration, 180° is inverted, and R advances 0 → 60 → 120 → 180 → 240 → 300 → 0. The mounting-loop anchor remains fixed. Physical hex occupancy is unchanged; its authored shape is separate from the source image’s upright axis.

The native test now commits all six rotations through actual drag/R/release and paid handling, compares each placed sprite with independent reference directions, and captures the committed 0°/180° states. This catches a half-step source offset as well as skipped or repeated input steps. Save fields and physical content identity remain unchanged.
