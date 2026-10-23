# SCP-650 first-pass encounter artwork

Added October 23, 2026. `scp650-atlas.png` is a new AI-generated transparent six-pose sprite sheet created for this project with the built-in image-generation tool. It interprets [SCP-650 by Dr Talson](https://scp-wiki.wikidot.com/scp-650), whose text is CC BY-SA 3.0. No Wiki or Fallout game artwork was copied. Preserve the SCP attribution and applicable adaptation licensing in distribution; the project's overall distribution strategy remains undecided.

The prompt requested a black stylized humanoid, smooth featureless head, handless pointed limbs, fixed elevated three-quarter camera, textured visibly pixelated RPG treatment and six separate still poses: reaching upward, two threatening postures, crouched, seated and huddled. These are pose states, never sequential animation frames.

`scp650-appearance.json` describes source-image regions, feet anchors and render scale. Godot samples the original image through atlas regions; it is not destructively cropped or resampled. The original deterministic stand-ins and manifest remain available with F4. Running `generate_spatial_assets.py` only regenerates those stand-ins.

This is initial encounter art, not final production approval. The sheet has one view per pose, approximate anchor alignment and generated visual interpretation. Furniture-supported seated/reclining poses, final pixel density, full facing sets and precise pose envelopes still need art/placement qualification. Current poses retain the module's single-hex footprint, with no claim that the full sprite silhouette is physical collision.
