using System.Collections.Generic;
using System.Linq;
using VrAction.Core.Model;

namespace VrAction.Core.SpatialAbstraction
{
    /// <summary>Tabletop mode: turns the largest table surface into a play grid; things on the table become obstacles.</summary>
    public sealed class SpatialAbstractor : ISpatialAbstractor
    {
        public AbstractionResult Abstract(ScanResult scan)
        {
            if (scan == null || scan.Surfaces.Count == 0) return AbstractionResult.Fail(ScanError.NoSurfaces);

            var tables = scan.Surfaces.Where(s => s.Kind == SurfaceKind.Table)
                .OrderByDescending(s => (long)s.WidthX * s.DepthZ).ThenBy(s => s.MinX).ThenBy(s => s.MinZ).ToList();
            if (tables.Count == 0) return AbstractionResult.Fail(ScanError.NoTable);
            var table = tables[0];

            int cell = SpatialRules.CellSizeMm;
            int w = table.WidthX / cell, d = table.DepthZ / cell;
            if (w * d < SpatialRules.MinPlayableCells) return AbstractionResult.Fail(ScanError.TooSmall);

            var g = new GridBuilder(table.MinX, table.MinZ, w, d, cell, CellKind.Ground);
            foreach (var s in scan.Surfaces
                .Where(s => s.Kind == SurfaceKind.Furniture || s.Kind == SurfaceKind.Other || s.Kind == SurfaceKind.Step)
                .Where(s => s.BottomY >= table.TopY - 20)
                .OrderBy(s => s.MinX).ThenBy(s => s.MinZ).ThenBy(s => s.TopY))
            {
                int rel = s.TopY - table.TopY;
                g.ForCellsIn(s.MinX, s.MinZ, s.MaxX, s.MaxZ, (x, z) =>
                {
                    int i = g.I(x, z);
                    g.Kinds[i] = CellKind.Obstacle;
                    if (rel > g.Heights[i]) g.Heights[i] = rel;
                });
            }
            return AbstractionResult.Ok(g.Build(SpaceMode.Tabletop, outsideIsHazard: true));
        }
    }

    /// <summary>Play area and hazard zones for a tabletop grid (FR-014).</summary>
    public sealed class TabletopPlaySpace
    {
        readonly HashSet<Cell> _hazardSet;
        public IReadOnlyList<Cell> HazardCells { get; }
        public int MinXMm { get; }
        public int MaxXMm { get; }
        public int MinZMm { get; }
        public int MaxZMm { get; }

        TabletopPlaySpace(SpatialData s)
        {
            var list = new List<Cell>();
            for (int z = 0; z < s.Depth; z++)
                for (int x = 0; x < s.Width; x++)
                    if (s.IsHazard(x, z)) list.Add(new Cell(x, z));
            HazardCells = list.AsReadOnly();
            _hazardSet = new HashSet<Cell>(list);
            MinXMm = s.OriginXMm; MinZMm = s.OriginZMm;
            MaxXMm = s.OriginXMm + s.Width * s.CellSizeMm;
            MaxZMm = s.OriginZMm + s.Depth * s.CellSizeMm;
        }

        public static TabletopPlaySpace From(SpatialData s) => new TabletopPlaySpace(s);

        public bool IsHazard(int x, int z) => _hazardSet.Contains(new Cell(x, z));

        /// <summary>Distance from a point (mm) to the nearest table edge; 0 or negative when outside.</summary>
        public int DistanceToBoundaryMm(int xMm, int zMm)
        {
            int a = System.Math.Min(xMm - MinXMm, MaxXMm - xMm);
            int b = System.Math.Min(zMm - MinZMm, MaxZMm - zMm);
            return System.Math.Min(a, b);
        }
    }
}
