# Symmetric body fit and adjustable straps

Implemented October 30, 2026. This refines the diagnostic [body-fit fixture](30-body-fit-prototype.md), [pocket geometry](31-pocket-coverage-prototype.md), [overlay bands](36-overlay-sublayers.md) and [mount providers](37-placement-extenders-and-mount-points.md). It is ordinary equipment behavior, not an SCP canon claim. The [game-wiki article](../source_material/game-wiki/body-fit-cleanup.html) cross-links current mechanics and evidence.

## Native controls and useful fits

Start a fresh scene. Use **I → Body fit · hex view**, holding or selecting the clothing as before. Click chooses the body anchor, **R** steps through six orientations, and **Fit / equip** buys normal handling time/AP. The diagnostic reports fitted strap length and authored range. Previewing does no work; pause cannot supply free dressing. A lower intersecting strap must still be freed before refitting.

Put the backpack aside before chest fitting. Useful current native poses:

| Placement | Anchor | Rotation | Resolved length |
|---|---|---|---|
| Waist | `(-6,12)` | 0° | 5 cells |
| Horizontal chest | `(-4,8)` | 0° | 5 cells |
| First chest diagonal | `(-4,8)` | 60° | 10 cells |
| Mirrored chest diagonal | `(-4,8)` | 120° | 10 cells |

The two diagonals form a real X on the native human body, meeting once at the chest anchor. At that intersection the current base garment counts once, then the two straps use subpositions 1 and 2. A third strap there exceeds the native allowance. Front and back remain separate surfaces. Every anchor/orientation still requires actual geometric fit.

The earlier three-belt demonstration works at waist `(-6,12,0)`, diagonal `(-4,10,2)` and horizontal chest `(-4,8,0)`. Its separate intersections exercise exposed versus covered attachment contacts. It is distinct from the chest-X example.

## Symmetric authoring

[author_body_fit_fixture.py](../tools/author_body_fit_fixture.py) regenerates the body and matching garment rows, boundary contacts, closed/open cover masks, trouser mouths and jacket fastening footprint in the embedded JSON. Other definitions and metadata are retained. After changing its inputs:

```sh
python3 tools/author_body_fit_fixture.py
python3 tools/author_body_fit_fixture.py --check
```

The odd-row offset canvas projects horizontally as `q + r/2`. Vertical reflection is `(q,r) → (-q-r,r)`. Odd rows require even-width symmetric spans; using even-row centering shifts half the silhouette. The corrected connected body retains nine shoulder cells. Default garments and required outward edges mirror the body. Hip/cargo mouths are paired mirrors. The jacket fastening includes both centreline-adjacent cells on odd rows. Open jacket coverage still exposes hip mouths while cargo access remains independent. Storage dimensions and art are unchanged.

## Bounded adjustable span

`BodyFitShape.LinearSpan` optionally declares `MinCells` and `MaxCells` for an extension-only straight strap. The native belt uses **5–10 body cells**. These are provisional body-local fit units, not millimetres, map units or a change to packed dimensions/mass.

The authored mask defines a contiguous horizontal core containing the anchor, with two outward end contacts. The solver keeps that core and walks independently along each rotated end until the first exterior cell. It accepts only a fully supported run within the authored total range. Independent end lengths accommodate shoulders and hips without moving the anchor. It never bridges a gap, shrinks the core, stretches past the limit or ignores a failed contact. Fixed shapes retain their original evaluation.

Catalog validation bounds ranges to 3–33 cells, requires minimum length to equal the core, and checks straight/contiguous occupancy and exact endpoint contacts. Any declared closed/open surface masks must cover the full core. Declared surfaces expand to the resolved span for band counts, access coverage, restraint, fastening interference and attachment exposure. Bulk/compatibility remain separate tests. Diagnostics draw the same fitted cells.

The existing five central webbing positions remain authored provider-local mounts inside the guaranteed core. Extension does **not** create additional attachment points or resize the gear illustration. Hook/pouch composition, required contacts, shared exterior clearance, recursive load and parent relationships retain their rules. Stretchable clothing, curved straps and adjustable gear-view art are not implemented.

## Transactions, persistence and verification

Span length is derived deterministically from immutable body/shape data and saved pose, with no duplicate mutable length. Fitting uses the ordinary whole-graph validated Move command, source retention, normal costs and commitment revalidation. Schema stays **14**; corrected immutable fixture content changes identity, requiring a fresh scene for earlier field-kit saves. Matching pending work resumes exactly.

```sh
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --inventory-smoke work/preview/spatial-scene/body-cleanup.png
python3 tools/author_body_fit_fixture.py --check
python3 tools/check_gear_data.py
python3 tools/build_game_wiki.py
python3 tools/build_game_wiki.py --check
```

[BodySymmetryChecks](../tests/SpatialChecks/BodySymmetryChecks.cs) qualify mirrors, connectedness, contacts, paired mouths, independent diagonal coordinates, range failure, six orientations, resolved surfaces, paid native X, covered contacts and pending continuation. Existing overlay, coverage, attachment, hook, AP and invalid-save checks remain required. [Native body-fit checks](../game/Presentation/SpatialScene.BodyFitSmoke.cs) exercise actual anchor/R/Apply input, read-only ten-cell previews and paid/restored X geometry. See [recorded results](reviews/inventory-runtime-checks.md); wiki compilation does not execute gameplay tests.

The [direct body-drop slice](39-body-drag-fitting.md) now qualifies stored-clothing drag fitting. Final anatomy art, body-size variants, dragging worn gear out of the body, world outfit composition, general cloth restraint and spillage remain later work. Named mounts and coarse ordinary-garment removal declarations still apply alongside geometry.
