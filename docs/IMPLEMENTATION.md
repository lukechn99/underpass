# Implementation status

## Verified locally

`Assets/Scripts/Core` is shared C# with no engine references. Fifteen scenario
checks passed using .NET 8's compiler/runtime against these exact source files:
starter mounts, compatibility, boss checkpoint gating, the layer-2/layer-6 death
example, storage protection/deduplication, retry consistency, stop restoration,
starter fallback, deeper checkpoint protection, inventory capacity and bounded
deterministic rolls, save JSON round-trip, unfinished-fight capture, invalid-save
rejection and Unity-compatible starter strings. GitHub Actions runs the same scenarios via `dotnet run`.

The current managed sandbox cannot initialize the normal .NET CLI migration
mutex under its read-only user home. Local verification invoked the SDK's C#
compiler directly and ran the resulting assembly; no HOME override or weakened
game tests were used. CI runs in a normal writable runner environment.

## Unity artifact verification

Build Automation build 2 compiled `cea8006` with Unity 6000.3.28f1. Its downloaded
artifact was tested in desktop Chrome with software rendering. Floors/scenery
render, the robot remains grounded during movement, inventory opens and deliberate
defeat/recovery responds. No missing primitive collider or orientation-lock errors
were captured. This verifies the startup fixes following build 1; it does not
verify native rendering, phone controls, complete progression or performance.

## Local persistence: core tested; Unity adapter awaiting build

- Engine-independent, versioned DTOs capture completed progress and room-entry
  health/inventory for unfinished encounters without mutating the live fight.
- Restore creates fresh room instances and validates room/checkpoint references,
  inventory placement/capacity, equipment and mount compatibility before use.
- Unity adapter serializes with JsonUtility to PlayerPrefs; primary/backup saves
  are validated on load. If both are unreadable, preserve them and play a clearly
  labelled temporary session rather than overwrite recoverable data.
- Periodic saves every unscaled second plus conquest, defeat, inventory toggles,
  pause/focus/quit callbacks. Unchanged snapshots do not write storage.
- PlayerPrefs uses IndexedDB on Web builds; browser flushing, quota failures,
  native app termination and real iPhone behaviour still require artifact/device
  tests. The final second of changes may be lost on abrupt termination. No cloud
  sync, account save or cross-device progress is introduced.

## Next, in order

1. Rebuild main to compile the persistence adapter; test reload and backup recovery.
2. Download/run that exact Web artifact; verify keyboard/mouse, input mapping,
   camera, light visibility, collision, room transitions, inventory and pause.
3. Export/sign a minimal iOS app and install it through internal TestFlight.
4. Verify local saves on real phone background/termination and storage failure;
   add migrations when the save schema or authored world changes.
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
