using System;
using System.Collections.Generic;
using System.Linq;
using Underpass.Core;
using UnityEngine;
using static Underpass.Prototype.SewerLayout;

namespace Underpass.Prototype
{
    // Deliberately replaceable whitebox presentation. No benchmark/UI dependency in Core.
    public sealed partial class TrialWorld : MonoBehaviour
    {
        sealed class Creature
        {
            public GameObject Model;
            public string Room;
            public int Kind;
            public float Health;
            public float Cooldown;
            public float Cycle;
            public Vector3 Charge;
        }
        readonly List<Creature> creatures = new List<Creature>();
        readonly Dictionary<string, GameObject> crates = new Dictionary<string, GameObject>();
        readonly Dictionary<string, float> lastShot = new Dictionary<string, float>();
        readonly Dictionary<Color, Material> palette = new Dictionary<Color, Material>();
        readonly Dictionary<int, Renderer> vents = new Dictionary<int, Renderer>();
        readonly LineRenderer[] shots = new LineRenderer[32];
        readonly float[] shotExpiry = new float[32];
        int nextShot;
        Expedition state;
        readonly LocalExpeditionStore saves = new LocalExpeditionStore();
        float nextSave;
        GameObject layerRoot, robot;
        Transform torso, leftLeg, rightLeg;
        CharacterController motor;
        Camera view;
        Light nearbyLight;
        int layer = 1;
        bool inventory;
        bool showMap;
        bool firing;
        Vector3 aim = Vector3.forward;
        Vector2 movement;
        int leftFinger = -1, rightFinger = -1;
        Vector2 leftOrigin, rightOrigin;
        Vector2 leftThumb, rightThumb;
        float gait;
        Material solid, unlit, abyss;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartTrial()
        {
            if (FindFirstObjectByType<TrialWorld>() == null) new GameObject("Underpass trial").AddComponent<TrialWorld>();
        }

        void Start()
        {
            SewerLayout.Validate();
            Application.targetFrameRate = 60;
#if !UNITY_WEBGL
            Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
            solid = Resources.Load<Material>("TrialSolid");
            unlit = Resources.Load<Material>("TrialUnlit");
            if (solid == null || unlit == null)
                throw new InvalidOperationException("Trial materials missing. Run Underpass > Prepare trial in Unity.");
            abyss = new Material(unlit) { color = Color.black };
            for (int i = 0; i < shots.Length; i++)
            {
                shots[i] = new GameObject("Pooled shot " + i).AddComponent<LineRenderer>();
                shots[i].transform.SetParent(transform); shots[i].sharedMaterial = unlit;
                shots[i].startWidth = .05f; shots[i].endWidth = .02f;
                shots[i].positionCount = 2; shots[i].enabled = false;
            }
            var rooms = new List<Room>();
            for (int floor = 1; floor <= 2; floor++)
                for (int i = 0; i < 4; i++) rooms.Add(new Room(Id(floor, i), floor,
                    boss: floor == 1 && i == 3, safePlace: floor == 1 && i == 0));
            state = saves.Load(rooms, Id(1, 0));
            if (state == null)
            {
                state = new Expedition(rooms, Id(1, 0));
                state.Enter(Id(1, 0)); state.Conquer(0);
            }
            layer = state.Rooms[state.CurrentRoom].Layer;
            saves.Save(state);
            BuildRobot();
            var cameraObject = new GameObject("Following top-down camera");
            view = cameraObject.AddComponent<Camera>();
            view.orthographic = true; view.orthographicSize = 9;
            view.transform.rotation = Quaternion.Euler(90, 0, 0);
            view.backgroundColor = new Color(.015f, .02f, .028f);
            view.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.035f, .04f, .05f);
            QualitySettings.pixelLightCount = 8;
            BuildLayer(); PlaceAtCurrentRoom();
        }

        GameObject Shape(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, Transform parent, bool collision = true)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Tint(color);
            var collider = obj.GetComponent<Collider>();
            if (collision && collider == null)
                throw new InvalidOperationException("Primitive collider missing for " + name + ". Check Assets/link.xml stripping rules.");
            if (!collision && collider != null)
            {
                collider.enabled = false;
                Destroy(collider);
            }
            return obj;
        }

        Material Tint(Color color)
        {
            if (!palette.TryGetValue(color, out Material material))
            {
                material = new Material(solid); material.color = color; material.SetFloat("_Glossiness", .2f);
                palette.Add(color, material);
            }
            return material;
        }

        void BuildRobot()
        {
            robot = new GameObject("Robot whitebox — final A/B body undecided");
            motor = robot.AddComponent<CharacterController>(); motor.height = 1.9f; motor.radius = .55f; motor.center = Vector3.up * .95f;
            var blue = new Color(.1f, .28f, .52f);
            leftLeg = Shape(PrimitiveType.Cube, "Heavy left leg", new Vector3(-.35f,.45f,0), new Vector3(.5f,.9f,.8f), blue, robot.transform, false).transform;
            rightLeg = Shape(PrimitiveType.Cube, "Heavy right leg", new Vector3(.35f,.45f,0), new Vector3(.5f,.9f,.8f), blue, robot.transform, false).transform;
            torso = new GameObject("Unrestricted rotating upper body").transform; torso.SetParent(robot.transform, false);
            Shape(PrimitiveType.Cube, "Armoured chest", Vector3.up * 1.3f, new Vector3(1.35f,.9f,1.1f), blue, torso, false);
            Shape(PrimitiveType.Cube, "Left light gun", new Vector3(-.8f,1.1f,.45f), new Vector3(.35f,.4f,1.1f), Color.gray, torso, false);
            Shape(PrimitiveType.Cube, "Right light gun", new Vector3(.8f,1.1f,.45f), new Vector3(.35f,.4f,1.1f), Color.gray, torso, false);
            nearbyLight = new GameObject("360-degree nearby illumination").AddComponent<Light>();
            nearbyLight.transform.SetParent(robot.transform, false); nearbyLight.transform.localPosition = Vector3.up * 2;
            nearbyLight.type = LightType.Point; nearbyLight.range = 11; nearbyLight.intensity = 2.2f; nearbyLight.color = new Color(.75f,.9f,1);
            for (int i = 0; i < 2; i++)
            {
                var light = new GameObject("Directional torso lamp").AddComponent<Light>(); light.transform.SetParent(torso, false);
                light.transform.localPosition = new Vector3(i == 0 ? -.45f : .45f, 1.5f, .6f);
                light.transform.localRotation = Quaternion.Euler(18,0,0);
                light.type = LightType.Spot; light.range = 22; light.spotAngle = 65; light.intensity = 5;
                light.shadows = LightShadows.Hard;
            }
        }

        void BuildLayer()
        {
            if (layerRoot != null) { layerRoot.SetActive(false); Destroy(layerRoot); }
            foreach (Mesh mesh in terrainMeshes) Destroy(mesh); terrainMeshes.Clear();
            layerRoot = new GameObject("Sewer layer " + layer); creatures.Clear(); crates.Clear(); vents.Clear();
            var concrete = new Color(.24f,.28f,.32f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 c = Centre(i);
                BuildRoomFloor(i, concrete);
                // Door gaps and outer boundaries are authored; no procedural gameplay layout.
                for (int side = 0; side < 4; side++)
                {
                    bool alongX = side < 2;
                    float sign = side % 2 == 0 ? -1 : 1;
                    bool door = alongX ? (sign < 0 ? i % 2 == 1 : i % 2 == 0) : (sign < 0 ? i / 2 == 1 : i / 2 == 0);
                    Vector3 edge = alongX ? new Vector3(sign * 10,1.2f,0) : new Vector3(0,1.2f,sign * 10);
                    if (!door) Shape(PrimitiveType.Cube, "Outer wall", c + edge, alongX ? new Vector3(.6f,2.4f,20) : new Vector3(20,2.4f,.6f), concrete, layerRoot.transform);
                    else
                    {
                        for (int part = -1; part <= 1; part += 2)
                            Shape(PrimitiveType.Cube, "Door jamb wall", c + edge + (alongX ? Vector3.forward : Vector3.right) * part * 6,
                                alongX ? new Vector3(.6f,2.4f,8) : new Vector3(8,2.4f,.6f), concrete, layerRoot.transform);
                        if (sign > 0) BuildPipePassage(c + (alongX ? Vector3.right : Vector3.forward) * 11, alongX);
                    }
                }
                Shape(PrimitiveType.Cylinder, "Rust pipe", c + PipeOffset, new Vector3(.8f,1,.8f), new Color(.45f,.22f,.1f), layerRoot.transform);
                if (i == 1 || i == 2) vents[i] = Shape(PrimitiveType.Cylinder, "Timed steam hazard", c + SteamOffset,
                    new Vector3(4,.03f,4), Color.gray, layerRoot.transform, false).GetComponent<Renderer>();
                Shape(PrimitiveType.Cube, "Cover block", c + CoverOffset, new Vector3(2,1.2f,2), concrete, layerRoot.transform);
                var lamp = new GameObject(layer == 1 ? "Light from city opening" : "Amber work light").AddComponent<Light>();
                lamp.transform.SetParent(layerRoot.transform); lamp.transform.position = c + Vector3.up * 5;
                lamp.type = LightType.Point; lamp.range = layer == 1 ? 15 : 7; lamp.intensity = layer == 1 ? 1.5f : .6f;
                lamp.color = layer == 1 ? new Color(.85f,.9f,1) : new Color(1,.55f,.2f);
                string roomId = Id(layer, i);
                var crate = Shape(PrimitiveType.Cube, "Breakable equipment crate", c + new Vector3(-3,.4f,2), Vector3.one * .8f, new Color(.8f,.5f,.12f), layerRoot.transform);
                crates[roomId] = crate;
                if (state.Owned.ContainsKey(roomId + "/equipment")) crate.SetActive(false);
                SpawnRoom(i);
            }

        }

        void SpawnRoom(int index)
        {
            string id = Id(layer, index);
            foreach (Creature old in creatures.Where(e => e.Room == id).ToArray()) { if (old.Model != null) { old.Model.SetActive(false); Destroy(old.Model); } creatures.Remove(old); }
            if (crates.TryGetValue(id, out GameObject crate)) crate.SetActive(!state.Owned.ContainsKey(id + "/equipment"));
            if (state.Rooms[id].Conquered) return;
            int count = layer == 1 && index == 3 ? 1 : 3;
            for (int i = 0; i < count; i++)
            {
                int kind = index == 1 ? 0 : index == 2 ? i % 2 + 1 : 0;
                bool boss = state.Rooms[id].Boss;
                Color color = kind == 0 ? new Color(.44f,.3f,.22f) : kind == 1 ? new Color(.27f,.36f,.18f) : new Color(.23f,.2f,.28f);
                var model = Shape(PrimitiveType.Capsule, boss ? "Rat boss whitebox" : kind == 0 ? "Rat" : kind == 1 ? "Beetle" : "Spider",
                    Centre(index) + new Vector3(i * 2 - 2, .8f, 5), Vector3.one * (boss ? 1.9f : 1), color, layerRoot.transform);
                creatures.Add(new Creature { Model = model, Room = id, Kind = kind, Health = boss ? 200 : kind == 1 ? 65 : 35 });
            }
        }

        void Update()
        {
            if (state == null) return;
            if (Time.unscaledTime >= nextSave)
            {
                saves.Save(state); nextSave = Time.unscaledTime + 1;
            }
            for (int i = 0; i < shots.Length; i++) if (Time.time >= shotExpiry[i]) shots[i].enabled = false;
            if (Input.GetKeyDown(KeyCode.I)) ToggleInventory();
            if (Input.GetKeyDown(KeyCode.Tab)) showMap = !showMap;
            if (Input.GetKeyDown(KeyCode.F9)) Die();
            ReadControls();
            if (!inventory)
            {
                Vector3 move = new Vector3(movement.x,0,movement.y);
                motor.SimpleMove(move * 5);
                gait += move.magnitude * Time.deltaTime * 12;
                leftLeg.localRotation = Quaternion.Euler(Mathf.Sin(gait) * move.magnitude * 18,0,0);
                rightLeg.localRotation = Quaternion.Euler(-Mathf.Sin(gait) * move.magnitude * 18,0,0);
                if (aim.sqrMagnitude > .01f) torso.rotation = Quaternion.LookRotation(aim);
                if (firing) Fire();
                ConquerIfClear();
                int room = (robot.transform.position.x > 11 ? 1 : 0) + (robot.transform.position.z > 11 ? 2 : 0);
                string target = Id(layer, room);
                if (target != state.CurrentRoom)
                {
                    string previous = state.CurrentRoom;
                    if (state.RoomInProgress) { state.Retreat(); SpawnRoom(Index(previous)); }
                    state.Enter(target);
                }
                if (Input.GetKeyDown(KeyCode.E)) Collect();
            }
            else motor.SimpleMove(Vector3.zero); // Gravity continues while inventory slows the world.
            if (ResolveFloorFall()) return;
            TickCreatures();
            // A repeatable steam cycle in the two route rooms; this is authored gameplay.
            int current = Index(state.CurrentRoom);
            foreach (Renderer vent in vents.Values) vent.sharedMaterial = Tint(Time.time % 8 < 3 ? new Color(.85f,.25f,.08f) : Color.gray);
            if ((current == 1 || current == 2) && Time.time % 8 < 3 &&
                Vector3.Distance(robot.transform.position, Centre(current) + new Vector3(-7,0,0)) < 2)
                state.Damage(15 * Time.deltaTime);
            if (state.Health <= 0) Die();
            ConquerIfClear();
        }

        void ConquerIfClear()
        {
            if (state.RoomInProgress && !creatures.Any(e => e.Room == state.CurrentRoom && e.Health > 0))
            {
                state.Conquer(0); saves.Save(state);
            }
        }

        void ReadControls()
        {
            movement = Vector2.zero; firing = false;
            if (!inventory)
            {
                movement = new Vector2((Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                    (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
                movement = Vector2.ClampMagnitude(movement, 1);
                if (Input.touchCount == 0)
                {
                    AimAtScreen(Input.mousePosition);
                    firing = Input.GetMouseButton(0) && Input.mousePosition.y < Screen.height - 100;
                }
            }
            float radius = Mathf.Min(Screen.width, Screen.height) * .12f;
            foreach (Touch touch in Input.touches)
            {
                bool ended = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                if (touch.phase == TouchPhase.Began && !inventory && touch.position.y < Screen.height * .35f)
                {
                    if (touch.position.x < Screen.width * .35f && leftFinger < 0) { leftFinger = touch.fingerId; leftOrigin = touch.position; }
                    else if (touch.position.x > Screen.width * .65f && rightFinger < 0) { rightFinger = touch.fingerId; rightOrigin = touch.position; }
                }
                if (touch.fingerId == leftFinger)
                {
                    leftThumb = Vector2.ClampMagnitude((touch.position - leftOrigin) / radius, 1);
                    if (!inventory && !ended) movement = leftThumb;
                    if (ended) { leftFinger = -1; leftThumb = Vector2.zero; }
                }
                else if (touch.fingerId == rightFinger)
                {
                    rightThumb = Vector2.ClampMagnitude((touch.position - rightOrigin) / radius, 1);
                    if (!inventory && !ended && rightThumb.sqrMagnitude > .08f) { aim = new Vector3(rightThumb.x,0,rightThumb.y); firing = true; }
                    if (ended) { rightFinger = -1; rightThumb = Vector2.zero; }
                }
                else if (!inventory && !ended && touch.position.y < Screen.height - 100) AimAtScreen(touch.position);
            }
        }

        void AimAtScreen(Vector2 screen)
        {
            Ray ray = view.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance)) { Vector3 direction = ray.GetPoint(distance) - robot.transform.position; direction.y = 0; if (direction.sqrMagnitude > .05f) aim = direction.normalized; }
        }

        void Fire()
        {
            foreach (var mount in state.Mounted)
            {
                Equipment gun = state.Owned[mount.Value];
                if (lastShot.TryGetValue(gun.Id, out float last) && Time.time - last < gun.Interval) continue;
                lastShot[gun.Id] = Time.time;
                float side = mount.Key == Mount.LeftArm || mount.Key == Mount.LeftShoulder ? -.8f : .8f;
                Vector3 origin = robot.transform.position + torso.right * side + aim.normalized * 1.1f + Vector3.up;
                Vector3 end = origin + aim.normalized * 20;
                if (Physics.Raycast(origin, aim.normalized, out RaycastHit hit, 20))
                {
                    end = hit.point;
                    Creature creature = creatures.FirstOrDefault(e => e.Model == hit.collider.gameObject);
                    if (creature != null && creature.Room == state.CurrentRoom)
                    {
                        creature.Health -= gun.Damage;
                        if (creature.Health <= 0) { creature.Model.GetComponent<Collider>().enabled = false; creature.Model.transform.localScale *= .6f; Destroy(creature.Model, 1.2f); }
                    }
                    if (crates.TryGetValue(state.CurrentRoom, out GameObject crate) && hit.collider.gameObject == crate) Collect(true);
                }
                LineRenderer line = shots[nextShot]; line.enabled = true;
                line.SetPosition(0, origin); line.SetPosition(1, end);
                shotExpiry[nextShot] = Time.time + .05f; nextShot = (nextShot + 1) % shots.Length;
            }
        }

        void TickCreatures()
        {
            foreach (Creature e in creatures)
            {
                if (e.Health <= 0 || e.Room != state.CurrentRoom) continue;
                Vector3 delta = robot.transform.position - e.Model.transform.position; delta.y = 0;
                e.Cooldown -= Time.deltaTime; e.Cycle += Time.deltaTime;
                if (delta.magnitude < 15)
                {
                    float speed = e.Kind == 0 ? 2.5f : e.Kind == 1 ? 1.4f : 1.8f;
                    if (e.Kind == 1 && e.Cycle % 4 < .7f) { if (e.Charge == Vector3.zero) e.Charge = delta.normalized; speed = 6; }
                    else e.Charge = Vector3.zero;
                    Vector3 step = (e.Charge == Vector3.zero ? delta.normalized : e.Charge) * speed * Time.deltaTime;
                    if (!NearOpening(layer, Index(e.Room), e.Model.transform.position + step - Centre(Index(e.Room)), .6f) &&
                        !Physics.Raycast(e.Model.transform.position, step.normalized, step.magnitude + .6f)) e.Model.transform.position += step;
                    if (delta.magnitude < 1.5f && e.Cooldown <= 0) { state.Damage(e.Kind == 1 ? 15 : 7); e.Cooldown = 1; }
                }
            }
        }

        void Collect(bool shot = false)
        {
            if (!crates.TryGetValue(state.CurrentRoom, out GameObject crate) || !crate.activeSelf) return;
            if (!shot && Vector3.Distance(robot.transform.position, crate.transform.position) > 2.5f) return;
            var item = Equipment.Variant(state.CurrentRoom + "/equipment", state.CurrentRoom,
                Index(state.CurrentRoom) == 1 ? WeaponClass.Heavy : WeaponClass.Light, 1234);
            if (state.Collect(item)) crate.SetActive(false);
        }

        void PlaceAtCurrentRoom()
        {
            motor.enabled = false;
            robot.transform.position = Centre(Index(state.CurrentRoom)) + new Vector3(-4,.1f,-4);
            Physics.SyncTransforms();
            motor.enabled = true;
        }

        void Die()
        {
            state.Defeat(); saves.Save(state); layer = state.CheckpointLayer < 1 ? 1 : state.CheckpointLayer;
            inventory = false; Time.timeScale = 1; lastShot.Clear(); BuildLayer(); PlaceAtCurrentRoom();
        }

        void ToggleInventory() { inventory = !inventory; Time.timeScale = inventory ? .2f : 1; saves.Save(state); }
        void LateUpdate() { if (view != null) view.transform.position = robot.transform.position + Vector3.up * 22; }
        void OnDestroy() { Time.timeScale = 1; if (abyss != null) Destroy(abyss); foreach (Mesh mesh in terrainMeshes) Destroy(mesh); foreach (Material material in palette.Values) Destroy(material); }

        void OnApplicationPause(bool paused)
        {
            if (!paused || state == null) return;
            state.StopUnfinishedEncounter(); saves.Save(state); layer = state.Rooms[state.CurrentRoom].Layer;
            inventory = false; Time.timeScale = 1; BuildLayer(); PlaceAtCurrentRoom();
        }

        void OnApplicationQuit() { saves.Save(state); }
        void OnApplicationFocus(bool focused) { if (!focused) saves.Save(state); }

        void OnGUI()
        {
            if (state == null) return;
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 16;
            GUI.Box(new Rect(8,8,Screen.width - 16,86), "Underpass — WHITEBOX TECHNICAL TRIAL");
            GUI.Label(new Rect(20,30,Screen.width - 40,30), "Health " + state.Health.ToString("0") + " | " + state.CurrentRoom + " | Checkpoint layer " + state.CheckpointLayer + " | WASD + aim + hold click · E collect · I inventory · F9 defeat");
            if (GUI.Button(new Rect(20,60,110,28), "Inventory")) ToggleInventory();
            if (GUI.Button(new Rect(140,60,90,28), "Map")) showMap = !showMap;
            if (state.IsStoragePoint && GUI.Button(new Rect(240,60,110,28), "Recover")) state.HealAtSafePlace();
            if (GUI.Button(new Rect(360,60,110,28), "Collect")) Collect();
            if (!inventory)
            {
                GUI.Box(new Rect(25,Screen.height - 165,140,140), "MOVE");
                GUI.Box(new Rect(Screen.width - 165,Screen.height - 165,140,140), "AIM / FIRE");
                if (leftFinger >= 0) GUI.Box(new Rect(leftOrigin.x + leftThumb.x * 55 - 12,Screen.height - leftOrigin.y - leftThumb.y * 55 - 12,24,24), "");
                if (rightFinger >= 0) GUI.Box(new Rect(rightOrigin.x + rightThumb.x * 55 - 12,Screen.height - rightOrigin.y - rightThumb.y * 55 - 12,24,24), "");
            }
            if (showMap && !inventory) ExplorationMap.Draw(state, layer, robot.transform.position);
            GUI.Label(new Rect(20,Screen.height - 25,Screen.width - 40,25), saves.Status);
            if (!inventory) return;
            GUI.Box(new Rect(15,105,Mathf.Min(760,Screen.width - 30),Screen.height - 120), "Inventory — world slowed to 20% (provisional)");
            int y = 140;
            foreach (var mount in state.Mounted.ToArray())
            {
                GUI.Label(new Rect(30,y,450,28), mount.Key + ": " + mount.Value);
                if (GUI.Button(new Rect(490,y,100,28), "Unequip")) state.Unequip(mount.Key);
                y += 32;
            }
            foreach (string id in state.Carried.ToArray())
            {
                GUI.Label(new Rect(30,y,250,28), id);
                foreach (Mount mount in Enum.GetValues(typeof(Mount)))
                {
                    GUI.enabled = Expedition.Fits(state.Owned[id], mount);
                    if (GUI.Button(new Rect(280 + (int)mount * 78,y,75,28), mount.ToString().Replace("Shoulder","Sh").Replace("Arm","Ar"))) state.Equip(id,mount);
                }
                GUI.enabled = state.IsStoragePoint;
                if (GUI.Button(new Rect(600,y,80,28), "Store")) state.Store(id);
                GUI.enabled = true; y += 32;
            }
            foreach (string id in state.Stored.ToArray())
            {
                GUI.Label(new Rect(30,y,400,28), "Stored: " + id); GUI.enabled = state.IsStoragePoint;
                if (GUI.Button(new Rect(490,y,100,28), "Withdraw")) state.Withdraw(id);
                GUI.enabled = true; y += 32;
            }
        }
    }
}
