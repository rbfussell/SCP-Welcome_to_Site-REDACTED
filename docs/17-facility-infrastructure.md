# Facility infrastructure and environmental containment

Status: Required product direction, October 22, 2026. The user requires Dwarf Fortress/RimWorld-style infrastructure, including electricity, heating and cooling, because physical operating conditions are part of containment. The contracts and milestone bounds below are proposed. This is a design extension, not an implemented utility system: the current spatial prototype has a debug lighting boolean, with no circuits, heat model or HVAC.

## Physical services, shared by every interface

A room designation does not supply power, a stable temperature or containment. Sources, routes, distribution equipment, consumers, controls and maintenance must actually provide the required conditions. Field interaction, facility overlays, OSCP orders and AIC actions all operate on these same installed objects and validated services.

| System | Required behavior | Staged detail |
|---|---|---|
| Electrical | Sources, circuits, switches/breakers, distribution capacity, loads, backup supply and faults | First power proof in M1; richer generation and distribution later |
| Heating/cooling | Room temperature, thermal inertia, heat exchange, equipment output, setpoints and capacity | First two-room thermal proof in M1 |
| Ventilation | Air connections, fans, isolation dampers and declared filter behavior | Bounded environmental-containment fixture in M3; no general fluid dynamics |
| Water/service fluids | Typed supply and return/drain connections, pumps, stored quantities and device requirements | Later bounded fixtures; pressure/chemistry detail needs a separate scope decision |
| Control/data | Sensors, controllers, actuator links, alarms and reachable monitoring services | M1 minimum power/thermal controls; SCiPnet connectivity remains a separate service |

Electricity, ventilation and data connectivity are distinct networks. Sharing a route or room does not make one a substitute for another. A disconnected workstation may still have power; a powered controller may have lost its sensor or actuator link.

Construction uses the square building grid, wall/service sockets and explicit inter-floor connections, independently of hex actor navigation. Installed segments and devices have stable IDs, condition, capacity and maintenance access. Crossing drawn lines do not connect networks without a compatible junction. Their visual overlay is a view of authoritative topology, not a separate layout.

**Pending player choice:** manual routing on utility overlays, optionally assisted by automatic route planning, versus room-level automatic connections. Neither option permits service without a valid installed connection and adequate capacity. Until resolved, author the small proof networks explicitly; do not commit a final construction UI. Proposed automatic routing produces a reviewable blueprint and physical work orders, using the same costs and access rules as manual routing.

## Electrical operation

Track requested demand, delivered power, connection limits, available generation and stored energy separately. A lamp's configured state, commanded state and actual illumination are distinct. Doors declare loss-of-power behavior such as fail-open, fail-locked or a mechanical release; one global default cannot decide every security door. Rated output and actual delivery can differ through condition, shortages or faults.

Proposed first resolver: deterministic capacity-limited circuits with explicit switches and protective devices. A consumer receives its declared minimum operating supply or enters a declared degraded/off state; selected consumers may support continuous output. Consumer priorities and stable tie rules determine load shedding. An overload trips the declared protection or follows an authored fault rule; it cannot silently create unlimited power. Redundant feeds need valid switching and source accounting, rather than duplicating supply through a graph cycle.

Backup power transfers only through installed, operational equipment. Batteries lose stored energy in proportion to delivered load and elapsed simulation time; generators require their declared fuel and startup conditions. A working backup can prevent an interruption, while transfer delay can still interrupt a sensitive load. No random outage is required to make maintenance relevant: damage, exhaustion, isolation and overload provide causal faults. Exact ratings, loss models and repair durations are tuning, not established requirements.

## Room environment and HVAC

Use a bounded room/connected-area model rather than simulating every air molecule. Each thermal space records physical volume, effective thermal capacity, temperature and boundary connections. Geometry and material definitions supply insulation/conductance; doors, openings and outdoor boundaries affect heat exchange. These are physical boundaries, not the shape or apparent height of a sprite.

Heating adds energy. Cooling removes it and rejects it to an explicit destination such as an outdoor boundary or a connected heat-rejection unit. Equipment has finite output and supply dependencies. A cooler in a sealed room cannot remove arbitrary heat without somewhere for that heat to go. Electrical draw and waste heat use declared device data; efficiencies and simplified external reservoirs must be explicit.

At each environmental integration boundary, compute heat sources/sinks and exchange over the elapsed **local simulation** interval. Keep authoritative units explicit: temperature in degrees Celsius, power/heat flow in watts, stored energy in joules and thermal capacity in joules per degree. Use deterministic numeric rules and published rounding. Choose or subdivide the integration step so heat exchange cannot overshoot equilibrium or skip a containment threshold. Render frames never advance temperature.

The first fixture needs two rooms, a door/opening, one outdoor-temperature boundary, one powered heater/cooler and one controllable heat source. It must demonstrate:

- A setpoint is a control request; the actual temperature approaches it according to capacity, losses and thermal inertia.
- Removing power stops dependent output, while the room warms or cools over time. It does not instantly become outdoor temperature.
- Opening a door changes exchange and may exceed the plant's capacity without any equipment being broken.
- A hot process can overpower cooling. Oversized cooling or a failed controller can also violate a lower temperature bound.
- Restoring power does not instantly restore safe conditions. Recovery and stabilization remain work in progress.

Ventilation later extends this with declared air transfer and bounded contaminant/filtration fields. Pressure, humidity, oxygen, radiation, cleanliness and other environmental quantities are capability candidates, **not implicitly working simulations**. A module requiring an unsupported quantity fails validation with an actionable message; it cannot quietly assume a safe value. Each new quantity needs units, transport/exposure semantics, clock ownership, persistence and a qualified resolver before certification.

## Sensors, automation and staff

Separate actual environmental state, measured values and personnel knowledge. A sensor reports its own reading, sample time, calibration/fault state and reachable recipients. A lost or stale sensor is not a reading of zero and is not proof of safe containment. Threshold alarms use the declared range, delay/hysteresis and sensor evidence; independently, an anomaly can react to actual conditions whether staff notice or not.

Controllers execute configured local policies using available inputs. A thermostat can keep operating while SCiPnet is unavailable if its own hardware, power and sensor/actuator path survive. The AIC may monitor, report or request authorized adjustments through these services; it cannot read an uninstrumented room's true temperature or repair a broken cable by issuing a command. Loss of its host does not automatically disable an independent hardwired safety controller.

OSCP Facilities shows known circuits, load/capacity, measured room conditions, target ranges, backup state, fault evidence and work status. Show commanded versus actual operation and stale readings distinctly. Commands can isolate circuits, change authorized setpoints, shed loads or request repair, but still require a reachable actuator or physical worker. A development-only inspector can show true topology, energy balance, actual conditions and the cause of each transition.

Maintenance requires qualified people, access, tools, parts, time and any isolation/authorization step. It participates in employment and order arbitration; a cafeteria employee does not become an electrician through an optional repair toggle. The M1 proof can assign a bounded maintenance qualification and authorized repair duty to one of the existing 16 staff. This does not qualify every technical profession or expand the population. Broader engineering employment and procedures are to be scoped with their content.

## Module-facing conditions and effects

The host owns utility topology and environmental state. A module declares required services and quantities, affected spaces, units, safe/unsafe ranges with inclusive/exclusive boundaries, rate-of-change sensitivity, exposure durations, control dependencies and permitted effects. It receives scoped queries/events and requests validated effects such as a heat source, a device fault or changed material conductance. Overrides retain provenance and composition rules; a module cannot bypass energy/item accounting accidentally through an unrestricted state write.

Keep procedure compliance distinct from the actual hazard rule. Losing backup capacity can violate a procedure before temperature leaves the safe range. Conversely, having every checklist box ticked does not suppress a hazard if a sensor lies or the actual condition has changed. Triggers need event ordering and threshold-crossing times, not periodic frame checks or a generic containment score.

Direct and unattended resolution use the same environment, conditions, resource use and causal outcomes. A certified coarse resolver may advance long stable intervals analytically, stopping at the next supply depletion, threshold or other dependency event. If aggregation cannot preserve a module's environmental trigger, retain sufficient fidelity instead of rolling a generic breach probability.

## SCP-019 as a design check

Primary reference: [SCP-019 — Sophia Light](https://scp-wiki.wikidot.com/scp-019), read from the [local archive](../source_material/README.md), snapshot `278a69a68e4b85ac7135d061b9fbd6f1027e1a96`. Article text is CC BY-SA 3.0; no article artwork is imported. This is a main-article infrastructure check, not a complete adaptation or cross-wiki canon review.

The article specifies a vertical vase above a grate in a reinforced room with an incinerator, constant observation through glass and a temperature of **0°C when the incinerator is inactive**. Even at 0°C and at rest, approximately one SCP-019-2 emerges per hour. Movement, threats, extreme temperatures, abrupt environmental changes and insertion of objects/organisms affect emergence; insertion can provoke a flood. Cooling is therefore one operating condition in an active containment process, not an off switch.

A future module would exercise these chains:

| Physical event | Shared consequence | Module/procedure concern |
|---|---|---|
| Cooling loses supply or capacity | Room temperature changes over time | Evaluate authored temperature response and escalation; a fault is not automatically an immediate escape |
| A controller overcools, or the environment changes suddenly | Lower-bound or rate-of-change event | Lower temperature is not universally safer |
| An entity is detected | Qualified observation creates evidence and an authorized response | Activate the installed incinerator; it needs its declared utilities and operational capability |
| Incinerator operates | Heat enters the room and uses supplies | The 0°C idle requirement does not prohibit the treatment cycle; controlled recovery matters |
| Monitoring or treatment fails | An entity can remain alive and damage the room | Actual entities, damage, exits and response determine the outcome |

The source gives no exact per-degree spawn multiplier, HVAC power rating or numerical disturbance curve. Those must be labeled game tuning and reviewed before implementing SCP-019. Its addendum also describes missed partially transparent entities and increasing resistance to incineration; a cold room plus an infallible fire button would not represent that account completely. Treatment alternatives and the article's uncertain explanations need a separate adaptation decision. No full SCP-019 campaign or combat population is added by this design pass.

## Time, persistence and diagnostics

Temperature, local supply consumption, equipment cycles, faults, exposure timers and maintenance work belong to the local execution clock. All wait during that zone's tactical input hold, as already agreed. Global manual pause stops all clocks. World-calendar timestamps can continue to label reports without consuming held local fuel or expiring local hazards.

A utility spanning a held and a running zone is a real causal boundary: neither side can assume unlimited supply, double-spend a shared battery or silently apply backlog on release. M1 qualifies networks within one execution zone. Before accepting cross-zone utility links, qualify an explicit boundary/interrupt or merged-execution policy under [the time contract](adr/0006-real-time-exploration-and-tactical-turns.md). Unsupported cross-zone topology must be rejected visibly; a future network game cannot treat that rejection as its final solution.

Save topology/revisions, isolation and faults, device condition, source stores, room thermal state, controller modes, sensor readings/sample times, remaining local timers, pending events and maintenance reservations together. Save/resume must preserve the same next threshold/failure event, without free backup charge, restored safe temperature or repeated resource consumption. Traces connect the original cause through service loss, environmental change, detection, alarm and response.

## Readable implementation boundaries

Proposed future code layout, not directories already implemented:

```text
Infrastructure/
  Topology/       ports, routes, junctions, connected services
  Electrical/    supply, demand, protection, backup stores
  Thermal/       spaces, boundaries, heat sources and integration
  Ventilation/   later bounded transfer/filter/isolation resolver
  Devices/       installed equipment and operating modes
  Controls/      sensors, controllers, actuator requests
  Maintenance/  faults and work prerequisites/continuations
```

Inventory owns portable parts, fuel and physical transfers; employment owns worker availability/authority; infrastructure owns installed service behavior. Installation/removal uses shared transactions rather than duplicating an item as both stored stock and an active device. OSCP and facility overlays query these systems and submit commands. SCP modules consume scoped conditions/events and request effects; they do not become alternate power or HVAC engines. Diagnostics should expose a device's dependency chain from source through route and controls to output, with the first unmet prerequisite and its cause.

## Implementation boundaries and acceptance

The immediate scene remains the spatial/observation proof. Its F8 lighting switch is useful for debugging SCP-650 and does not satisfy the infrastructure requirements.

Proposed M1 extension: one authored electrical fixture with a protected branch, limited backup, light/door consumers and a bounded heater/cooler; the two-room thermal fixture above; a sensor/controller/alarm path; one qualified repair procedure. Reuse slice rooms/staff, generated device sprites and existing room conversion. No structural expansion, complete fluid simulation or new named SCP module is required.

M3 adds one bounded ventilation/isolation fixture and an **SCP-019-informed environmental hazard test**, including idle production, a hot treatment cycle and controlled thermal recovery. This can use diagnostic entities and explicitly labeled tuning. Full SCP-019 adaptation, water/service-fluid content and wider engineering jobs remain separately scoped. Electrical and thermal infrastructure are required foundations, rather than optional release decorations.

Acceptance requirements are tracked as FAC-02 through FAC-09 in [scope and acceptance](04-scope-and-acceptance.md). Numeric capacities, routing UX, supported environmental quantities and cross-zone network policy must be resolved or bounded explicitly before their affected implementations are certified.
