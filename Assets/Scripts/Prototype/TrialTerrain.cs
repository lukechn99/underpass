using System.Collections.Generic;
using UnityEngine;
using static Underpass.Prototype.SewerLayout;

namespace Underpass.Prototype
{
    public sealed partial class TrialWorld
    {
        readonly List<Mesh> terrainMeshes = new List<Mesh>();

        void BuildRoomFloor(int room, Color concrete)
        {
            Vector3 centre = Centre(room);
            var openings = new List<Rect> { LethalGap(room) };
            if (HasDescent(layer, room)) openings.Add(DescentGap);
            var xs = new SortedSet<float> { -10, 10 };
            var zs = new SortedSet<float> { -10, 10 };
            foreach (Rect cut in openings) { xs.Add(cut.xMin); xs.Add(cut.xMax); zs.Add(cut.yMin); zs.Add(cut.yMax); }
            var x = new List<float>(xs); var z = new List<float>(zs);
            for (int a = 0; a < x.Count - 1; a++)
                for (int b = 0; b < z.Count - 1; b++)
                {
                    Vector3 p = new Vector3((x[a]+x[a+1])*.5f, 0, (z[b]+z[b+1])*.5f);
                    if (openings.Exists(cut => Inside(cut, p))) continue;
                    Shape(PrimitiveType.Cube, "Solid floor around authored opening", centre + p - Vector3.up*.3f,
                        new Vector3(x[a+1]-x[a], .6f, z[b+1]-z[b]), concrete, layerRoot.transform);
                }
            foreach (Rect cut in openings)
            {
                bool descent = HasDescent(layer, room) && cut == DescentGap;
                Vector3 middle = centre + new Vector3(cut.center.x, -4, cut.center.y);
                var bottom = Shape(PrimitiveType.Cube, descent ? "Absolute-black layer descent" : "Deep lethal shaft surface",
                    middle, new Vector3(cut.width, .1f, cut.height), new Color(.12f,.17f,.2f), layerRoot.transform, false);
                if (descent) bottom.GetComponent<Renderer>().sharedMaterial = abyss;
                for (int side = 0; side < 4; side++)
                {
                    bool alongX = side < 2; float sign = side % 2 == 0 ? -1 : 1;
                    Vector3 offset = alongX ? new Vector3(sign*cut.width*.5f,0,0) : new Vector3(0,0,sign*cut.height*.5f);
                    var liner = Shape(PrimitiveType.Cube, "Shaft inner surface", middle + Vector3.up*2 + offset,
                        alongX ? new Vector3(.08f,4,cut.height) : new Vector3(cut.width,4,.08f),
                        new Color(.16f,.2f,.23f), layerRoot.transform, false);
                    if (descent) liner.GetComponent<Renderer>().sharedMaterial = abyss;
                    Shape(PrimitiveType.Cube, descent ? "Blue descent rim" : "Amber lethal edge marking",
                        centre + new Vector3(cut.center.x,.015f,cut.center.y) + offset,
                        alongX ? new Vector3(.12f,.03f,cut.height) : new Vector3(cut.width,.03f,.12f),
                        descent ? new Color(.15f,.5f,.8f) : new Color(1,.6f,.1f), layerRoot.transform, false);
                }
            }
        }

        void BuildPipePassage(Vector3 centre, bool alongX)
        {
            var rust = new Color(.42f,.24f,.14f);
            Shape(PrimitiveType.Cube, "Pipe walkway", centre - Vector3.up*.3f,
                alongX ? new Vector3(3,.6f,4) : new Vector3(4,.6f,3), rust, layerRoot.transform);
            for (int side = -1; side <= 1; side += 2)
                Shape(PrimitiveType.Cube, "Pipe passage collision side", centre + Vector3.up*.7f +
                    (alongX ? Vector3.forward : Vector3.right)*side*2.2f,
                    alongX ? new Vector3(3,1.4f,.3f) : new Vector3(.3f,1.4f,3), rust, layerRoot.transform);
            // Lower half of a pipe, cut away above the player for the top-down camera.
            PipeShell(centre, alongX, 3, 2.7f, rust);
            for (int end = -1; end <= 1; end += 2)
                PipeShell(centre + (alongX ? Vector3.right : Vector3.forward)*end*1.35f,
                    alongX, .18f, 2.8f, new Color(.3f,.34f,.36f));
        }

        void PipeShell(Vector3 centre, bool alongX, float length, float radius, Color color)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            Vector3 Point(float angle, float end)
            {
                float side = Mathf.Cos(angle)*radius, y = 1.5f + Mathf.Sin(angle)*radius;
                return alongX ? new Vector3(end,y,side) : new Vector3(side,y,end);
            }
            for (int segment = 0; segment < 12; segment++)
            {
                float a = Mathf.PI + segment*Mathf.PI/12, b = a + Mathf.PI/12;
                var quad = new[] { Point(a,-length*.5f), Point(a,length*.5f), Point(b,length*.5f), Point(b,-length*.5f) };
                for (int face = 0; face < 2; face++)
                {
                    int n = vertices.Count;
                    for (int i = 0; i < 4; i++) vertices.Add(quad[face == 0 ? i : 3-i]);
                    triangles.AddRange(new[] {n,n+1,n+2,n,n+2,n+3});
                }
            }
            var mesh = new Mesh { name = "Cutaway sewer pipe" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            terrainMeshes.Add(mesh);
            var obj = new GameObject("Environment-native pipe passage"); obj.transform.SetParent(layerRoot.transform); obj.transform.position = centre;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh; obj.AddComponent<MeshRenderer>().sharedMaterial = Tint(color);
        }

        bool ResolveFloorFall()
        {
            Vector3 p = robot.transform.position;
            if (p.y > -1) return false;
            int room = (p.x > 11 ? 1 : 0) + (p.z > 11 ? 2 : 0);
            if (HasDescent(layer, room) && Inside(DescentGap, p-Centre(room)))
            {
                if (state.RoomInProgress) state.Retreat();
                layer = 2; state.Enter(Id(2,0)); saves.Save(state);
                inventory = false; Time.timeScale = 1; BuildLayer(); PlaceAtCurrentRoom();
                return true;
            }
            // All other missing-floor falls are defeat, including an unexpected out-of-bounds fall.
            Die(); return true;
        }
    }
}
