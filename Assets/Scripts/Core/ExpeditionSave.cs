using System;
using System.Collections.Generic;
using System.Linq;

namespace Underpass.Core
{
    // Plain data: Unity, storage, and JSON implementations stay outside the rules.
    [Serializable]
    public sealed class ExpeditionSave
    {
        public int Version;
        public string World;
        public string CurrentRoom, CheckpointRoom;
        public int CheckpointLayer, CarryCapacity, StorageCapacity;
        public float Health;
        public string[] Conquered, Carried, Stored;
        public SavedEquipment[] Equipment;
        public SavedMount[] Mounts;
    }

    [Serializable]
    public sealed class SavedEquipment
    {
        public string Id, SourceRoom;
        public int Class;
        public bool Starter;
        public float Damage, Interval;
    }

    [Serializable]
    public sealed class SavedMount
    {
        public int Mount;
        public string Id;
    }

    public sealed partial class Expedition
    {
        public const string SaveWorld = "sewer-whitebox-v1";

        // Saving an active fight records its entry state without interrupting play.
        // Closing the tab need not deliver a reliable shutdown callback.
        public ExpeditionSave CaptureSave()
        {
            var items = entry == null ? owned : entry.Owned;
            var reserve = entry == null ? carried : entry.Carried;
            var mounts = entry == null ? mounted : entry.Mounted;
            return new ExpeditionSave {
                Version = 1, World = SaveWorld,
                CurrentRoom = LastSafeRoom, CheckpointRoom = CheckpointRoom,
                CheckpointLayer = CheckpointLayer,
                CarryCapacity = CarryCapacity, StorageCapacity = StorageCapacity,
                Health = entry == null ? Health : entry.Health,
                Conquered = rooms.Values.Where(r => r.Conquered).Select(r => r.Id).OrderBy(id => id).ToArray(),
                Carried = reserve.ToArray(), Stored = stored.OrderBy(id => id).ToArray(),
                Equipment = items.Values.OrderBy(i => i.Id).Select(i => new SavedEquipment {
                    Id = i.Id, SourceRoom = i.SourceRoom, Class = (int)i.Class,
                    Starter = i.Starter, Damage = i.Damage, Interval = i.Interval
                }).ToArray(),
                Mounts = mounts.OrderBy(p => p.Key).Select(p => new SavedMount { Mount = (int)p.Key, Id = p.Value }).ToArray()
            };
        }

        // Construct a new instance, then validate completely before exposing it.
        // Invalid saves cannot partially mutate the running adventure or room definitions.
        public static Expedition RestoreSave(IEnumerable<Room> definitions, string startRoom, ExpeditionSave save)
        {
            if (save == null || save.Version != 1 || save.World != SaveWorld ||
                save.CarryCapacity < 1 || save.CarryCapacity > 100 || save.StorageCapacity < 1 || save.StorageCapacity > 10000 ||
                float.IsNaN(save.Health) || float.IsInfinity(save.Health) || save.Health < 0 || save.Health > 100 ||
                save.Conquered == null || save.Carried == null || save.Stored == null || save.Equipment == null || save.Mounts == null)
                throw new ArgumentException("Unsupported or invalid expedition save.");
            var s = new Expedition(definitions.Select(r => new Room(r.Id, r.Layer, r.Boss, r.SafePlace)),
                startRoom, save.CarryCapacity, save.StorageCapacity);
            if (save.CurrentRoom == null || save.CheckpointRoom == null ||
                !s.rooms.ContainsKey(save.CurrentRoom) || !s.rooms.ContainsKey(save.CheckpointRoom) ||
                save.Conquered.Distinct().Count() != save.Conquered.Length)
                throw new ArgumentException("Invalid saved room references.");
            foreach (string id in save.Conquered)
            {
                if (id == null || !s.rooms.ContainsKey(id)) throw new ArgumentException("Unknown conquered room.");
                s.rooms[id].Conquered = true;
            }
            var checkpoint = s.rooms[save.CheckpointRoom];
            if (save.CheckpointLayer == 0 ? save.CheckpointRoom != startRoom :
                save.CheckpointLayer < 1 || checkpoint.Layer != save.CheckpointLayer || !checkpoint.Boss || !checkpoint.Conquered)
                throw new ArgumentException("Invalid saved checkpoint.");
            if (!s.rooms[save.CurrentRoom].Conquered && save.CurrentRoom != save.CheckpointRoom)
                throw new ArgumentException("Saved return room is not safe.");
            s.owned.Clear(); s.carried.Clear(); s.mounted.Clear();
            foreach (var item in save.Equipment)
            {
                if (item == null || !Enum.IsDefined(typeof(WeaponClass), item.Class) ||
                    (item.Starter ? (item.Id != "starter-left" && item.Id != "starter-right") || !string.IsNullOrEmpty(item.SourceRoom) || item.Class != 0 :
                        item.SourceRoom == null || !s.rooms.ContainsKey(item.SourceRoom)))
                    throw new ArgumentException("Invalid saved equipment.");
                var equipment = new Equipment(item.Id, item.SourceRoom, (WeaponClass)item.Class, item.Starter, item.Damage, item.Interval);
                if (s.owned.ContainsKey(item.Id)) throw new ArgumentException("Duplicate saved equipment.");
                s.owned.Add(item.Id, equipment);
            }
            var placed = new HashSet<string>();
            foreach (string id in save.Carried.Concat(save.Stored))
                if (id == null || !s.owned.ContainsKey(id) || !placed.Add(id)) throw new ArgumentException("Invalid inventory placement.");
            if (save.Carried.Length > s.CarryCapacity || save.Stored.Length > s.StorageCapacity)
                throw new ArgumentException("Saved inventory exceeds capacity.");
            s.carried.AddRange(save.Carried); s.stored.UnionWith(save.Stored);
            foreach (var mount in save.Mounts)
            {
                if (mount == null || !Enum.IsDefined(typeof(Mount), mount.Mount) || mount.Id == null ||
                    !s.owned.ContainsKey(mount.Id) || !placed.Add(mount.Id) || s.mounted.ContainsKey((Mount)mount.Mount) ||
                    !Fits(s.owned[mount.Id], (Mount)mount.Mount)) throw new ArgumentException("Invalid saved mount.");
                s.mounted.Add((Mount)mount.Mount, mount.Id);
            }
            if (placed.Count != s.owned.Count) throw new ArgumentException("Unplaced saved equipment.");
            s.Health = save.Health; s.CheckpointLayer = save.CheckpointLayer;
            s.CheckpointRoom = save.CheckpointRoom;
            s.CurrentRoom = s.LastSafeRoom = save.CurrentRoom;
            return s;
        }
    }
}
