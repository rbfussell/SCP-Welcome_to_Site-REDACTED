# Scheduled employment and relief prototype

Status: Bounded native implementation, October 25, 2026. This proves one observation-duty routine with two appointments, not the six professions or full EMP-01–08 acceptance in the design contract.

## Playing it

The normal local scene and standalone OSCP scenario now appoint Chen as **Security observer** and Ortiz as **Observation technician** at the receiving-room observation post. Employment owns their work; they no longer need synthetic standing orders. The director is player-controlled and has no autonomous appointment.

Both employees start on duty at separate physical stations. The post has a minimum of one ready employed observer. This staffing minimum is distinct from the containment procedure's three actual humans, which includes the director. Staffing alone cannot suppress SCP-650's relocation, and inadequate staffing cannot force it to relocate while somebody still sees it.

These are deliberately short, repeating **local execution-time test shifts**:

| Employee | Initial duty ends | Next duty starts | Cycle | Maximum overtime |
|---|---:|---:|---:|---:|
| Chen | 50s | 110s | 180s | 20s |
| Ortiz | 120s | 180s | 180s | 20s |

Chen's 120-second shift starts at cycle offset 110 and wraps across offset zero. Ortiz's starts at zero and lasts 120 seconds. Each normally relieves the other. They walk to nearby rest points between shifts. This is a timing/work-ownership proof, not realistic working hours, sleep, meals or an in-shift break system. The appointments and post requirements live in the operation JSON.

Select a worker in the field panel or **OSCP → Personnel**. Existing observe/follow/post/recovery orders remain available. A delivered, acknowledged director order suspends scheduled employment and warns that the post may be uncovered. A message still pending delivery does not suspend it. **Return to scheduled duty** releases the current direct order; the employee travels back if scheduled, or follows the relief/off-shift policy otherwise.

OSCP command example:

```text
order tech post 8 4
order tech duty
```

The phone can prepare either command while paused and send on closing, using its existing validated draft workflow. Workstation commands continue in real time. Persistent follow/post/observe commands need explicit release; a completed trolley recovery returns an employed handler to scheduling on the next local tick. Non-employed legacy recovery fixtures retain their explicit follow-up observation order.

## Work, coverage and interruption

The work handler has reporting, on-duty, awaiting-relief, off-shift, interrupted and blocked states, each with a reason. It uses the ordinary movement service and retains fractional walking positions when orders redirect travel. Closed access or occupied routes produce blocked duty and reports; assignment cannot teleport someone to a station. Off-shift employees retain their employment identity and do not fall into the old discretionary patrol.

An employee provides post coverage only when physically stationary at their assigned station, ready and able to observe the configured point. Actual attention, blink/startle, light and sight matter. An active direct order or tactical participation excludes them from employment coverage even if still standing at the station. The SCP module independently evaluates actual anomaly observation by all eligible humans.

At shift end, a worker waits for enough other scheduled, physically ready on-duty employees at that post. An absent incoming worker starts bounded overtime. Once its limit expires, the worker leaves and the post can genuinely be uncovered. Returning/reloading cannot reset the deadline or invent a handover. Initial off-shift staff have no imaginary pre-campaign overtime.

The present authority model remains the scenario director's operations authority. Departmental supervisors, urgency tiers, explicit emergency overrides, panic/injury doctrine, qualifications, negotiated team agreements, equipment prerequisites and safe interruption of arbitrary long tasks remain future work. Trolley interruption releases its pending push reservation while leaving its real cargo/equipment in place.

## Information and time

The Personnel app shows authored appointments/schedules and **last delivered employment reports**, including their local timestamp and reason. Its staffing warning counts those reports and is explicitly labeled reported coverage, not a live tracker or containment certificate. Losing both voice and radio leaves old records visible; reconnecting reports the current state without fabricating missed labor. Runtime duty/coverage queries are diagnostic and are not the personnel UI's source.

Local input waits and diagnostic holds freeze the work handler, movement and relief progress while the world calendar continues. Manual/phone pause freezes both clocks. During paid tactical resolution, participants remain reserved for combat; scheduled nonparticipants can work and move on those local ticks. The current training scenario enrolls all its actors; a separate core fixture qualifies nonparticipant employment.

This first implementation uses local execution time for shifts and overtime. It does **not** implement the eventual world-calendar-to-held-area scheduling policy. Returning from a long hold does not consume shifts, replay labor or silently finish handoffs. Calendar appointments and cross-area dispatch remain proposed contracts; the clock choice here is fixture scope, not a permanent game-design change.

## Ownership and persistence

| Component | Owns |
|---|---|
| [EmploymentState](../src/SiteSimulation/Employment/EmploymentState.cs) | Appointment/post definitions, duty continuation and separate delivered reports |
| [EmploymentSystem](../src/SiteSimulation/Employment/EmploymentSystem.cs) | Shift evaluation, physical readiness, relief, overtime, duty travel and report delivery |
| [EmploymentSaveCodec](../src/SiteSimulation/Employment/EmploymentSaveCodec.cs) | Detached validation and restoration without replay |
| [PersonnelOrderSystem](../src/SiteSimulation/Operations/PersonnelOrderSystem.cs) | Authority, delivery, acknowledgement, command replacement and release |
| [OSCP personnel view](../game/Presentation/Oscp/OscpWorkspace.Personnel.cs) | Report-based roster and staffing presentation |

Outer save schema **6** adds employment continuation and delivered reports. Definitions remain content, included in the existing content identity; changing the operation JSON makes old scenario saves incompatible. A save matching an employment scenario must include its employment state; the loader does not invent shifts or relief history. Matching scenarios without employment continue to support the existing schema-2–5 migration paths. SCP-650 module schema remains 2.

Save/load preserves pending orders, interrupted physical routes, blocked work, local shift phase, relief deadline, release state and received information. Invalid restores discard a detached candidate without mutating the live simulation.

## Verification

```bash
python3 tools/run_spatial_scene.py --check
python3 tools/run_spatial_scene.py --oscp-smoke work/preview/spatial-scene/employment-oscp.png
python3 tools/run_spatial_scene.py --operation-smoke work/preview/spatial-scene/employment-local.png
```

[Employment checks](../tests/SpatialChecks/EmploymentChecks.cs) exercise real work versus pending orders, reassignment and physical return, actual relief, missing relief and overtime, blocked access, readiness loss, separate information, local/manual/tactical holds, paid combat resolution, continuation and atomic rejection. Godot checks additionally exercise actual command-line Enter for `duty`, the report-based roster and both device layouts, plus existing physical field/OSCP integration.

Full hiring, optional-job preferences, six professions, daily calendars/breaks/needs, employment editing, autonomous door operation, vacancies, general reservations, full authority and injury/panic systems are not implemented by this slice. See the broader [employment contract](07-employment-and-orders.md).

The [inventory follow-on](23-personal-inventory-prototype.md) now supplies actual phone/radio items and shared physical access gates. Outer schema 8 (including garment state) supersedes prior schema references; the changed current scenario/catalog identity requires fresh saves. Earlier device flags remain compatibility service cut-outs.
