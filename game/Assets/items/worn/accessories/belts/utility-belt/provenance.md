# Worn utility belt — provenance

Generated November 4, 2026 with the built-in imagegen tool (imagegen skill). This is a new stand-in, copied byte-for-byte into this bundle. No crop, recolor, inpaint or alpha edit was saved to its source PNG. Front/back source rectangles and destination scale are runtime metadata.

- Selected source: `/home/robf/.codex/generated_images/01a1614d-06c5-7533-9e41-7451ef31e8db/exec-b90af475-ed25-46bb-ac94-a1c548201f2a.png`
- Project output: `utility-belt.paperdoll-v1.png`, 2172 × 724 RGBA.
- SHA-256: `194fc44b414eb516653646da1e707d7583a63e87df27c1b1f78bd413ec415dbd`.
- Design/material reference: [existing loose belt](../../../../../inventory-objects/utility-belt.png).
- Style/proportion reference: the native `work/preview/spatial-scene/worn-mask-worn-mask-front.png` capture from the preceding shoe-mask slice; development preview, not a runtime dependency.
- Registration: [paperdoll manifest](../../../../../paperdoll-proof/manifest.json), frame 10. Front `[390,110,1390,215]`; back `[390,425,1390,195]`; normal waist pose only.
- The pouch uses its original [shared loose illustration](../../../../../inventory-objects/utility-pouch.png), unchanged, as a bounded front exterior overlay. No additional pouch sprite was generated.

## Exact final prompt

Use case: stylized-concept. Asset type: transparent front/back WORN utility-belt sprite registration sheet for the existing adult RPG paperdoll. Input image 1 is the design/material reference: olive canvas utility belt with worn dark metal single-prong buckle. Input image 2 is only the paperdoll style/proportion reference, not an edit target. Generate ONE sheet containing TWO isolated views of THAT SAME BELT ONLY, no body or clothing: top half FRONT view of the belt fastened around an invisible adult waist with the metal buckle CENTERED; bottom half BACK view of its plain rear webbing. Straight-on orthographic paperdoll views, gently curved around the invisible waist, visible band only, not an overhead ellipse or giant loop. Same width and thickness in both views. Detailed visibly pixelated material-rich classic Fallout-inspired sprite treatment matching the references. Top front centered at one-quarter canvas height and back centered at three-quarter height. Very generous transparent margins around each thin horizontal band, clearly separated rows. Opaque olive woven fabric and muted dark metal buckle, correct closed buckle, no floating long tail. No pouches, holsters, hooks, straps dangling, body parts, clothes, trousers, mannequin, scenery, cast shadow, labels, lettering, grid or checkerboard. Genuine alpha transparency. This is one belt's two views, not two different belt designs.

## Limits

This atlas qualifies a static front/back normal waist belt on the reference body. It is not a rig, arbitrary strap pose, world animation set or per-bundle runtime loader. `item.art.json` records provenance/registration; the existing paperdoll manifest is still loaded by the renderer.

