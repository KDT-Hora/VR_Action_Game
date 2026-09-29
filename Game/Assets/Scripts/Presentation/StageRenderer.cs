using System.Collections.Generic;
using UnityEngine;
using VrAction.Core.Model;

namespace VrAction.Game.Presentation
{
    /// <summary>Builds the miniature stage (one merged mesh) and static markers from a SpatialData + Stage.</summary>
    public static class StageRenderer
    {
        static Material _mat;
        static Material Mat => _mat != null ? _mat : (_mat = new Material(Shader.Find("Sprites/Default")));

        public static GameObject Build(Transform stageRoot, SpatialData s, Stage stage)
        {
            var root = new GameObject("StageVisual");
            root.transform.SetParent(stageRoot, false);
            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(root.transform, false);
            terrain.AddComponent<MeshFilter>().sharedMesh = BuildTerrainMesh(s);
            terrain.AddComponent<MeshRenderer>().sharedMaterial = Mat;

            Marker(root.transform, s, stage.Goal, new Color(1f, 1f, 1f), 0.20f, 0.6f, "Goal");
            return root;
        }

        public static Vector3 CellCenter(SpatialData s, Cell c, float y = 0f)
            => new Vector3((s.OriginXMm + c.X * s.CellSizeMm + s.CellSizeMm / 2) / 1000f, y,
                           (s.OriginZMm + c.Z * s.CellSizeMm + s.CellSizeMm / 2) / 1000f);

        public static GameObject Marker(Transform parent, SpatialData s, Cell c, Color color, float size, float heightScale, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            float h = s.HeightAt(c.X, c.Z) / 1000f;
            go.transform.localScale = new Vector3(0.03f, 0.05f * heightScale * 2f, 0.03f) * (size / 0.2f);
            go.transform.localPosition = CellCenter(s, c, h + go.transform.localScale.y);
            go.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default")) { color = color };
            return go;
        }

        static Color ColorFor(CellKind k, bool hazard)
        {
            switch (k)
            {
                case CellKind.Ground: return hazard ? new Color(0.85f, 0.75f, 0.45f) : new Color(0.45f, 0.75f, 0.45f);
                case CellKind.Platform: return hazard ? new Color(0.75f, 0.6f, 0.4f) : new Color(0.6f, 0.5f, 0.35f);
                case CellKind.Wall: return new Color(0.55f, 0.55f, 0.6f);
                case CellKind.Obstacle: return new Color(0.5f, 0.4f, 0.55f);
                default: return Color.clear;
            }
        }

        public static Mesh BuildTerrainMesh(SpatialData s)
        {
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            float cs = s.CellSizeMm / 1000f;

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color col)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                for (int k = 0; k < 4; k++) cols.Add(col);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2); tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }

            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            for (int z = 0; z < s.Depth; z++)
                for (int x = 0; x < s.Width; x++)
                {
                    var kind = s.KindAt(x, z);
                    if (kind == CellKind.Void) continue;
                    float x0 = (s.OriginXMm + x * s.CellSizeMm) / 1000f, z0 = (s.OriginZMm + z * s.CellSizeMm) / 1000f;
                    float h = s.HeightAt(x, z) / 1000f;
                    var col = ColorFor(kind, s.IsHazard(x, z));
                    Quad(new Vector3(x0, h, z0), new Vector3(x0, h, z0 + cs), new Vector3(x0 + cs, h, z0 + cs), new Vector3(x0 + cs, h, z0), col);

                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + dx[k], nz = z + dz[k];
                        float nh = s.InBounds(nx, nz) && s.KindAt(nx, nz) != CellKind.Void ? s.HeightAt(nx, nz) / 1000f : -0.02f;
                        if (nh >= h) continue;
                        var side = col * 0.75f; side.a = 1f;
                        Vector3 p0, p1;
                        if (k == 0) { p0 = new Vector3(x0 + cs, 0, z0); p1 = new Vector3(x0 + cs, 0, z0 + cs); }
                        else if (k == 1) { p0 = new Vector3(x0, 0, z0 + cs); p1 = new Vector3(x0, 0, z0); }
                        else if (k == 2) { p0 = new Vector3(x0 + cs, 0, z0 + cs); p1 = new Vector3(x0, 0, z0 + cs); }
                        else { p0 = new Vector3(x0, 0, z0); p1 = new Vector3(x0 + cs, 0, z0); }
                        Quad(new Vector3(p0.x, nh, p0.z), new Vector3(p1.x, nh, p1.z), new Vector3(p1.x, h, p1.z), new Vector3(p0.x, h, p0.z), side);
                    }
                }

            var mesh = new Mesh { name = "StageTerrain", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
