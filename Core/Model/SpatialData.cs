using System;

namespace VrAction.Core.Model
{
    public enum CellKind : byte { Void = 0, Ground = 1, Wall = 2, Obstacle = 3, Platform = 4 }

    public enum SpaceMode { Tabletop, Room }

    /// <summary>Simplified game-space grid. Immutable; arrays are copied in and never exposed.</summary>
    public sealed class SpatialData
    {
        readonly CellKind[] _kinds;
        readonly int[] _heights;
        readonly bool[] _hazards;

        public int CellSizeMm { get; }
        public int Width { get; }
        public int Depth { get; }
        public int OriginXMm { get; }
        public int OriginZMm { get; }
        public SpaceMode Mode { get; }

        public SpatialData(SpaceMode mode, int cellSizeMm, int width, int depth, int originXMm, int originZMm,
                           CellKind[] kinds, int[] heightsMm, bool[] hazards)
        {
            if (width <= 0 || depth <= 0 || cellSizeMm <= 0) throw new ArgumentException("invalid grid");
            int n = width * depth;
            if (kinds.Length != n || heightsMm.Length != n || hazards.Length != n) throw new ArgumentException("size mismatch");
            Mode = mode; CellSizeMm = cellSizeMm; Width = width; Depth = depth; OriginXMm = originXMm; OriginZMm = originZMm;
            _kinds = (CellKind[])kinds.Clone();
            _heights = (int[])heightsMm.Clone();
            _hazards = (bool[])hazards.Clone();
        }

        public bool InBounds(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth;
        int I(int x, int z) => z * Width + x;

        public CellKind KindAt(int x, int z) => InBounds(x, z) ? _kinds[I(x, z)] : CellKind.Void;
        public int HeightAt(int x, int z) => InBounds(x, z) ? _heights[I(x, z)] : 0;
        public bool IsHazard(int x, int z) => InBounds(x, z) && _hazards[I(x, z)];
        public bool IsWalkable(int x, int z)
        {
            var k = KindAt(x, z);
            return k == CellKind.Ground || k == CellKind.Platform;
        }

        public CellKind[] KindsCopy() => (CellKind[])_kinds.Clone();
        public int[] HeightsCopy() => (int[])_heights.Clone();
        public bool[] HazardsCopy() => (bool[])_hazards.Clone();
    }
}
