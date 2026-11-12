# Implementation status

## Verified locally

`Assets/Scripts/Core` is shared C# with no engine references. Nineteen scenario
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

## Local persistence: build 3 browser checks passed

- Engine-independent, versioned DTOs capture completed progress and room-entry
  health/inventory for unfinished encounters without mutating the live fight.
- Restore creates fresh room instances and validates room/checkpoint references,
  inventory placement/capacity, equipment and mount compatibility before use.
- Unity adapter serializes with JsonUtility to PlayerPrefs; primary/backup saves
  are validated on load. If both are unreadable, preserve them and play a clearly
  labelled temporary session rather than overwrite recoverable data.
- Periodic saves every unscaled second plus conquest, defeat, inventory toggles,
  pause/focus/quit callbacks. Unchanged snapshots do not write storage.
- Build 3 (`d872917`) was tested using the exact downloaded Unity artifact in
  desktop Chrome. A safe-room unequip survives closing/reopening the game tab.
  Entering hostile L1-B, taking damage and unequipping a gun, then closing the tab
  restores L1-A, health 100 and both entry-mounted guns. In an isolated test
  profile, changing the primary JSON version to an unsupported value causes a
  validation warning and successful backup recovery. No runtime errors occurred.
- PlayerPrefs IndexedDB records were inspected to confirm the saved loadout.
  These tests reused the same browser context across new tabs; they do not prove
  full browser-process restart, OS termination, storage quota or iPhone behaviour. The final second of changes may be lost on abrupt termination. No cloud
  sync, account save or cross-device progress is introduced.

## Exploration map: build 6 browser checks passed

- Room discovery is independent of conquest; retreat, defeat and unfinished-room
  rollback retain knowledge without preserving unearned equipment.
- A separate Unity map presenter draws explored room footprints, shared authored
  cover/pipe/hazard offsets, connections between visited rooms, safe/checkpoint
  markers, the layer-1 descent and current player position.
- Save schema 2 stores discoveries. Schema 1 remains readable and migrates only
  known safe/conquered rooms; it cannot reconstruct visits never saved by schema 1.
- Four additional scenarios verify discovery lifetime, unfinished-fight saves,
  migration and invalid discoveries. Nineteen core scenarios pass locally.
- This is a room-level map milestone. Within-room terrain reveal, layer browsing,
  lighting occlusion and final cartographic art remain future work.
- Build 6 (`4e7e02a`, automatically triggered via polling) starts cleanly. A real
  version-1 equipment save loaded in the map build with its loadout intact. The
  initial map reveals only A; entering B reveals its footprint/obstacles and the
  A–B connection. Restoring saved IndexedDB data in a fresh browser context keeps
  B mapped while returning the robot to A with room-entry health.

## Web update caching: packaging fix awaiting artifact

The test server's in-place build-3 → build-6 upgrade in a warm browser context
failed with a loader `RangeError: Maximum call stack size exceeded`. Fresh-context
migration passed, and repeating the same-address upgrade with HTTP caching
bypassed also passed while keeping the local save database. This points to stale
HTTP build assets rather than the save migration; the exact cached-file mismatch
was not isolated.

The build hook now enables `PlayerSettings.WebGL.nameFilesAsHashes` so changed
binary contents receive distinct URLs. Verify the generated filenames and repeat
the warm-cache upgrade with the next artifact before treating this as resolved.
A production host should revalidate `index.html` and publish the build atomically.
Do not clear the local save database to work around asset caching.

## Floor hazards and pipe passages: source prepared; artifact pending

- Floors are partitioned around authored rectangular openings, with no floor
  colliders inside them. Lethal shafts have amber rims and lit, visible inner
  surfaces; the layer-1 descent uses an unlit absolute-black interior and blue rim.
- A fall below the floor threshold triggers normal defeat, except within the
  authored layer-descent opening, which enters layer 2 without requiring conquest.
  Inventory slow motion no longer suspends character gravity.
- Cutaway lower pipe shells, metal bands, walkways and side collision replace
  plain inter-room floor strips. The top remains open for the gameplay camera.
- The exploration map uses the same opening definitions and distinguishes lethal
  shafts from descent. Runtime-generated pipe meshes are destroyed on layer rebuild.
- Layout validation runs before cloud builds and at startup: keep gaps inside room
  edges, prevent overlap between opening types, and protect recovery, cover,
  equipment and enemy spawn positions. Ground creatures stop short of openings;
  full pathfinding around them is not implemented.
- No new core progression rules: falls reuse existing defeat/descent flows. Unity
  compilation, fall collision, black-hole lighting, recovery, map accuracy and
  phone-scale pipe visibility require verification in the next build.

## Next, in order

1. Verify floor hazards, descent, pipe collisions and content-hashed output in the next Web artifact.
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

The map reveals visited room footprints, not terrain gradually within a room.
Descent now has an actual floor opening; final falling/transition animation remains open.
