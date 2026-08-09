---
title: Why Kingo must exist
type: note
summary: "<todo: write this at the end>"
tags: [architecture, vision]
created: 2026-08-08
status: evolving
---

# Kingo exists because ... <todo: write this at the end> ...

```
an owner has resources to share
  → adversarial actors misuse resources
    → the owner wants to protect the resources from misuse
      → the owner must restrict access to trusted actors
        → intent must be enforced (low trust)
```

Without an adversary an owner's intent needs no enforcement. Saying it would be enough. Adversaries and incompetents are why it has to be enforced, and why nothing is trusted by default.

The access list enforces it. Its size is parties × resources and someone maintains it by hand, and a platform hosting many owners multiplies both. The cost of saying who may reach what overtakes the worth of saying it.

What remains is deriving access from relationships the organization already keeps for other reasons: membership in a group, a document's folder, a file's author. Those relationships are already there. Access is computed from them.

## Actors

- The **requester** wants to reach material. Their goal alone produces no system; an unlocked door satisfies it. The constraint that produces one is the owner's.
- The **resource owner** wants their material reached only by whom they intend. On a platform they are often an ordinary user: someone publishing a video owns the video, and the platform owns the system.
- The **system owner** operates the platform. Wanting it trustworthy is a goal of their own, not a service to the resource owner.

The **nefarious actor** is not derived from anyone's values. They exist on their own, and their existence turns a convention into a system. Incompetence counts with malice: both defeat an unenforced intent, so the design answers both the same way.

## Use cases