---
title: Decide whether tenant isolation is structural or decided
type: todo
summary: "Whether the account scopes a request before the engine runs, or isolation is judged by the engine like any other access."
tags: [architecture, multi-tenancy, security]
priority: high
status: open
effort: medium
relates-to: "[[actor-catalog]]"
---

# Decide whether tenant isolation is structural or decided

## Observation

Tenant is a PAP concept and must also be a PDP one. Without that, a caller scoped to one tenant can ask about another tenant's resources.

Two shapes are available:

- Structural — the account scopes the request before the engine runs, so no theory a tenant writes can reach past it.
- Decided — the engine judges cross-tenant access the way it judges everything else.

## Why it matters

Deciding isolation with the same engine it compartmentalizes makes a theory bug a cross-tenant breach. The tenant's goal in [[actor-catalog]] is to be unreachable and unaffected by any other tenant, so that shape defeats the goal it serves.

The answer also settles a catalog entry: whether crossing the tenant boundary is one of the thief's paths through Kingo, or a defect outside it.

## Done when

A decision note states the shape, and the thief's paths in [[actor-catalog]] match it.
