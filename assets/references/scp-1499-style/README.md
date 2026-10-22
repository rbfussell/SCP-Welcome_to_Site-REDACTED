# User-supplied SCP-1499 art references

Received October 22, 2026. The user supplied these examples as the style they envision for the game. The PNGs are preserved byte-for-byte, including their alpha channels. [The manifest](manifest.json) records original attachment names, dimensions and SHA-256 hashes. They are art references; no gameplay visual binding or production scale is selected by importing them here.

| Reference | Observed dimensions | Design relevance |
|---|---|---|
| [SCP-1499-1 creature](scp-1499-1-creature.png) | 474×1,556 | Tall silhouette, detailed eyes/mouths, material texture, deep shadows and selective olive highlights |
| [Black towers](scp-1499-black-tower.png) | 1,172×1,528 | Angled architecture, strong vertical depth, readable structural edges, charcoal materials and restrained violet accents |
| [GP-5 mask](scp-1499-gp5-mask.png) | 857×1,212 | Equipment illustration with textured rubber/metal/glass, substantial shading and clear component shapes |

## Visual interpretation

The examples clarify detailed pixel-sprite artwork with deliberate visible pixel/stepped shading, dimensional form and textured surfaces. This corrects an overly broad reading of “not pixel art”: the user rejected the simplified low-color NES/RPG Maker aesthetic, not every form of pixel-based artwork. Fallout 2 remains the broader reference. The user explicitly confirmed retaining this detailed, visibly pixelated treatment at normal gameplay zoom. Filtering, scaling and asset production must preserve the authored pixel structure.

Use material detail, silhouette, shading and restrained accent colors as shared references. The browns/olives and charcoal/violet accents belong to these subjects; they do not impose one mandatory palette on every ordinary facility, town or anomaly.

These images serve different presentation roles. The creature guides actor design, but its in-game directional poses must be authored for the selected fixed camera. The tower guides architectural appearance; world use requires declared ground footprints, anchors and suitable draw/occlusion layers. The mask can guide a detailed inventory illustration; its world, worn and inventory views can have separate assets tied to the same item identity. Do not scale every attachment by the same factor and assume their relative world sizes are correct.

Reference image dimensions are not a native tile resolution, actor height, animation frame size or texture budget. Inspect a representative scene at gameplay zoom before choosing those values. Collision, line of sight, sensor capabilities, traversable bridges/floors and other mechanics remain declared module/world behavior, not automatically inferred from image pixels.

Production perspective, direction count, animation totals, palette/lighting treatment and scale need a consistent specification. The generated labeled diagnostic sprites remain the development default until finished assets satisfy those bindings. See [the sprite/art contract](../../../docs/06-sprites-and-placeholders.md).
