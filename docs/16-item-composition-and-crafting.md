# Item composition, materials and crafting

Status: Confirmed architectural direction with proposed contracts, October 22, 2026. Recorded alongside the SCP-650 implementation; this document does not implement crafting or add M1 content. A later [personal inventory slice](23-personal-inventory-prototype.md) now implements real envelopes, nested membership, mass, access and timed handling; divisible materials/composition/crafting below remain future contracts. The user's examples explain desired flexibility, not mandatory recipes or claims about SCP canon.

## One inventory model, several views

Personal inventory, world/map stock and nested inventories use the same item identities, physical placement, access and ownership rules from [storage and logistics](05-storage-and-logistics.md). UI panels are projections of that state. A map summary cannot create stock or bypass a closed box. A personal pocket does not use separate permissive packing rules.

Keep future responsibilities explicit:

```text
Simulation/Inventory/
  Items/          identity, dimensions, condition and material/part definitions
  Containers/     internal regions and native acceptance capabilities
  Placement/      masks, physical dimensions, volume, mass and reservations
  Hierarchy/      physical parent relations and recursive accounting
  Access/         lids, seals, reach, tools, installed/serviceable state
  Transactions/   move, split, merge, install and release
Simulation/Crafting/
  Composition/    installed parts, material amounts and structural roles
  Schematics/     learned/authored plans and repeatable batch requests
  Assembly/       staged jobs using inventory transactions
  Disassembly/    accessible recovered parts/materials and losses
  Properties/     versioned capability/effect evaluators
Presentation/Inventory/
  Personal/       character equipment and accessible containers
  World/          nearby fixtures, map stock and delegated work
  Hierarchy/      inspectable nested relationships shared by both views
```

These are intended boundaries, not empty placeholder subsystems to add now. Share validation/services rather than implementing the hierarchy independently in each view. Every state-changing request should explain the failing constraint in ordinary terms.

## Small objects still have real size

A gum packet can have six authored small-item positions, while each piece has dimensions, mass and count. Its interior is distinct from its exterior footprint. A larger object cannot fit just because the UI happens to display both as one icon or because total mass is low. Check physical envelope, shape, interior space, mass and compatibility. The same rules prevent placing a hand grenade in a gum packet.

Grid scale is a container/view choice with a declared mapping to physical units. A six-dot interior and a backpack's coarser grid can coexist. Zooming or opening a finer view never changes capacity. The physical precision and supported shape rules still need qualification; this does not require general 3D packing.

Discrete parts, stacks and divisible material lots are separate representations. Material amounts have explicit units and validated precision. Counted gum pieces are not merged into a weight quantity unless a transformation explicitly changes their form. Powders, liquids and formable solids can expose split/portion actions according to their definition, tools and access. Phase, density/packing rules, carrier and container suitability affect usable space: weight alone is not a universal volume measure. A stack's footprint can grow with amount; allocation must revalidate before commitment.

Compatible portions can merge only if their material identity, condition, provenance distinctions and anomalous properties remain compatible. Do not average away an important anomalous property to simplify a stack. Emptying a suitable open packet can produce individual pieces; extracting a measured portion from a material lot preserves its actual remaining amount.

## Contained versus installed

An assembled item retains a composition record but does **not** expose all installed components as ordinary container contents. Its case, wires and internal parts contribute to mass, condition and capabilities while remaining installed. Each part/material quantity has one authoritative physical membership, with typed relations such as contained, installed, carried by a work process or located in the world. Avoid counting a part both inside an assembly and in loose stock.

Completing assembly changes accessibility and available actions; it does not erase provenance or magically create an unrelated item from its name. Disassembly is a validated work action that exposes/reclaims actual parts and materials. Required tools, competence, damage, losses and irreversible joins are content rules. A completed item cannot be emptied for free through a generic container command. A multi-select “disassemble and empty” request expands into real accessible work, then ordinary transfers/splits, with capacity/staging checks.

Whether explicitly designed service slots may remain accessible without full disassembly—for example a replaceable radio battery—is awaiting the user's answer. Proposed service ports would grant only their named access, leaving other installed components sealed. This must not silently turn every assembly into an open container.

## Assemble from components and properties

A schematic declares structural roles, required component/material predicates, quantities, fit, required work and the resulting capability composition. The user's radio/transceiver example illustrates adding capabilities from different assemblies: a communications device needs a valid structure, power and relevant control/output functions; adding supported voice input/transmission components can produce a transceiver capability. Merely placing arbitrary parts together does not simulate a complete electronic circuit.

A learned or experimentally authored plan can retain those constraints as a repeatable schematic. The result comes from the **actual** input instances and supported property evaluators, rather than a single hardcoded output lookup. Damage, material quality and module-authored traits can affect the output. The schema must distinguish an object's true properties from what the character has learned; disassembly or crafting does not automatically reveal every hidden anomalous trait.

Capabilities provide bounded composition: equipment actions ask for applicable properties/functions rather than recognizing every outcome by item ID. Examples include protection, carrying, power supply, communication, tool use and fictional payload effects. New kinds of behavior still require a supported capability or engine extension. This is extensibility through defined rules, not a promise that arbitrary physics/chemistry/electronics will emerge from arbitrary properties.

The user's anomalous-ammunition and small-container payload examples are future game-content illustrations. Material refinement could alter a fictional material's effect rating; an installed anomalous component could contribute a supported trait; compatible housing could determine size/access. No particular multiplier, SCP-914 outcome, SCP-127 interaction or physical construction recipe is adopted as canon or implemented here. Effect ratings remain fictional game statistics.

## Work, batches and state changes

Assembly reserves actual components/material amounts and output/staging capacity. Preparation, extraction, installation and completion use the same physical inventory transactions as direct play and delegated staff. A batch request may reserve inputs for many units, then commit individual work boundaries; it must show completed, remaining and blocked work. Save/cancel cannot duplicate parts, material or completed outputs. An assembly in progress has explicit ownership and recoverable continuation.

Repeating a schematic uses available compatible inputs; it does not clone unique parts or a successful anomalous result without resources. A failed or interrupted job retains the actual partial assembly and declared consumed/lost material. Taking apart many supported ammunition assemblies can yield their recovered components and compatible material lots through this same process, with real access and work costs.

An anomalous transformation such as a future refinement module requests a host transaction affecting instances/material traits. Recompute mass/size/capabilities and validate the resulting placement. A declared anomalous source/sink may intentionally violate ordinary conservation, but the transaction must record that exception and handle overflow or failed placement without overlap or duplication. Original provenance and transformation history remain inspectable.

## Use, wear and remnants

Equipment has condition and component-specific use/damage responses. Wear is supplied by the performed action and damage context, with appropriate item/material evaluators; it is not one universal degradation rate. Unsupported capability combinations are rejected with a reason.

Future firing consumes the actual ammunition assembly and produces supported remnants, including an ejected casing where appropriate. The casing appears once at a valid physical location or collection container and retains its relevant condition/provenance. It cannot also remain an installed part of a still-intact round. Reloading, recovery and reuse are ordinary work over those actual objects.

Useful future proofs: six pieces fitting a small packet while an oversized object fails; portion/split/merge without material loss; a sealed assembly that exposes parts only after authorized work; two compatible input qualities producing different derived properties; an interrupted batch with exact accounting; a size-changing anomalous refinement that cannot corrupt placement; and one use/remnant/condition cycle. These are future qualification ideas, with scope chosen explicitly before implementation.
