---
title: Schema dissolves into administration
summary: "The schema stops being a domain type: namespaces are globally unique per installation and are the versioned config aggregate; a schema survives only as the administrative unit of ownership and atomic change. Shared groups work; isolation becomes administrative, not referential."
tags: [decision, schemas, evaluation, architecture]
created: 2026-07-21
status: locked
---

# Schema dissolves into administration

Decided 2026-07-21, mid-clean-room ([[rewrite-interpreters]]), when the evaluation context's schema port forced the question of what a schema *is*.

Namespaces are the versioned config entity, globally unique per installation, and schema-agnostic — as facts already are. The `Schema` domain class goes away: what SDL's `schema:` key names becomes purely administrative — the unit of ownership (who may edit which namespaces) and of atomic config change (an SDL push is one Write transaction over its namespace configs). Zanzibar's own architecture: per-namespace configs, flat global references, administration layered on top.

Consequences:

- **`Namespace` returns to config aggregate root** — it is the stored, versioned artifact. `Schema`, `SchemaIdentifier`, and `SchemaVersion` retire as domain types; the aggregate table in [[domain-language]] reverts on the config side.
- **The evaluation context reads namespaces, not schemas.** The schema-reader port becomes a namespace reader; the prepared projection becomes a prepared catalog — every namespace at the config pin — keeping error-taxonomy family 1 pure (undefined namespace/relationship detectable before any I/O). Lazy per-namespace loading mid-walk was rejected for breaking that purity.
- **One pin covers everything.** Namespace configs version on the same store timeline as facts ([[drift-prevention-at-the-write-edges]]), so the [[kookie]] alone pins both fact state and config state. `Decision` and `Expansion` drop the schema-version slot; the coherent (`Kookie`, `SchemaVersion`) pair collapses into the one pin.
- **Global uniqueness and duplicate checks move to Write-against-store** — not expressible as domain invariants once no aggregate spans namespaces.
- **Isolation becomes administrative.** Nothing structural stops one namespace's rewrite referencing another team's namespace; team boundaries are edit permissions over name prefixes, not referential walls. Hierarchical organization is naming convention (`youtube.group`), which requires adding `.` to the `<namespace>` character rule.

## Alternatives

- **Schema as bounded scope.** A schema is a referential wall: rewrites and facts resolve only within it; cross-schema facts are unrepresentable; isolation is an invariant. Aligns administration, versioning, and evaluation on one boundary and keeps the existing aggregate model intact. Lost on the shared-groups problem: an org-wide `group:eng#member` has no schema that can own it usefully — every schema would carry its own membership copy, resurrecting the group-sync drift that centralized ReBAC exists to kill, unless a governed cross-schema publishing mechanism is designed. The relief valve is speculative machinery; the pathology is certain.
- **Schema as curation with the class retained.** Namespaces globally unique but `Schema` kept as a stored root grouping them. Lost because the versioned entity evaluation and replay care about is the namespace; a retained `Schema` root either versions things it doesn't own or dwindles to a container earning no aggregate status — the administrative role doesn't need a domain type.

## Why

It costs a remodel: re-promoting `Namespace` to root, retiring three minted types, reverting the config side of the aggregate table, amending the drift decision's pair into one pin, and reframing SDL's envelope semantics. It reverses settled work (the 2026-07-15 aggregate collapse, `schema.empty`, the composite `SchemaVersion`). This is the cheapest moment it will ever be: no hosts exist and the interpreters are unwritten.

It buys Zanzibar's proven shape — shared groups work flat, with no cross-schema ceremony — deletes a concept instead of adding one (no catalog-version type: the kookie already pins config), simplifies routing to pin resolution alone, and gives `Decision` a four-slot shape whose one pin replays both facts and rules.
