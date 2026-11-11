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

## Unity Web build 1: compiled; startup fixes awaiting rebuild

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

Unity Build Automation build 1 successfully compiled commit `cfddc11` with
Unity 6000.3.28f1. The exact downloaded Web artifact was exercised in desktop
Chrome using software rendering. Inventory UI responded, but the environment
was black and runtime logs reported missing BoxCollider and CapsuleCollider
classes. Runtime-created primitives need explicit linker preservation because
the empty entry scene has no serialized references to these component types.

The follow-up preserves only the primitive mesh/collider types used by the
trial, disables decorative colliders immediately before deferred destruction,
synchronizes physics transforms before placing the controller, and explicitly
selects flat ambient lighting. Missing required colliders now fail explicitly.
Browser builds also skip the unsupported orientation-lock call. These source
fixes require a new Unity artifact before claiming playable startup. Native iOS,
phone input, gameplay progression and performance remain unverified.

## Next, in order

1. Rebuild main in Build Automation to validate the startup fixes.
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
