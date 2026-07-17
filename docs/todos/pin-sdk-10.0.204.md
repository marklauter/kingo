---
title: Pin global.json to SDK 10.0.204 with rollForward disable
summary: "global.json currently pins 10.0.203 / latestFeature; bump to 10.0.204 and set rollForward to disable to match the house SDK version."
tags: [note, todo, global-json, sdk, house-canon]
created: 2026-07-16
priority: medium
effort: low
status: open
---

Update `global.json` to pin the .NET SDK to `10.0.204` and set
`rollForward` to `disable` rather than keeping `latestFeature`.

Current: version `10.0.203`, rollForward `latestFeature`.

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

Before merging, confirm the GitHub-hosted runner image (`ubuntu-latest`) actually
carries `10.0.204`. If it doesn't, either the workflow must add an explicit
`dotnet-version: 10.0.204` to install it, or the pin will red the build — which is
the intended fail-loud behavior, not a regression.

Confirm any `Directory.Build.props` `TargetFramework` still aligns after the bump,
then run `build-gate.sh` to verify.
