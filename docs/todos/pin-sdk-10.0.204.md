---
title: Pin global.json to SDK 10.0.204 with rollForward disable
summary: "Closed: global.json pins 10.0.204 with rollForward disable — the full target. CI is hermetic and fail-loud: it installs exactly 10.0.204 or reds the build."
tags: [note, todo, global-json, sdk, house-canon]
created: 2026-07-16
priority: medium
effort: low
status: closed
---

Update `global.json` to pin the .NET SDK to `10.0.204` and set
`rollForward` to `disable` rather than keeping `latestFeature`.

Current at writing: version `10.0.203`, rollForward `latestFeature`.

Target (matches lexi, the house canon):

```json
{
  "sdk": {
    "version": "10.0.204",
    "rollForward": "disable"
  }
}
```

## CI implication

The `setup-dotnet` composite action installs via `global-json-file: global.json`,
so the CI SDK pin flows straight from this file — no workflow hardcodes
`dotnet-version`. With `rollForward: disable`, CI becomes hermetic: it installs
*exactly* `10.0.204` and fails loudly if the runner image doesn't ship it, rather
than silently rolling forward to a newer patch/feature band.

## Resolution

Closed 2026-07-21. `global.json` pins `10.0.204` with `rollForward: disable` —
the full target. Local resolution verified (`dotnet --version` → 10.0.204);
gate green under the pin. CI needed no edit, and the runner-image caveat above
was overcautious: `actions/setup-dotnet@v5` with `global-json-file` *installs*
the resolved version from the release feed rather than selecting from the
image, so the workflow installs exactly `10.0.204` wherever it runs and the
pin flows from this one file.
