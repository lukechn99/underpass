using Underpass.Core;
using UnityEngine;
using static Underpass.Prototype.SewerLayout;

namespace Underpass.Prototype
{
    // Replaceable presentation; discoveries and conquest remain engine-independent.
    public static class ExplorationMap
    {
        static GUIStyle legend;
        public static void Draw(Expedition state, int layer, Vector3 position)
        {
            if (legend == null) legend = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            float size = Mathf.Min(360, Screen.width - 48, Screen.height - 210);
            if (size < 100) return;
            var panel = new Rect((Screen.width - size) * .5f - 12, 105, size + 24, size + 92);
            GUI.Box(panel, "Explored sewer · layer " + layer);
            var map = new Rect(panel.x + 12, panel.y + 32, size, size);
            float scale = size / (Spacing + RoomWidth + 2);
            Vector2 Project(Vector3 p) => new Vector2(map.x + (p.x + 11) * scale,
                map.y + (33 - p.z) * scale);
            Rect Footprint(Vector3 p, float width, float depth)
            {
                Vector2 point = Project(p);
                return new Rect(point.x - width * scale * .5f, point.y - depth * scale * .5f, width * scale, depth * scale);
            }
            void Fill(Rect rect, Color color)
            {
                Color previous = GUI.color; GUI.color = color;
                GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous;
            }
            bool Known(int i) => state.Rooms[Id(layer, i)].Discovered;
            for (int i = 0; i < 4; i++)
            {
                if (!Known(i)) continue;
                // Only connect explored endpoints. Unknown rooms and their contents stay hidden.
                if (i % 2 == 0 && Known(i + 1))
                    Fill(Footprint(Centre(i) + Vector3.right * 11, 3, 4), new Color(.25f,.3f,.35f));
                if (i < 2 && Known(i + 2))
                    Fill(Footprint(Centre(i) + Vector3.forward * 11, 4, 3), new Color(.25f,.3f,.35f));
            }
            for (int i = 0; i < 4; i++)
            {
                if (!Known(i)) continue;
                Room room = state.Rooms[Id(layer,i)]; Vector3 c = Centre(i);
                Fill(Footprint(c, RoomWidth, RoomWidth), room.Conquered ? new Color(.12f,.34f,.3f) : new Color(.3f,.24f,.22f));
                Fill(Footprint(c + CoverOffset, 2, 2), new Color(.65f,.7f,.74f));
                Fill(Footprint(c + PipeOffset, .8f, .8f), new Color(.85f,.45f,.16f));
                if (i == 1 || i == 2) Fill(Footprint(c + SteamOffset, 4, 4), new Color(.8f,.35f,.1f,.7f));
                Vector2 title = Project(c + new Vector3(-8,0,8));
                GUI.Label(new Rect(title.x,title.y,RoomWidth * scale,24), "ABCD"[i] + (room.Conquered ? " · clear" : " · hostile"));
                if (i == 3 && layer == 1)
                {
                    Vector2 hole = Project(c + DescentOffset);
                    GUI.Label(new Rect(hole.x-12,hole.y-12,60,24), "↓ 2");
                }
                if (room.SafePlace || (room.Boss && room.Conquered))
                {
                    Vector2 safe = Project(c);
                    GUI.Label(new Rect(safe.x-28,safe.y+8,100,24), room.Id == state.CheckpointRoom && state.CheckpointLayer > 0 ? "CP · safe" : "safe");
                }
            }
            Fill(Footprint(position, 1.3f, 1.3f), new Color(.25f,.85f,1));
            GUI.Label(new Rect(panel.x+12,map.yMax+5,size,24), "Blue: you · green: cleared · brown: hostile", legend);
            GUI.Label(new Rect(panel.x+12,map.yMax+29,size,24), "Visited rooms only · exploration survives defeat", legend);
        }
    }
}
