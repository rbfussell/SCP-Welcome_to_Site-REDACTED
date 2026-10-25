# Inventory object sprites

Generated October 25, 2026 with the built-in `image_gen.imagegen` tool, one transparent-background request per object. These are game-project assets, copied into this directory; runtime does not depend on the generator cache.

The [manifest](manifest.json) records each original output path, exact subject, common full prompt template, unmodified PNG size, alpha range and crop region used by Godot. For the original eleven objects, combining `promptTemplate` with the asset's `subject` reproduces its exact submitted prompt. The four clothing additions preserve their complete prompt per asset and in [wear-prompts.json](wear-prompts.json). The PNGs are original tool outputs; registration selects an opaque-silhouette bounding region without rewriting image pixels. Nearest filtering retains texture; the gum-wrapper binding has a presentation-only base orientation adjustment.

Objects: secure phone, site radio, backpack, trousers, coat, flashlight, dressing packet, utility case, gum packet, gum piece, inert training weight, work shirt, wool socks, work shoes and chemical coverall. No icon backgrounds, rarity cards or copied inventory-interface art are used. Physical dimensions, occupancy, access and AP/time are defined independently in the simulation content. Replacing artwork cannot change packing or gameplay identity.

These are development stand-ins. They are separate from the six-facing character sheets and do not yet change the actor's rendered clothing or play handling animations.
