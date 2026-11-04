# Engine and shared simulation

**Implemented architecture:** Godot .NET presentation over an independent C# simulation. The game uses its own rules and state, rather than modifying the Godot engine source. The API catalog covers this repository's engine and presentation functions; it does not duplicate the entire external Godot API.

## Authority and interaction

1. Godot receives input through `SpatialScene`, OSCP, or inventory panels.
2. `SpatialSimulation` accepts commands and validates local readiness, equipment, knowledge, and tactical costs.
3. Domain services change the one authoritative `SimulationWorld`.
4. Movement and perception supply committed physical facts. Anomaly modules evaluate those facts and request bounded effects.
5. The host validates a relocation at commitment; the module cannot directly write the body's world position.
6. Acquired contacts and delivered reports feed player awareness and the journal. The diagnostic trace can expose more truth than the character knows.
7. Godot draws the committed results. Wall cutaways, sprite selection, camera view, and UI inspection do not change collision or grant observation.

## Time and spatial models

The facade declares a **50 ms tick** and **300 ms exploration step**. Square construction/floor geometry and hex actor navigation are distinct. Actors interpolate committed movement; their physical progress matters to sight and collisions, not just their displayed sprite.

Manual pause stops simulation clocks. Local hold and tactical input waiting hold local progress while the world calendar can continue. Local movement, blinks, anomaly deadlines, inventory handling, and work must not catch up against elapsed calendar time on release. A counter named `OffsiteUpdates` is derived from world time; it is not proof of a simulated regional economy.

Inspect [the facade's current source](file-src-sitesimulation-spatialsimulation-cs.html) for exact command and tick order. [World](system-world.html), [actors](system-actors.html), [perception](system-perception.html), [anomalies](system-anomalies.html), [combat](system-combat.html), and [inventory](system-inventory.html) document the participating boundaries.

## Save and continuation

[Mount points and placement extenders](mount-points.html) exercise shared inventory geometry, composed Godot views and exact paid continuation through the belt → hook → pouch fixture. Offered support does not create loose storage or another item parent.

[Symmetric body fitting and adjustable straps](body-fit-cleanup.html) use resolved geometry for preview, wearing and access, including a bounded native chest X. Length derives from immutable geometry and saved pose; paid transactions remain authoritative.

Snapshots preserve state and paid continuations, with content identity and version checks. Restore constructs and validates a detached candidate before replacing live state. Current inventory continuation requires **outer schema 14**; SCP-650 uses **module schema 2**. Earlier docs describe the schema at the time they were written and must not override current validation. Matching older non-inventory fixtures have explicit compatibility paths, not universal migration.

See [persistence](system-persistence.html) and the source for `PrototypeSaveCodec`, inventory, combat, operation, and employment codecs.

## Independent modes

Local play and standalone workstation/phone OSCP share `SimulationRuntime` and simulation services. World-map and facility-management entry points are accepted direction, with implementation still pending. A scene is an adapter to shared state, not a separate game simulation.

## External dependencies

The project baseline pins Godot .NET 4.7.1 and .NET SDK 8.0.128. These are repository requirements, not a claim about current upstream releases. See the design library and `game/project.godot`, `game/SiteScene.csproj`, and `global.json` in the repository for configuration.

[Direct body-fit drops](body-drag-fitting.html) bridge one accessible stored item into the existing Move transaction. Read-only UI pose/rotation drafts do not broaden inventory knowledge or bypass costs and commitment validation.

[Confirmed clothing presentation](wearable-appearance.html) keeps the fit grid underneath character-matched body/garment sprites and silhouette-clipped highlights. This remains a visual requirement; current body diagnostics and baked world artwork do not implement the final compositor.

[Body inspection/removal](body-inspection.html) exposes only own worn material through a read-only surface query. Front/back camera changes do no physical work; selected source IDs enter existing paid item transactions with normal access and commitment validation.

[Full-body sprite qualification](full-body-paperdoll.html) now composes seven wearable definitions over actual worn state and paid fastening/removal. The new underwear profile changes kit identity while retaining save schema 14. Explicit omitted gear and unchanged baked world sprites distinguish this art fixture from the future complete wardrobe pipeline.

Presentation follow-up: [engine-rendered paperdoll mosaic](paperdoll-mosaic.html) now covers the base groin beneath real clothing. The current accepted reference is shirtless with close-fitting tan fabric briefs, replacing the painted undershirt/loose shorts. The earlier featureless mannequin request remains unimplemented. Physical equipment and access rules are unchanged.

November 4 follow-up: [worn backpack/harness presentation](worn-backpack.html) adds mount-aware front/back art and routes visible pack pixels to the existing segmented gear view. Physical fit, access, contents and paid handling remain authoritative.

[Worn waist belt/direct pouch](worn-belt-and-pouch.html) extend the qualified sprite subset to eight wearable definitions and one exterior binding. They read the existing graph without changing physical fit, native content identity or schema.
