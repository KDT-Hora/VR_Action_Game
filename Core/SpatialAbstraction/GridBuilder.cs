using System;
using VrAction.Core.Model;

namespace VrAction.Core.SpatialAbstraction
{
    /// <summary>Mutable working grid used while abstracting a scan; produces an immutable SpatialData.</summary>
    internal sealed class GridBuilder
    {
        public readonly int Width, Depth, OriginX, OriginZ, Cell;
        public readonly CellKind[] Kinds;
        public readonly int[] Heights;

        public GridBuilder(int originX, int originZ, int width, int depth, int cell, CellKind fill)
        {
            OriginX = originX; OriginZ = originZ; Width = width; Depth = depth; Cell = cell;
            Kinds = new CellKind[width * depth];
            Heights = new int[width * depth];
            for (int i = 0; i < Kinds.Length; i++) Kinds[i] = fill;
        }

        public int CenterX(int x) => OriginX + x * Cell + Cell / 2;
        public int CenterZ(int z) => OriginZ + z * Cell + Cell / 2;
        public bool In(int x, int z) => x >= 0 && z >= 0 && x < Width && z < Depth;
        public int I(int x, int z) => z * Width + x;

        /// <summary>Calls action for every cell whose centre lies inside [minX,maxX) x [minZ,maxZ).</summary>
        public void ForCellsIn(int minX, int minZ, int maxX, int maxZ, Action<int, int> action)
        {
            for (int z = 0; z < Depth; z++)
            {
                int cz = CenterZ(z);
                if (cz < minZ || cz >= maxZ) continue;
                for (int x = 0; x < Width; x++)
                {
                    int cx = CenterX(x);
                    if (cx >= minX && cx < maxX) action(x, z);
                }
            }
        }

        static bool Walkable(CellKind k) => k == CellKind.Ground || k == CellKind.Platform;

        /// <summary>
        /// A walkable cell is a hazard if a neighbour is Void, a drop larger than the max step,
        /// or (when outsideIsHazard) lies outside the grid.
        /// </summary>
        public bool[] ComputeHazards(bool outsideIsHazard)
        {
            var hz = new bool[Kinds.Length];
            int[] dx = { 1, -1, 0, 0 }, dz = { 0, 0, 1, -1 };
            for (int z = 0; z < Depth; z++)
                for (int x = 0; x < Width; x++)
                {
                    int i = I(x, z);
                    if (!Walkable(Kinds[i])) continue;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + dx[k], nz = z + dz[k];
                        if (!In(nx, nz)) { if (outsideIsHazard) hz[i] = true; continue; }
                        int ni = I(nx, nz);
                        if (Kinds[ni] == CellKind.Void) hz[i] = true;
                        else if (Walkable(Kinds[ni]) && Heights[i] - Heights[ni] > SpatialRules.MaxStepMm) hz[i] = true;
                    }
                }
            return hz;
        }

        public SpatialData Build(PlayMode mode, bool outsideIsHazard)
            => new SpatialData(mode, Cell, Width, Depth, OriginX, OriginZ, Kinds, Heights, ComputeHazards(outsideIsHazard));
    }
}
