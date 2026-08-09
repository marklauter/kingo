---
title: Why Kingo must exist
type: note
summary: "Kingo is derived from a tension rather than assumed: owners want their material reached only by whom they intend, adversaries make intent unenforceable by convention, the access list that enforces it costs people × resources to maintain, and deriving access from existing relationships is what remains."
tags: [architecture, vision]
created: 2026-08-08
status: evolving
---

# Kingo is derived from a tension, not assumed

The chain runs before the system exists.

People want to reach material they do not own. Owners want their material reached only by whom they intend. With no adversary that costs nothing — a stated convention would hold.

Adversarial and incompetent actors exist. Intent has to be enforced rather than assumed, which is why the posture is low trust by construction rather than by precaution.

The first enforcement is a list: name every party permitted to reach every resource. It works.

The list is where it breaks. Its size is parties × resources, and someone maintains it by hand. A platform hosting many owners multiplies both. The goal — reached only by whom I intend — collides with the cost of saying so.

What remains is deriving access from relationships that already exist and are already maintained for other reasons: membership in a group, a document's folder, a file's author. The graph is already there, so access is computed from it rather than stated.

```
people want to reach material they do not own
  → owners want to control who reaches theirs
    → adversarial and incompetent actors exist (tension)
      → intent must be enforced, not assumed → low trust
        → enumerate the permitted (solution: the access list)
          → the list is parties × resources, hand-maintained (new tension)
            → derive access from existing relationships (system under design)
```

## Actors

- The **requester** wants to reach material. Their goal alone produces no system — an unlocked door satisfies it. The constraint that produces an authorization system is the owner's, not theirs.
- The **resource owner** wants their material reached only by whom they intend. On a platform they are often an ordinary user: someone publishing a video owns the video, and the platform owns the system.
- The **system owner** operates the platform. Wanting it trustworthy is a goal of their own, not a service rendered to the resource owner.

The **nefarious actor** is not derived from anyone's values. They exist independently, and their existence is what turns a convention into a system. Incompetence sits beside malice: both defeat an unenforced intent, which is why the two produce the same design pressure.

## Use cases