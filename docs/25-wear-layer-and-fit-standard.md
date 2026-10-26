# Wear layers and fit standard

Status: October 25, 2026. User-confirmed direction: base garments occupy layer 0; outer items have a predictable wearing order; fitting something over another item depends on its construction. The example ordering below records the user's intended game behavior. Fit-profile fields, compatibility defaults and discomfort effects are proposed. This does not add accessory items or a fit solver to the running game.

This extends [body equipment and container navigation](24-body-equipment-and-container-navigation.md). The current runtime already validates authored layers, body coverage, fastening, dressing order and outermost packs. It does not yet implement the fit/comfort details described here.

The subsequent [body-hex shape/contact proposal](27-three-view-inventory-and-body-fit.md) refines worn placement: closed contact edges must align with the body's boundary while authored openings permit continuation. Packed inventory remains square-grid. This proposal extends the fit standard without claiming that named mounts have already been replaced in the runtime.

## Layer means position in a wearing order

Layer 0 is the base or closest-to-body position for a particular body location. Examples include underwear, socks, a baseball cap and ordinary glasses. These belong to different anatomical locations, so sharing the number does not make them conflict.

The number remains stable when optional inner garments are absent. A helmet keeps its outer layer when worn directly on the head; it does not become layer 0 merely because no cap is underneath it. Layer 0 also does not mean all base items must literally touch skin at every point.

A proposed authoring reference for head/face accessories is:

| Location / family | Layer 0: base | Layer 1: over the base | Layer 2: outer protection |
|---|---|---|---|
| Headwear | Baseball cap | Knit cap / toque / watch cap | Helmet |
| Eyes / face | Ordinary glasses | Compatible over-glasses goggles | Compatible gas mask |

This table describes intended ordering, not blanket permission for every item combination. Larger equipment, enclosing suits and outermost packs continue to use their own authored higher layers and mounting rules. The existing shirt/trousers/coat/suit/pack fixture values remain test content until the wider standard is adopted; this document does not silently renumber saved equipment.

## Fit decides whether the ordered combination works

Validation must distinguish anatomical location, layer order and actual fit. A higher layer does not guarantee compatibility. Equally, a lower item does not become wearable over a higher one just because the player equipped it last.

| User example | Intended authored result |
|---|---|
| Baseball cap, then a suitable knit cap | Allowed: the outer knit cap can accommodate the cap beneath |
| Knit cap, then baseball cap over it | Rejected: incorrect wearing order and insufficient outer-cap fit |
| Suitable knit cap beneath a compatible helmet | Allowed when the helmet's fit permits it |
| Baseball cap beneath a compatible helmet | Can be allowed but awkward; record discomfort separately from a fit rejection |
| Glasses beneath suitable goggles | Allowed for a compatible over-glasses profile |
| Glasses beneath a suitable gas mask | Can be allowed by that mask's profile |
| Ordinary glasses over a gas mask | Rejected: glasses belong beneath it and do not fit over its outer shape |

These are game-content contracts. The engine should not contain branches for specific named hats, goggles or masks. Content authors select reusable fit families and supply exceptions where required. Some goggles or masks can support glasses underneath while other profiles reject them.

Proposed fit-profile concepts:

- **Location and coverage:** where the item mounts and what it physically covers. Refine the current coarse head/face regions when accessory content needs distinctions such as eyes, crown and mouth/nose; a strap or mounting contact is not automatically full coverage of an entire region.
- **Default layer and fit family:** a stable wearing position and a reusable shape/construction class, such as fitted cap, stretch knit headwear, rigid helmet or spectacle frame.
- **Fit allowance:** what the outer item can accommodate beneath it, using families, size/bulk, stretch or internal clearance as appropriate. Worn fit allowance is separate from the item's packed inventory dimensions.
- **Conflicts and required clearance:** brim/visor obstruction, frame space or another authored incompatibility. Explicit rejection takes precedence over a broad compatible family.
- **Allowed but awkward combinations:** a distinct result from refusal. Comfort or handling modifiers can be added later; no numerical penalty is chosen here.
- **Coverage, fastening and access:** compatible equipment still obeys the existing rules for covered pockets, supported openings, required removal and timed/AP-bearing dressing.

Fit checks must consider the complete overlapping worn stack, rather than just the immediately preceding item. A helmet may accommodate one inner layer but fail with the combined bulk of a cap and knit cap. Neither a fit-family match nor an extra layer creates unlimited wearing capacity.

## Content authoring workflow

An author should be able to choose a standard body location and fit family, receive a suggested layer/profile, then describe only the exceptions. The validator should explain refusals in terms of the actual obstruction, such as a fitted cap not accommodating the knit cap beneath it, rather than reporting only an arbitrary slot error.

Before publishing the accessory standard, each profile needs examples of allowed, rejected and awkward stacks; checks for both attempted wearing orders; overlap with adjacent equipment; total bulk/load; fastening/access changes; and save/restore. Existing outermost-pack rules remain mandatory. A valid final stack does not bypass the physical dressing sequence: placing a cap underneath an already worn toque still requires clearing the outer headwear first.

The next implementation step is reusable fit metadata and validation over the existing equipment graph. A general cloth simulation, bespoke code for every item pair, new art for every combination and completed comfort/protection systems are not prerequisites for that bounded step.
