# Underpass: design document

Living specification, consolidated from the design conversation. **Decided** is
the current agreement; **Proposed** is a starting point to test; **Open** is
unresolved. Latest decisions below supersede earlier drafts of the same rule.

## 1. Vision and pillars

**Decided.** Play as a sturdy robot exploring ever deeper beneath the earth.
Sections include sewers, rock and clay, caves, subterranean oceans and molten
regions, ultimately revealing another earth at the centre. The complete section
sequence and story remain open. Players should feel the world is enormous and
amazing, with striking colours, animations and views into deeper spaces.

- Curiosity and discovery drive descent.
- Learn attack patterns, find openings, position well and combine equipment.
- Enter enemy rooms, fight or escape, and choose how far to push.
- New sections introduce fresh aesthetics and gameplay mechanics.
- Avoid endless numerical power creep. Tactical players should succeed with
  weaker equipment; new tools create possibilities, not universal best builds.

The world is a continuous persistent expedition, not fresh runs. Sessions are
flexible: meaningful progress in a few minutes, or uninterrupted longer play.
Traditional puzzles are not a general focus. Combat observation provides the
problem-solving. No crafting.

## 2. Platforms and development

**Decided.** Native iOS is primary; phone and desktop browsers offer quick play.
Android follows later. Share logic/assets while designing and testing each
platform's playability. Landscape on phones. Single-player first; account for
future co-op in the architecture. Separate local saves per device/browser for
the first version. Story and music remain open.

Target a sustained **60 fps on iPhone 15 Pro**, adjusting effects as necessary.
The supported device range, minimum OS/browser and final budgets remain open.
Browser and native performance must be measured independently on real devices.

**Decided evaluation:** Unity-first technical trial; final engine commitment
depends on the results. Proposed workflow: GitHub source, Unity Build Automation,
browser previews and internal TestFlight builds. Development/builds mainly in
the cloud; the user's Mac with Xcode is available for occasional setup/debugging,
not required to run continuously. Apple enrollment/signing are pending.

## 3. Working world scope

**Proposed working scope:** about **20 layers: five sections of four layers**.
This replaces the earlier rough 100-layer idea and remains tunable.

Each layer contains rooms. The starting authored arrangement is a four-room
square, with two routes to a diagonal descent room:

```text
A: Start ---- B: Route
   |             |
C: Route ---- D: Boss + checkpoint + open hole
                 |
                 v
             Next layer
```

**Decided.** Room layouts, connections, gameplay obstacles, landmarks and
progression devices are designed by hand. Procedural generation supplies
environmental decoration only. Weapon variant generation is a separate system.
Generated scenery must preserve gameplay routes and readability.

**Decided.** Connections between rooms should fit their environment; explicit
framed doors are not required. In the sewer, a traversable pipe can connect rooms
on the same layer. Author these passages as part of the environment and preserve
readable routes for movement and escape. Upward routes are rare.

Rooms can, and often should, have missing sections of floor. Falling into these
**lethal floor gaps causes defeat**, following the normal checkpoint, territory
setback and equipment-loss rules; it does not transport the robot to another
layer. These gaps are deliberate authored terrain hazards, not missing geometry
or procedural decoration.

**Layer-descent holes are a separate feature and must appear absolute black.**
They transport the robot to the layer below rather than causing defeat. Keep
lethal floor gaps visually distinguishable from these absolute-black descent
holes, including under the robot's lights. The exact treatment of lethal gaps'
depth/edges remains to be established in art and playtesting.

Players can dodge through rooms and use layer-descent holes without defeating
enemies. The descent room holds the boss, checkpoint and an always-accessible
layer-descent hole. Killing the boss earns the layer checkpoint; passing it does
not.

Access is mostly open: no mandatory weapon/item requirement for success.
Hazards may damage health gradually; equipment can mitigate them. For example,
lava protection helps without making an otherwise impossible passage possible.

## 4. Room conquest and retreat

**Decided.** Defeat all hostile enemies to conquer a room and secure its finds.
Destructible scenery and non-hostile creatures do not need to be cleared.
Conquered rooms stay cleared normally, except for death-related territory
setbacks or specific world events.

Escape is always possible. Leaving an unconquered room resets its enemies and
forfeits newly found weapons/items from that room. Previously earned progress
remains. The precise treatment of old equipment discarded inside an unfinished
room is open; do not silently equate ordinary retreat with the quit snapshot.

## 5. Checkpoints, defeat and equipment loss

**Decided current rule:**

1. A checkpoint activates in a layer's boss room after defeating its boss.
2. Defeat returns the robot to its most recently activated checkpoint, at full
   health. Simply reaching a deeper layer does not activate a checkpoint.
3. **All conquered rooms in explored layers deeper than the checkpoint layer
   become hostile again.** Conquered rooms on that layer and shallower remain
   conquered. This is the initial rule, to tune in playtesting.
4. Weapons/items from lost rooms are lost if equipped or carried. Shared storage
   protects them regardless of their source room.
5. Reclearing replaces lost copies only. Equipment already protected in shared
   storage stays collected; do not award a duplicate of that discovery.
6. Earlier equipment remains relevant: after activating a layer-2 checkpoint,
   equipment from layers 1–2 survives a layer-6 defeat. Unprotected equipment
   from lost rooms in layers 3–6 does not.
7. The initial weapons remain a recovery fallback. If discarded and all other
   usable weapons are lost, restore that initial set.

This explicitly replaces the early proposal that all secured equipment always
survives defeat, and the earlier capped/proportional territory-reset proposals.

**Open:** first-game return point before any boss checkpoint, behaviour when
activating an older shallower checkpoint after a deeper one, and enhancement
provenance when old equipment gains a later power. The bootstrap provisionally
uses the starting room before the first boss checkpoint.

## 6. Interruptions and health

**Decided.** When stopping inside an unfinished room, reset that room and restore
the robot's **room-entry health and inventory**, then resume at the last cleared
room or safe place. This is not automatically full health. Completed progress
persists. Between sessions, save locally; durable serialization is not yet built.

Health is the baseline survival resource. Shields/protection are equipment
capabilities, not a universal regenerating system. Combat has limited healing
pickups, and designated safe places provide reliable recovery. Safe places and
boss checkpoints need not be identical locations.

## 7. Controls and camera

**Decided:**

- Left touch stick moves independently of aim.
- Right touch stick turns the torso and fires equipped guns together.
- Dragging elsewhere turns the torso/lights without firing.
- Desktop keyboard moves; mouse/trackpad aims; hold primary button to fire,
  release to stop. Moving the pointer alone does not fire.
- The upper body has unrestricted 360-degree rotation, independently of legs.
- The camera is top-down and follows the robot, not a whole-room fixed view.
- Ordinary movement, cover and positioning are the evasion baseline. No
  universal dash for now. Later mobility tools remain open.
- Inventory is accessible at any time and slows the world in single-player.
  Passive/active utility equipment is decided per item; exact controls and
  slowdown amount are to be prototyped. Co-op inventory behaviour is open.

Lighting determines visibility. Darkness, headlamps and environmental lights
matter. Some enemies detect the robot before it sees them. Off-screen warnings
are minimal; react to attacks and learn threats. Exact light/visibility ranges,
obstruction behaviour, camera framing and input tuning remain open.

## 8. Weapons, loadout and storage

**Decided:**

- Two light forearm mounts and two shoulder mounts. No shared weight/power budget.
- Light weapons fit arms or shoulders; heavy weapons fit shoulders only.
- Start with **two identical light arm guns**, shoulders empty; those guns also
  form the recovery fallback. All equipped guns fire toward a common aim point.
- Most weapons have unlimited ammunition with reload, firing-rate, damage and
  behaviour tradeoffs. Special weapons may use finite ammo, energy or heat.
- Limited carried reserve inventory uses simple slots: each weapon/item takes
  one slot. Equipped gear and shared storage are separate.
- Shared storage is accessed at designated safe places. It starts limited and
  expands through discovered capacity upgrades.
- Loot comes from hidden places, destructibles, abandoned machines and enemies.
- Sections have distinctive weapons, with repeated finds offering **bounded
  generated variants**. Trade accuracy, rate, damage and similar characteristics
  for comparable overall power suited to different styles. Validate in playtests.
- Finds/variants remain consistent within an adventure, including retries.
- Later environments, items or events can enhance earlier equipment in ways
  players cannot foresee, encouraging them to retain favourites. Decide whether
  each enhancement is permanent/situational as content is designed.

**Open:** reserve/utility/storage capacities, item stacking, specific weapon
behaviours and utility controls, enhancement persistence and provenance. The
bootstrap's eight reserve/storage slots and damage/interval rolls are temporary
test values, not agreed balance or a completed loot system.

## 9. Enemies and encounters

**Decided.** Include small tactical groups, busy rooms and occasional swarms.
Depth increases difficulty through patterns/openings, enemy combinations,
terrain and visibility, using stat increases sparingly. Bosses guard routes but
can be bypassed through harder-to-find/dangerous alternatives, including the
open descent hole. Boss rewards vary: equipment, routes or lasting area changes.

The first boss is a rat. A pipe-dwelling worm is a candidate for a lower sewer
layer, with exact placement/attacks open. For the prototype, the implementer may
choose ordinary enemies creatively; rats, heavier beetles and spiders are the
provisional starting roster, not a committed final bestiary.

## 10. Visual and model direction

**Decided.** Stylized 3D, striking colour/animation, enormous depth, fresh section
identities. Near the city, keep closer to recognizable reality; become more
fantastical deeper down. Iron Marines inspires chunky expressive shape/art
language, and Robokill inspires overhead robot readability.

Robot: sturdy broad torso, two heavy legs, broad feet, proportions conceivably
fitting a human pilot. Keep the occupant ambiguous. Torso-mounted directional
lamps plus 360-degree nearby illumination. A short soft-edged nearby pool and
longer beams are proposed; final radii/colour/intensity remain open.

Mixed arm/shoulder mounts are selected. **Keep original A (Bastion, rounded
blue) and B (Forge, squared industrial) as separate body candidates**, not the
later blended studies. Model choice, colours and materials remain open.

Original candidate sheet: [robot studies](concepts/robot-candidates.png).
These are concept images, not production meshes. Check all designs from the
actual top-down gameplay camera at phone size, in addition to close-up renders.

Sewer creatures: recognizable rats, insects, worms and other wildlife with
stylized/exaggerated proportions; bold, expressive, dangerous and characterful.
Animated impacts and defeats, **no gore**. Brief bodies then fade.

## 11. Sewer section

Working four-layer progression:

| Layer | Identity |
|---|---|
| 1 | Just below the city: light through manholes/runoff drains, puddles |
| 2 | More pipes, mechanical infrastructure and industrial systems |
| 3 | Deeper grey water |
| 4 | Beneath that: specific identity open |

Cool concrete, rusted pipes, amber work lights, vivid markings and deep shafts.
Use environment-native room passages such as traversable sewer pipes. Missing
floor sections provide lethal fall hazards, distinct from absolute-black holes
that lead to the next layer.
Industrial cycles define the section, with destructible infrastructure supporting
play. Water contributes to setting; deeper water mechanics belong to a later
section. Wildlife inhabits the machinery, rather than maintenance robots being
the sewer's principal enemies. Final cycle/encounter designs remain open.

## 12. Map and saving

**Decided.** Gradually reveal actual explored terrain and mark discovered room/
layer connections and safe places. Map UI, player annotations, representation of
territory setbacks and terrain persistence are open. Local saving must preserve
layout, finds, variants, inventory/storage and conquered/checkpoint state across
sessions; the current in-memory trial is not durable saving.

## 13. Milestones and verification

1. Prove cloud build → phone install with a minimal app; configure signing and
   internal TestFlight after Apple enrollment.
2. Unity technical trial: movement/independent aiming, illumination, timed
   machinery, combined fire, demanding combat and native/Web builds. Pin a
   repeatable workload and measure sustained iPhone/Safari results separately.
3. **Compact complete slice:** two layers, roughly 6–8 rooms (two four-room squares
   are a proposed eight-room layout), one boss checkpoint, alternative descent,
   combat, inventory, storage, room rewards, retreat, recovery and territory loss.
4. Review actual builds before committing to engine, renderer, production art
   pipeline or broader content. Expand only after the loop is fun and workable.

Keep rules independent from engine/presentation and benchmark tools independent
from normal gameplay. Use meaningful rule tests, actual Unity compilation/builds,
real-device measurements and matched visual review. Do not substitute desktop
or headless results for real-phone evidence.

## 14. Open and deferred decisions

Story/robot occupant reveal; music/sound design; final robot A/B, palette/materials;
specific first weapons/attacks; utility slots/controls; capacities; enhancement
rules; edge cases of stored/discarded/modified equipment; initial checkpoint;
map/save UX; accessibility/remapping/text/motion controls; remaining section
identities/counts; final renderer/engine; asset production standards; performance,
memory/download budgets; minimum devices; pricing/distribution details; co-op.

Baseline accessibility (proposed): readable scalable text, distinguish threats
without colour alone, adjustable sound, reduced flashes/shake and input remapping.
These need review and implementation, not a claim of existing support.
