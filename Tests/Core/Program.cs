using System;
using System.Collections.Generic;
using System.Linq;
using Underpass.Core;

static class Program
{
    static int failures, total;
    static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    static Expedition New(int capacity = 8)
    {
        var rooms = new List<Room>();
        for (int layer = 1; layer <= 6; layer++)
        {
            rooms.Add(new Room("L" + layer + "A", layer, safePlace: layer == 3));
            rooms.Add(new Room("L" + layer + "Boss", layer, boss: true));
        }
        return new Expedition(rooms, "L1A", capacity);
    }

    static void Clear(Expedition state, string room)
    {
        Expect(state.Enter(room), "Could not enter " + room);
        Expect(state.Conquer(0), "Could not conquer " + room);
    }

    static Equipment Find(string room, string suffix = "gun", WeaponClass kind = WeaponClass.Light)
        => Equipment.Variant(room + "/" + suffix, room, kind, 1234);

    static void Run(string name, Action test)
    {
        total++;
        try { test(); Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
    }

    static int Main()
    {
        Run("two identical starter arm guns; shoulders empty", () => {
            var s = New();
            Expect(s.Mounted.Count == 2 && s.Mounted.ContainsKey(Mount.LeftArm) && s.Mounted.ContainsKey(Mount.RightArm), "Starter mount layout");
            var a = s.Owned[s.Mounted[Mount.LeftArm]]; var b = s.Owned[s.Mounted[Mount.RightArm]];
            Expect(a.Damage == b.Damage && a.Interval == b.Interval, "Starter weapons differ");
        });
        Run("heavy weapons rejected by arms; light weapons fit shoulders", () => {
            var s = New(); s.Enter("L1A");
            var heavy = Find("L1A", "heavy", WeaponClass.Heavy); var light = Find("L1A", "light");
            Expect(s.Collect(heavy) && s.Collect(light), "Collection failed");
            Expect(!s.Equip(heavy.Id, Mount.LeftArm) && s.Carried.Contains(heavy.Id), "Failed equip mutated inventory");
            Expect(s.Equip(heavy.Id, Mount.LeftShoulder), "Heavy shoulder fit");
            Expect(s.Equip(light.Id, Mount.RightShoulder), "Light shoulder fit");
        });
        Run("entering a deeper layer does not activate its checkpoint", () => {
            var s = New(); Clear(s, "L2Boss"); s.Enter("L6Boss");
            Expect(!s.ActivateBossCheckpoint() && s.CheckpointLayer == 2, "Unbeaten boss activated checkpoint");
            Expect(!s.Conquer(1), "Living enemies allowed conquest");
            Expect(s.Conquer(0) && s.CheckpointLayer == 6, "Defeating boss did not activate checkpoint");
        });
        Run("layer-2 checkpoint protects layer 1/2 finds; death resets every deeper conquered room", () => {
            var s = New();
            s.Enter("L1A"); var old = Find("L1A"); s.Collect(old); s.Conquer(0);
            Clear(s, "L2Boss");
            s.Enter("L3A"); var recent = Find("L3A", kind: WeaponClass.Heavy); s.Collect(recent); s.Equip(recent.Id, Mount.LeftShoulder); s.Conquer(0);
            Clear(s, "L4A"); Clear(s, "L5A"); s.Enter("L6A"); var unsecured = Find("L6A"); s.Collect(unsecured); s.Damage(70);
            s.Defeat();
            Expect(s.CurrentRoom == "L2Boss" && s.Health == 100, "Wrong return state");
            Expect(s.Rooms["L1A"].Conquered && s.Rooms["L2Boss"].Conquered && s.Owned.ContainsKey(old.Id), "Protected progress lost");
            Expect(s.Rooms.Values.Where(r => r.Layer > 2).All(r => !r.Conquered), "Deeper rooms remain cleared");
            Expect(!s.Owned.ContainsKey(recent.Id) && !s.Owned.ContainsKey(unsecured.Id), "Unprotected equipment survived");
        });
        Run("shared storage protects a deep find and suppresses duplicate re-awards", () => {
            var s = New(); Clear(s, "L2Boss"); s.Enter("L3A");
            var gear = Find("L3A"); s.Collect(gear); s.Conquer(0); Expect(s.Store(gear.Id), "Safe-place storage failed");
            s.Enter("L6A"); s.Defeat();
            Expect(s.Stored.Contains(gear.Id) && s.Owned.ContainsKey(gear.Id), "Banked equipment lost");
            s.Enter("L3A"); Expect(!s.Collect(Find("L3A")), "Stored gear duplicated"); s.Conquer(0);
            Expect(s.Withdraw(gear.Id), "Shared storage unavailable at another safe place");
        });
        Run("a forfeited find can be recovered with the same variant", () => {
            var s = New(); s.Enter("L1A"); var gear = Find("L1A"); s.Collect(gear); s.Equip(gear.Id, Mount.LeftShoulder);
            s.Damage(25); s.Retreat();
            Expect(s.Health == 75 && !s.Owned.ContainsKey(gear.Id) && !s.Rooms["L1A"].Conquered, "Retreat state wrong");
            s.Enter("L1A"); var retry = Find("L1A");
            Expect(s.Collect(retry) && retry.Damage == gear.Damage && retry.Interval == gear.Interval, "Retry rerolled or lost find");
        });
        Run("stopping restores the room-entry health, inventory and mounts", () => {
            var s = New(); s.Damage(20); s.Enter("L1A"); var gear = Find("L1A");
            s.Collect(gear); s.Equip(gear.Id, Mount.LeftArm); s.Discard("starter-right"); s.Damage(60);
            s.StopUnfinishedEncounter();
            Expect(s.Health == 80 && s.Mounted[Mount.LeftArm] == "starter-left" && s.Mounted[Mount.RightArm] == "starter-right", "Entry state not restored");
            Expect(!s.Owned.ContainsKey(gear.Id) && s.Carried.Count == 0, "Unfinished rewards persisted");
        });
        Run("discarded starter set is restored when defeat removes all usable carried weapons", () => {
            var s = New(); s.Discard("starter-left"); s.Discard("starter-right");
            s.Enter("L1A"); var gear = Find("L1A"); s.Collect(gear); s.Equip(gear.Id, Mount.LeftArm); s.Defeat();
            Expect(s.Owned.Count == 2 && s.Mounted.Count == 2 && s.Owned.Values.All(i => i.Starter), "Recovery fallback failed");
        });
        Run("conquering a deeper boss protects that layer's previously gathered equipment", () => {
            var s = New(); s.Enter("L3A"); var gear = Find("L3A"); s.Collect(gear); s.Conquer(0);
            Clear(s, "L3Boss"); s.Enter("L6A"); s.Defeat();
            Expect(s.Rooms["L3A"].Conquered && s.Owned.ContainsKey(gear.Id) && s.CheckpointLayer == 3, "Checkpoint failed to secure layer");
        });
        Run("full reserve inventory refuses a pickup without losing owned equipment", () => {
            var s = New(1); s.Enter("L1A"); var a = Find("L1A", "a"); var b = Find("L1A", "b");
            Expect(s.Collect(a) && !s.Collect(b) && s.Carried.Single() == a.Id, "Capacity rejection mutated inventory");
            Expect(!s.Store(a.Id), "Stored unsecured loot from an active encounter");
        });
        Run("generated variants stay bounded and deterministic", () => {
            for (uint i = 0; i < 500; i++)
            {
                var a = Equipment.Variant("room/weapon", "room", WeaponClass.Light, i);
                var b = Equipment.Variant("room/weapon", "room", WeaponClass.Light, i);
                Expect(a.Damage >= 8 && a.Damage <= 12.01 && a.Interval > 0, "Unbounded roll");
                Expect(a.Damage == b.Damage && a.Interval == b.Interval, "Non-deterministic roll");
            }
        });
        Run("save JSON roundtrip preserves checkpoint, variants, storage and mounts", () => {
            var s = New(); Clear(s, "L2Boss"); s.Enter("L3A");
            var banked = Find("L3A"); var mounted = Find("L3A", "heavy", WeaponClass.Heavy);
            s.Collect(banked); s.Collect(mounted); s.Equip(mounted.Id, Mount.LeftShoulder);
            s.Conquer(0); s.Store(banked.Id); s.ExpandStorage(4); s.Damage(19);
            var options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
            string json = System.Text.Json.JsonSerializer.Serialize(s.CaptureSave(), options);
            var data = System.Text.Json.JsonSerializer.Deserialize<ExpeditionSave>(json, options);
            var r = Expedition.RestoreSave(New().Rooms.Values, "L1A", data);
            Expect(r.CurrentRoom == "L3A" && r.CheckpointLayer == 2 && r.Health == 81, "Return state changed");
            Expect(r.StorageCapacity == 12 && r.Stored.Contains(banked.Id) && r.Mounted[Mount.LeftShoulder] == mounted.Id, "Inventory changed");
            Expect(r.Owned[mounted.Id].Damage == mounted.Damage, "Weapon roll changed");
            r.Enter("L6A"); r.Defeat();
            Expect(r.Stored.Contains(banked.Id) && !r.Owned.ContainsKey(mounted.Id), "Reload changed death rules");
        });
        Run("saving an unfinished fight restores entry state without changing live play", () => {
            var s = New(); Clear(s, "L1A"); s.Damage(12); s.Enter("L3A");
            var item = Find("L3A"); s.Collect(item); s.Equip(item.Id, Mount.LeftArm); s.Damage(50);
            var r = Expedition.RestoreSave(New().Rooms.Values, "L1A", s.CaptureSave());
            Expect(s.Health == 38 && s.RoomInProgress && s.Owned.ContainsKey(item.Id), "Capture interrupted live fight");
            Expect(r.Health == 88 && r.CurrentRoom == "L1A" && !r.RoomInProgress, "Entry return state wrong");
            Expect(!r.Owned.ContainsKey(item.Id) && r.Mounted[Mount.LeftArm] == "starter-left", "Unsecured gains survived reload");
            Expect(!r.Rooms["L3A"].Conquered, "Unfinished room cleared on reload");
        });
        Run("corrupt or unsupported saves fail without mutating room definitions", () => {
            var s = New(); Clear(s, "L2Boss");
            Action<Action<ExpeditionSave>> rejects = mutate => {
                var data = s.CaptureSave(); mutate(data);
                var definitions = New().Rooms.Values.ToArray();
                bool rejected = false;
                try { Expedition.RestoreSave(definitions, "L1A", data); }
                catch (ArgumentException) { rejected = true; }
                Expect(rejected, "Invalid save accepted");
                Expect(definitions.All(r => !r.Conquered), "Failed restore mutated input definitions");
            };
            rejects(d => d.Version = 99);
            rejects(d => d.World = "other-world");
            rejects(d => d.Health = float.NaN);
            rejects(d => d.CurrentRoom = "unknown");
            rejects(d => d.CheckpointLayer = 5);
            rejects(d => d.Carried = new[] { "starter-left" });
            rejects(d => d.Mounts[0].Mount = 99);
            rejects(d => d.Equipment[0].Class = 99);
            rejects(d => d.Equipment[0].Damage = -1);
            rejects(d => d.Conquered = null);
            rejects(d => d.StorageCapacity = -1);
        });
        Run("starter source strings tolerate Unity JSON null-to-empty conversion", () => {
            var s = New(); var data = s.CaptureSave();
            foreach (var item in data.Equipment) item.SourceRoom = "";
            var r = Expedition.RestoreSave(New().Rooms.Values, "L1A", data);
            Expect(r.Mounted.Count == 2, "Unity starter strings rejected");
        });
        Console.WriteLine("Core scenarios: " + (total - failures) + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }
}
