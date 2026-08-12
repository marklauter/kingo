---
title: Why Kingo must exist
type: note
summary: "<todo: write this at the end>"
tags: [architecture, vision]
created: 2026-08-08
status: evolving
---

# Kingo exists because ... <todo: write this at the end> ...

<todo: this derivation is wrong - it's for a bank, github, or youtube - not for the security system>

```
owner owns resources
  → adversarial actors misuse resources
    → owner wants to protect resources from misuse
      → owner must restrict access to trusted actors
        → trust must be enforced at scale
          → intent must be mechanized
```

Scale is what makes the last two steps necessary rather than convenient. An owner who deals with a handful of actors extends trust by knowing them, and enforces it by being present. Neither survives volume, so trust stops resting on the owner and the intent has to exist in a form a machine applies.

A mechanism applies an intent it was told. The first way to tell it is a list: name every party permitted to reach every resource. The list works, and it is where the design breaks. Its size is parties × resources, someone maintains it by hand, and a platform hosting many owners multiplies both. The cost of saying who may reach what overtakes the worth of saying it.

What remains is deriving access from relationships the organization already keeps for other reasons: membership in a group, a document's folder, a file's author. Those relationships are already there. Access is computed from them.

## Actors

- The **requester** wants to reach material. Their goal alone produces no system; an unlocked door satisfies it. The constraint that produces one is the owner's.
- The **resource owner** wants their material reached only by whom they intend. On a platform they are often an ordinary user: someone publishing a video owns the video, and the platform owns the system.
- The **system owner** operates the platform. Wanting it trustworthy is a goal of their own, not a service to the resource owner.

The **nefarious actor** is not derived from anyone's values. They exist on their own, and their existence turns a convention into a system. Incompetence counts with malice: both defeat an unenforced intent, so the design answers both the same way.

## Use cases