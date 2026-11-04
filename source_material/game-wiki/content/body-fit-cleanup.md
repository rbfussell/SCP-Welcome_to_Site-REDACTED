# Symmetric body fitting and adjustable straps

**Implemented native fixture, October 30:** connected nine-shoulder body, mirrored garment/contact masks, paired hip/cargo mouths and centreline jacket fastening. This is ordinary equipment design, not an SCP canon claim. See the [runtime guide](doc-docs-38-body-fit-cleanup-md.html), [inventory](system-inventory.html) and [implementation ledger](status.html).

## Fitting and layering

The [geometry evaluator](file-src-sitesimulation-inventory-equipment-bodyfit-bodyfitgeometry-cs.html) supports an optional authored straight-span range. The native belt extends its five-cell core to at most ten cells, independently at each end until the body boundary. Both end contacts and every intervening cell must fit. It cannot bridge a gap or exceed its limit. Ordinary shapes stay fixed. The [author sheet](doc-templates-gear-data-sheet-md.html) separates storage dimensions from body-local fit.

At chest anchor `(-4,8)`, rotations 60° and 120° form a real ten-cell-per-strap X. The latest base garment counts once; two straps fit, a third at the same clothed intersection fails. [Worn-stack projection](file-src-sitesimulation-inventory-equipment-inventorywornstack-cs.html) uses resolved cells for bands and physical interference on front/back surfaces. Access cover, restraint and contact exposure remain distinct.

The five central authored webbing points remain inside the guaranteed core. Extensions do not create new mounts or resize gear-view artwork. [Hook/provider chains](mount-points.html) retain composed frames, shared clearance, load and ancestry checks.

## Presentation and continuation

[Final clothing presentation](wearable-appearance.html) uses a matching character body and worn sprite silhouettes, with the fit grid hidden in normal use and cutout-clipped highlights. The current cell/contact drawing remains an engineering fixture.

[Presentation](system-presentation.html) previews the same geometry and reports span/range. Anchor/R input is read-only; Apply uses paid handling. `tools/author_body_fit_fixture.py` regenerates matching body/clothing/mouth/fastening geometry using odd-row reflection `(q,r) → (-q-r,r)`.

[Persistence](system-persistence.html) derives length from body, shape and saved pose; no duplicate mutable length is stored. Schema stays 14, but content identity changed: start fresh for earlier field-kit content. Exact pending continuation remains qualified.

## Evidence and limits

- [Symmetry/span checks](check-bodysymmetrychecks.html): mirrored/connected geometry, independent X coordinates, bounded fitting, bands, contact exposure and continuation.
- [Native body-fit checks](file-game-presentation-spatialscene-bodyfitsmoke-cs.html): actual anchor/R/Apply controls, paid X and read-only previews.
- [Recorded results](doc-docs-reviews-inventory-runtime-checks-md.html): actual runs; wiki compilation does not execute tests.

[Direct body-fit drops](body-drag-fitting.html) now qualify stored-clothing fitting through actual dragging. [Front/back inspection and removal](body-inspection.html) now qualifies worn-item picking and drag sources using these masks. Final anatomy art, body sizes, curved straps, cloth fitting, extended webbing art, spillage and world clothing remain future work. The [gear standard](doc-docs-29-gear-data-standard-md.html) remains distinct from native DTOs and public mod loading.

Related: [engine](engine.html), [inventory](system-inventory.html), [presentation](system-presentation.html), [persistence](system-persistence.html), [mount points](mount-points.html), [overlay bands](doc-docs-36-overlay-sublayers-md.html), [body-fit foundation](doc-docs-30-body-fit-prototype-md.html), [design library](documents.html).

Presentation follow-up: [full-body sprite qualification](full-body-paperdoll.html) tests a separate partial outfit with alpha-based picking/highlights. It does not replace this page's physical fit, access, provider or handling contracts.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.
