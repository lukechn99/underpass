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
        public static string Id(int floor, int index) => "L" + floor + "-" + "ABCD"[index];
        public static int Index(string id) => "ABCD".IndexOf(id[id.Length - 1]);
        public static Vector3 Centre(int index) => new Vector3((index % 2) * Spacing, 0, (index / 2) * Spacing);
    }
}
