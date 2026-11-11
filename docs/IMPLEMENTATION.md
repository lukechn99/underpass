# Implementation status

## Verified locally

`Assets/Scripts/Core` is shared C# with no engine references. Eleven scenario
checks passed using .NET 8's compiler/runtime against these exact source files:
starter mounts, compatibility, boss checkpoint gating, the layer-2/layer-6 death
example, storage protection/deduplication, retry consistency, stop restoration,
starter fallback, deeper checkpoint protection, inventory capacity and bounded
deterministic rolls. GitHub Actions runs the same scenarios via `dotnet run`.

The current managed sandbox cannot initialize the normal .NET CLI migration
mutex under its read-only user home. Local verification invoked the SDK's C#
compiler directly and ran the resulting assembly; no HOME override or weakened
game tests were used. CI runs in a normal writable runner environment.

## Source prepared; NOT validated in Unity yet

- Unity 6000.3.28f1 project, build entry scene and preparation/export hooks.
- Runtime whitebox: two authored four-room layers, movement, free torso aiming,
  mouse hold-fire, raw touch sticks/look gestures, nearby/spot lighting, simple
  rat/beetle/spider actors, first rat boss, steam-cycle hazard, crates, descent,
  inventory swapping/storage, recovery and deliberate defeat.
- Pooled shot lines and shared palette materials avoid per-shot object creation
  and per-object duplicate materials. Layer geometry is rebuildable prototype
  code, not the eventual asset/streaming pipeline.
- The first boss has no additional hostile actors, so boss defeat and room
  conquest coincide for this initial trial.

No Unity editor/license is configured locally. The official Linux editor alone
has an installed size larger than this workspace's available disk, before adding
Web/iOS modules. Use the configured cloud build service as the first Unity gate.
There is no successful Unity/Web/iOS compilation, screenshot, phone install or
performance measurement yet. Treat the whitebox source as needing that gate.

## Next, in order

1. Run Unity import and the Web build in Build Automation; fix reported errors.
2. Download/run that exact Web artifact; verify keyboard/mouse, input mapping,
   camera, light visibility, collision, room transitions, inventory and pause.
3. Export/sign a minimal iOS app and install it through internal TestFlight.
4. Implement durable versioned local saves with stable discovery identities and
   transaction snapshots; verify real phone background/termination behaviour.
5. Complete gameplay visibility, gradual terrain mapping, healing drops, reloads,
   passive/active utilities, discovered storage upgrades, real attack patterns
   and defeated-body fading. These are not complete in the bootstrap.
6. Replace debug UI with mobile-safe layouts, handle safe areas, accessibility,
   discovery/reward feedback and loading/transition UX.
7. Build actual authored assets, review robot A/B in matched gameplay captures,
   and establish section art/model/material standards.
8. Isolated benchmark scene/replay, iPhone 15 Pro measurements, sustained thermal
   runs and renderer/engine acceptance. Do not claim 60 fps before measurements.

## Provisional choices, not new product decisions

Eight reserve/storage slots, 20% inventory time scale, fixed world seed 1234,
prototype safe places in the initial/boss rooms, starting-room return before any
checkpoint, instant placeholder layer transition, basic hitscan guns and equal-DPS
damage/interval trades. Built-in renderer is a bootstrap baseline; production
rendering is still subject to the technical trial.

The debug map reveals room labels for testing. It is not the agreed terrain-map
feature. The placeholder descent marker is not final hole geometry or animation.
