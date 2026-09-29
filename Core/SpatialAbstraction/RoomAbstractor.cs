using System;
using System.Collections.Generic;
using System.Linq;
using VrAction.Core.Model;

namespace VrAction.Core.SpatialAbstraction
{
    /// <summary>
    /// Room mode: floor becomes the field, walls the boundary, desks and steps become raised platforms
    /// (bridged by ramps so they stay reachable), low furniture becomes obstacles, tall furniture walls.
    /// </summary>
    public sealed class RoomAbstractor : ISpatialAbstractor
    {
        public AbstractionResult Abstract(ScanResult scan)
        {
            if (scan == null || scan.Surfaces.Count == 0) return AbstractionResult.Fail(ScanError.NoSurfaces);
            var floors = scan.Surfaces.Where(s => s.Kind == SurfaceKind.Floor).ToList();
            if (floors.Count == 0) return AbstractionResult.Fail(ScanError.NoFloor);

            int cell = SpatialRules.CellSizeMm;
            int minX = floors.Min(f => f.MinX), minZ = floors.Min(f => f.MinZ);
            int maxX = floors.Max(f => f.MaxX), maxZ = floors.Max(f => f.MaxZ);
            int w = (maxX - minX) / cell, d = (maxZ - minZ) / cell;
            if (w * d < SpatialRules.MinPlayableCells) return AbstractionResult.Fail(ScanError.TooSmall);

            var g = new GridBuilder(minX, minZ, w, d, cell, CellKind.Void);
            foreach (var f in floors)
                g.ForCellsIn(f.MinX, f.MinZ, f.MaxX, f.MaxZ, (x, z) => { g.Kinds[g.I(x, z)] = CellKind.Ground; g.Heights[g.I(x, z)] = 0; });

            // Apply in a fixed order so the result never depends on scan ordering: platforms, furniture, then walls on top.
            var ordered = scan.Surfaces
                .Where(s => s.Kind != SurfaceKind.Floor)
                .Select(s => new { S = s, M = SemanticMapper.Map(s) })
                .Where(x => x.M != null)
                .OrderBy(x => Rank(x.M.Kind)).ThenBy(x => x.S.MinX).ThenBy(x => x.S.MinZ).ThenBy(x => x.S.TopY)
                .ToList();

            var platformBoxes = new List<int[]>(); // px0, pz0, px1, pz1, top
            foreach (var item in ordered)
            {
                int cx0 = int.MaxValue, cz0 = int.MaxValue, cx1 = int.MinValue, cz1 = int.MinValue;
                var s = item.S; var m = item.M;
                g.ForCellsIn(s.MinX, s.MinZ, s.MaxX, s.MaxZ, (x, z) =>
                {
                    int i = g.I(x, z);
                    if (g.Kinds[i] == CellKind.Void) return; // nothing stands outside the floor
                    g.Kinds[i] = m.Kind; g.Heights[i] = m.HeightMm;
                    cx0 = Math.Min(cx0, x); cz0 = Math.Min(cz0, z); cx1 = Math.Max(cx1, x); cz1 = Math.Max(cz1, z);
                });
                if (m.Kind == CellKind.Platform && cx0 != int.MaxValue) platformBoxes.Add(new[] { cx0, cz0, cx1, cz1, m.HeightMm });
            }

            foreach (var b in platformBoxes) TryBuildRamp(g, b[0], b[1], b[2], b[3], b[4]);

            var data = g.Build(SpaceMode.Room, outsideIsHazard: false);
            return AbstractionResult.Ok(data);
        }

        static int Rank(CellKind k)
        {
            switch (k) { case CellKind.Platform: return 0; case CellKind.Obstacle: return 1; case CellKind.Wall: return 2; default: return 3; }
        }

        /// <summary>Stairs down one side of a platform so the small character can climb it (max step 150mm).</summary>
        static void TryBuildRamp(GridBuilder g, int x0, int z0, int x1, int z1, int top)
        {
            int step = SpatialRules.MaxStepMm;
            int rampCells = (top + step - 1) / step - 1;
            if (rampCells <= 0) return;

            int zc = (z0 + z1) / 2, xc = (x0 + x1) / 2;
            // sides in fixed order: -x, +x, -z, +z ; each: (dx, dz), origin cell just outside the platform
            int[][] sides =
            {
                new[] { -1, 0, x0 - 1, zc },
                new[] { 1, 0, x1 + 1, zc },
                new[] { 0, -1, xc, z0 - 1 },
                new[] { 0, 1, xc, z1 + 1 },
            };
            foreach (var sd in sides)
            {
                int dx = sd[0], dz = sd[1];
                // two-cell-wide lane, perpendicular offset 0 and +1
                var lane = new List<KeyValuePair<int, int>>();
                bool ok = true;
                for (int k = 0; k <= rampCells && ok; k++)
                    for (int w = 0; w < 2 && ok; w++)
                    {
                        int x = sd[2] + dx * k + (dx == 0 ? w : 0);
                        int z = sd[3] + dz * k + (dz == 0 ? w : 0);
                        if (!g.In(x, z) || g.Kinds[g.I(x, z)] != CellKind.Ground || g.Heights[g.I(x, z)] != 0) { ok = false; break; }
                        lane.Add(new KeyValuePair<int, int>(g.I(x, z), k));
                    }
                if (!ok) continue;
                foreach (var c in lane)
                    if (c.Value < rampCells) g.Heights[c.Key] = top - (c.Value + 1) * step; // landing cell (k == rampCells) stays on the floor
                return;
            }
        }
    }
}
