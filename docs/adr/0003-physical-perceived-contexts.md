# ADR-0003: Physical authority and per-observer perceived contexts

**Status:** Proposed  
**Date:** October 22, 2026  
**Decision owner:** Project owner; not yet assigned

## Context

The user requires modules that transfer actors, alter perceived environments, affect ordinary world state and interact indirectly through shared observation. SCP-1499 is the second demonstration after SCP-650. A separate scene with a self-contained mission result cannot express all requested behavior.

## Options considered

| Option | Benefit | Cost |
|---|---|---|
| Every anomaly is another physical map | Simple scene switching | Cannot distinguish perceived identities/geometry or mapped ordinary-world effects |
| Effects exist only in a client render layer | Easy visual experimentation | Actions, saves, NPC senses and consequences can disagree |
| Physical records plus explicit perceived contexts and bindings | Shared authoritative actions with per-observer experiences | Requires target mapping, conflict policy, causal tracing and persistence |

## Proposed decision

Retain authoritative physical entities/domains and add per-observer perceived contexts, versioned target bindings and scoped cross-context links. Modules use host capabilities to change either or both. Validate mapped actions and commit each consequence once. Knowledge is evidence derived from those experiences, not the underlying world state itself.

Dependent sensory/anomaly evaluation uses committed location/perception changes before evaluating observer sets. This lets SCP-1499 change SCP-650's observation conditions through shared services. Each module declares qualifying senses/media and context policy.

## Consequences and bounds

Physical transfer, altered perception and hybrid mechanics can share controls, inventories and saves. Diagnostics must show actual and perceived contexts separately. Mapping ambiguities and competing overrides require explicit rejection/composition rather than load-order decisions.

M2 proves one physical profile and one bounded perception/consequence profile. It does not prove a universal mapping language or unrestricted reality modification. M3 separately qualifies whole-world checkpoint branches; per-observer context changes are not time-rewind operations.

Related: [ADR-0001](0001-shared-simulation.md), [SCP-1499/context contract](../10-world-perception-and-scp-1499.md), [continuation rules](../08-player-controls-time-and-continuation.md), and CTX-01 through CTX-05 in [acceptance](../04-scope-and-acceptance.md).
