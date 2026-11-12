using UnityEngine;

namespace Underpass.Prototype
{
    // Authored whitebox geometry shared by the world and exploration map.
    public static class SewerLayout
    {
        public const float RoomWidth = 20;
        public const float Spacing = 22;
        public static readonly Vector3 PipeOffset = new Vector3(-7, 1, 0);
        public static readonly Vector3 CoverOffset = new Vector3(2, .6f, -2);
        public static readonly Vector3 SteamOffset = new Vector3(-7, .04f, 0);
        public static readonly Vector3 DescentOffset = new Vector3(4, .03f, 4);
        // Authored openings avoid recovery, cover and creature spawn positions.
        public static Rect LethalGap(int room) => room == 0 ? new Rect(5, 5, 3, 3) :
            room == 1 ? new Rect(4, -7, 4, 3) : room == 2 ? new Rect(4, 3, 4, 3) : new Rect(5, -7, 3, 4);
        public static Rect DescentGap => new Rect(2.5f, 2.5f, 3, 3);
        public static bool HasDescent(int layer, int room) => layer == 1 && room == 3;
        public static bool NearOpening(int layer, int room, Vector3 local, float margin)
        {
            Rect Expanded(Rect r) => new Rect(r.xMin-margin, r.yMin-margin, r.width+2*margin, r.height+2*margin);
            return Inside(Expanded(LethalGap(room)), local) || (HasDescent(layer,room) && Inside(Expanded(DescentGap),local));
        }
        public static void Validate()
        {
            for (int floor = 1; floor <= 2; floor++)
                for (int room = 0; room < 4; room++)
                {
                    var cuts = HasDescent(floor,room) ? new[] { LethalGap(room), DescentGap } : new[] { LethalGap(room) };
                    foreach (Rect r in cuts)
                        if (r.width < 2 || r.height < 2 || r.xMin <= -9 || r.xMax >= 9 || r.yMin <= -9 || r.yMax >= 9)
                            throw new System.InvalidOperationException("Floor opening must leave navigable room edges.");
                    if (cuts.Length == 2 && cuts[0].Overlaps(cuts[1]))
                        throw new System.InvalidOperationException("Lethal and descent openings overlap.");
                    var safe = new[] { new Vector3(-4,0,-4), new Vector3(-3,0,2), CoverOffset, PipeOffset };
                    foreach (Vector3 p in safe)
                        if (NearOpening(floor,room,p,.8f))
                            throw new System.InvalidOperationException("Opening overlaps recovery or equipment placement.");
                    int count = floor == 1 && room == 3 ? 1 : 3;
                    for (int actor = 0; actor < count; actor++)
                        if (NearOpening(floor,room,new Vector3(actor*2-2,0,5),.8f))
                            throw new System.InvalidOperationException("Opening overlaps actor placement.");
                }
        }
        public static bool Inside(Rect area, Vector3 local) => area.Contains(new Vector2(local.x, local.z));
        public static string Id(int floor, int index) => "L" + floor + "-" + "ABCD"[index];
        public static int Index(string id) => "ABCD".IndexOf(id[id.Length - 1]);
        public static Vector3 Centre(int index) => new Vector3((index % 2) * Spacing, 0, (index / 2) * Spacing);
    }
}
