using System.Collections.Generic;
using VrAction.Core.Model;

namespace VrAction.Core.StageGeneration
{
    /// <summary>Grid movement rules and deterministic BFS (fixed neighbour order).</summary>
    public static class Pathing
    {
        static readonly int[] Dx = { 1, -1, 0, 0 };
        static readonly int[] Dz = { 0, 0, 1, -1 };

        public static bool CanStep(SpatialData s, Cell a, Cell b)
        {
            int man = System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Z - b.Z);
            if (man != 1) return false;
            if (!s.IsWalkable(a.X, a.Z) || !s.IsWalkable(b.X, b.Z)) return false;
            return System.Math.Abs(s.HeightAt(a.X, a.Z) - s.HeightAt(b.X, b.Z)) <= SpatialRules.MaxStepMm;
        }

        /// <summary>Distance in steps from start for every cell (index z*Width+x); -1 when unreachable.</summary>
        public static int[] Distances(SpatialData s, Cell start, out int[] parents)
        {
            var dist = new int[s.Width * s.Depth];
            parents = new int[s.Width * s.Depth];
            for (int i = 0; i < dist.Length; i++) { dist[i] = -1; parents[i] = -1; }
            if (!s.IsWalkable(start.X, start.Z)) return dist;
            var q = new Queue<Cell>();
            dist[start.Z * s.Width + start.X] = 0;
            q.Enqueue(start);
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    var n = new Cell(c.X + Dx[k], c.Z + Dz[k]);
                    if (!CanStep(s, c, n)) continue;
                    int ni = n.Z * s.Width + n.X;
                    if (dist[ni] >= 0) continue;
                    dist[ni] = dist[c.Z * s.Width + c.X] + 1;
                    parents[ni] = c.Z * s.Width + c.X;
                    q.Enqueue(n);
                }
            }
            return dist;
        }

        public static int[] Distances(SpatialData s, Cell start) => Distances(s, start, out _);

        /// <summary>Shortest path including both ends, or an empty list when unreachable.</summary>
        public static List<Cell> ShortestPath(SpatialData s, Cell from, Cell to)
        {
            var dist = Distances(s, from, out var parents);
            int ti = to.Z * s.Width + to.X;
            var path = new List<Cell>();
            if (!s.InBounds(to.X, to.Z) || dist[ti] < 0) return path;
            for (int i = ti; i != -1; i = parents[i]) path.Add(new Cell(i % s.Width, i / s.Width));
            path.Reverse();
            return path;
        }

        /// <summary>Connected walkable components, each as a list of cell indices; ordered by first cell index.</summary>
        public static List<List<int>> Components(SpatialData s)
        {
            var seen = new bool[s.Width * s.Depth];
            var result = new List<List<int>>();
            for (int i = 0; i < seen.Length; i++)
            {
                if (seen[i] || !s.IsWalkable(i % s.Width, i / s.Width)) continue;
                var dist = Distances(s, new Cell(i % s.Width, i / s.Width));
                var comp = new List<int>();
                for (int j = 0; j < dist.Length; j++) if (dist[j] >= 0) { comp.Add(j); seen[j] = true; }
                result.Add(comp);
            }
            return result;
        }
    }
}
