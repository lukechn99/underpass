using System;
using System.Collections.Generic;
using System.Linq;

namespace Underpass.Core
{
    public enum Mount { LeftArm, RightArm, LeftShoulder, RightShoulder }
    public enum WeaponClass { Light, Heavy }

    public sealed class Room
    {
        public string Id { get; }
        public int Layer { get; }
        public bool Boss { get; }
        public bool SafePlace { get; }
        public bool Conquered { get; internal set; }
        public Room(string id, int layer, bool boss = false, bool safePlace = false)
        {
            if (string.IsNullOrWhiteSpace(id) || layer < 1) throw new ArgumentException("Invalid room.");
            Id = id; Layer = layer; Boss = boss; SafePlace = safePlace;
        }
    }

    public sealed class Equipment
    {
        public string Id { get; }
        public string SourceRoom { get; }
        public WeaponClass Class { get; }
        public bool Starter { get; }
        public float Damage { get; }
        public float Interval { get; }

        public Equipment(string id, string sourceRoom, WeaponClass weaponClass, bool starter = false,
            float damage = 10, float interval = 0.25f)
        {
            if (string.IsNullOrWhiteSpace(id) || damage <= 0 || interval <= 0 ||
                float.IsNaN(damage) || float.IsNaN(interval) || float.IsInfinity(damage) || float.IsInfinity(interval))
                throw new ArgumentException("Invalid equipment.");
            Id = id; SourceRoom = sourceRoom; Class = weaponClass; Starter = starter;
            Damage = damage; Interval = interval;
        }

        // Stable across processes, runtimes and retries. A temporary equal-DPS baseline,
        // not proof of equal effectiveness: reload, accuracy and behaviour still need playtests.
        public static Equipment Variant(string id, string room, WeaponClass kind, uint seed)
        {
            uint hash = 2166136261u ^ seed;
            foreach (char c in id) hash = unchecked((hash ^ c) * 16777619u);
            float trade = 0.8f + (hash % 401) / 1000f;
            float damage = kind == WeaponClass.Heavy ? 30 : 10;
            float interval = kind == WeaponClass.Heavy ? 0.8f : 0.25f;
            return new Equipment(id, room, kind, false, damage * trade, interval * trade);
        }
    }

    public sealed class Expedition
    {
        readonly Dictionary<string, Room> rooms;
        readonly Dictionary<string, Equipment> owned = new Dictionary<string, Equipment>();
        readonly List<string> carried = new List<string>();
        readonly HashSet<string> stored = new HashSet<string>();
        readonly Dictionary<Mount, string> mounted = new Dictionary<Mount, string>();
        Entry entry;

        sealed class Entry
        {
            public float Health;
            public Dictionary<string, Equipment> Owned;
            public List<string> Carried;
            public Dictionary<Mount, string> Mounted;
        }

        public IReadOnlyDictionary<string, Room> Rooms => rooms;
        public IReadOnlyDictionary<string, Equipment> Owned => owned;
        public IReadOnlyList<string> Carried => carried;
        public IReadOnlyCollection<string> Stored => stored;
        public IReadOnlyDictionary<Mount, string> Mounted => mounted;
        public int CarryCapacity { get; }
        public int StorageCapacity { get; private set; }
        public float Health { get; private set; } = 100;
        public float MaxHealth => 100;
        public int CheckpointLayer { get; private set; }
        public string CheckpointRoom { get; private set; }
        public string CurrentRoom { get; private set; }
        public string LastSafeRoom { get; private set; }
        public bool RoomInProgress => entry != null;

        public Expedition(IEnumerable<Room> definitions, string startRoom, int carryCapacity = 8, int storageCapacity = 8)
        {
            rooms = definitions.ToDictionary(r => r.Id);
            if (!rooms.ContainsKey(startRoom) || carryCapacity < 1 || storageCapacity < 1)
                throw new ArgumentException("Invalid expedition setup.");
            CarryCapacity = carryCapacity; StorageCapacity = storageCapacity;
            CurrentRoom = LastSafeRoom = CheckpointRoom = startRoom;
            // The starting location is a provisional fallback until the first boss checkpoint.
            CheckpointLayer = 0;
            RestoreStarters();
        }

        public bool Enter(string id)
        {
            if (!rooms.ContainsKey(id) || RoomInProgress) return false;
            CurrentRoom = id;
            if (rooms[id].Conquered) { LastSafeRoom = id; return true; }
            entry = new Entry {
                Health = Health,
                Owned = new Dictionary<string, Equipment>(owned),
                Carried = new List<string>(carried),
                Mounted = new Dictionary<Mount, string>(mounted)
            };
            return true;
        }

        public bool Collect(Equipment item)
        {
            // Identity is per discovery, not per weapon type. Stored copies suppress re-awards.
            if (owned.ContainsKey(item.Id) || carried.Count >= CarryCapacity ||
                !rooms.ContainsKey(item.SourceRoom) || item.SourceRoom != CurrentRoom) return false;
            owned.Add(item.Id, item); carried.Add(item.Id); return true;
        }

        public static bool Fits(Equipment item, Mount mount) => item.Class == WeaponClass.Light ||
            mount == Mount.LeftShoulder || mount == Mount.RightShoulder;

        public bool Equip(string id, Mount mount)
        {
            if (!carried.Contains(id) || !Fits(owned[id], mount)) return false;
            carried.Remove(id);
            if (mounted.TryGetValue(mount, out string old)) carried.Add(old);
            mounted[mount] = id; return true;
        }

        public bool Unequip(Mount mount)
        {
            if (carried.Count >= CarryCapacity || !mounted.TryGetValue(mount, out string id)) return false;
            mounted.Remove(mount); carried.Add(id); return true;
        }

        public void Damage(float amount)
        {
            if (amount < 0 || float.IsNaN(amount) || float.IsInfinity(amount))
                throw new ArgumentException("Invalid damage.");
            Health = Math.Max(0, Health - amount);
        }

        public void HealAtSafePlace() { if (RoomInProgress) throw new InvalidOperationException("Not safe."); Health = MaxHealth; }

        public bool Conquer(int hostilesRemaining)
        {
            if (!RoomInProgress || hostilesRemaining != 0) return false;
            rooms[CurrentRoom].Conquered = true; LastSafeRoom = CurrentRoom; entry = null;
            if (rooms[CurrentRoom].Boss) ActivateBossCheckpoint();
            return true;
        }

        public bool ActivateBossCheckpoint()
        {
            Room room = rooms[CurrentRoom];
            if (!room.Boss || !room.Conquered) return false;
            CheckpointLayer = room.Layer; CheckpointRoom = room.Id; return true;
        }

        public bool Store(string id)
        {
            // Prototype safe places are the start and conquered boss rooms.
            if (!IsStoragePoint || !carried.Contains(id) || stored.Count >= StorageCapacity) return false;
            carried.Remove(id); stored.Add(id); return true;
        }

        public bool Withdraw(string id)
        {
            if (!IsStoragePoint || !stored.Contains(id) || carried.Count >= CarryCapacity) return false;
            stored.Remove(id); carried.Add(id); return true;
        }

        public bool IsStoragePoint => !RoomInProgress &&
            (CurrentRoom == CheckpointRoom ||
             ((rooms[CurrentRoom].Boss || rooms[CurrentRoom].SafePlace) && rooms[CurrentRoom].Conquered));

        public void ExpandStorage(int slots)
        {
            if (slots < 1) throw new ArgumentException("Invalid capacity upgrade.");
            StorageCapacity = checked(StorageCapacity + slots);
        }

        public bool Discard(string id)
        {
            if (!owned.ContainsKey(id) || stored.Contains(id)) return false;
            Remove(id); return true;
        }

        public void Retreat()
        {
            if (!RoomInProgress) return;
            foreach (string id in owned.Keys.Where(id => !entry.Owned.ContainsKey(id) && !stored.Contains(id)).ToArray()) Remove(id);
            entry = null; CurrentRoom = LastSafeRoom;
        }

        public void StopUnfinishedEncounter()
        {
            if (entry == null) { CurrentRoom = LastSafeRoom; return; }
            Health = entry.Health;
            owned.Clear(); foreach (var pair in entry.Owned) owned.Add(pair.Key, pair.Value);
            carried.Clear(); carried.AddRange(entry.Carried);
            mounted.Clear(); foreach (var pair in entry.Mounted) mounted.Add(pair.Key, pair.Value);
            entry = null; CurrentRoom = LastSafeRoom;
        }

        public void Defeat()
        {
            foreach (Room room in rooms.Values.Where(r => r.Layer > CheckpointLayer)) room.Conquered = false;
            foreach (string id in owned.Keys.Where(id => !owned[id].Starter && !stored.Contains(id) &&
                !rooms[owned[id].SourceRoom].Conquered).ToArray()) Remove(id);
            entry = null;
            CurrentRoom = LastSafeRoom = CheckpointRoom;
            Health = MaxHealth;
            if (!mounted.Any() && !carried.Any()) RestoreStarters();
        }

        void RestoreStarters()
        {
            AddStarter("starter-left", Mount.LeftArm);
            AddStarter("starter-right", Mount.RightArm);
        }

        void AddStarter(string id, Mount mount)
        {
            if (owned.ContainsKey(id)) return;
            owned.Add(id, new Equipment(id, null, WeaponClass.Light, true)); mounted[mount] = id;
        }

        void Remove(string id)
        {
            foreach (Mount mount in mounted.Where(p => p.Value == id).Select(p => p.Key).ToArray()) mounted.Remove(mount);
            carried.Remove(id); owned.Remove(id);
        }
    }
}
