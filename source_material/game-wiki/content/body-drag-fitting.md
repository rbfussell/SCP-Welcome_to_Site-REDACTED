# Direct body-fit drops

**Implemented native interaction, October 30:** drag one visible stored clothing item onto the diagnostic body to preview and purchase its exact fit. See the [runtime guide](doc-docs-39-body-drag-fitting-md.html), [inventory](system-inventory.html), [presentation](system-presentation.html) and [implementation ledger](status.html). This is ordinary equipment handling, not an SCP canon adaptation.

## Exact placement, normal work

The [drop surface](file-game-presentation-inventory-inventorybodyfitsurface-drop-cs.html) shares click/drag polygon picking and [symmetric body geometry](body-fit-cleanup.html). The pointer hex is the authored anchor. Empty margins/gaps refuse placement. Mask and contacts appear at the exact proposed pose; failed drops do not reposition, rotate, repack or remove anything.

R over the body uses six orientations. R over storage uses two, even when body mode is visible; [provider views](mount-points.html) retain six attachment turns. Independent transient drafts leave stored state unchanged. Esc cancels the drag and hides the interface without closure changes. Paused release cannot perform free dressing.

## Access and continuation

[InventoryBodyFit](file-src-sitesimulation-inventory-equipment-bodyfit-inventorybodyfit-cs.html) offers `PreviewBodyFitDrop` for one currently inspectable item. Closed/inaccessible ancestors, foreign equipment and non-wearables expose no geometry. The selector still lists own worn/held clothing only, without scanning hidden storage.

Release submits ordinary Move with its exact pose. Geometry, [overlay bands](doc-docs-36-overlay-sublayers-md.html), coverage, restraints, outermost packs, [provider exposure/clearance](mount-points.html) and access remain authoritative. Source-dependent time/AP is paid before completion; the source stays put until then. Commitment revalidates the graph. Front-mounted refits preserve their mount; stored clothing uses its authored default.

[Persistence](system-persistence.html) remains schema 14 with unchanged immutable content. UI drafts are transient; accepted paid requests continue through existing saves. Saves matching the preceding body-cleanup content remain supported.

## Evidence and limits

- [Domain checks](check-bodydropchecks.html): access/knowledge bounds, exact requests/costs, blockers, pause/AP, revalidation and pending continuation.
- [Native checks](file-game-presentation-spatialscene-bodydropsmoke-cs.html): grid-to-body drags, picking, rotation contexts, failed/paused releases, Esc and paid completion.
- [Recorded runs](doc-docs-reviews-inventory-runtime-checks-md.html): actual results; wiki building does not execute gameplay tests.

This is a fitting target in the engineering view. [Body inspection/removal](body-inspection.html) now adds exposed front/back targets and worn-item drag sources. Final composited clothing, sprite front/back views, anatomical default-mount selection, body/provider target arbitration and world appearance remain future work.

[Confirmed final presentation](wearable-appearance.html) hides the fitting grid and shows character-matched clothing sprites, with highlights clipped to visible garment cutouts. The diagnostic cells remain useful engineering geometry; this requirement does not change current fitting or access rules.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [symmetric fit](body-fit-cleanup.html), [mount points](mount-points.html), [three-view inventory](doc-docs-27-three-view-inventory-and-body-fit-md.html), [design library](documents.html).
