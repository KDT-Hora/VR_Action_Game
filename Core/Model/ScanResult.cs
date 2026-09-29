using System;
using System.Collections.Generic;

namespace VrAction.Core.Model
{
    /// <summary>Coarse classification only (spec Q6): floor/wall/ceiling/desk/large furniture/step.</summary>
    public enum SurfaceKind { Floor, Wall, Ceiling, Table, Furniture, Step, Other }

    /// <summary>Axis-aligned box in millimetres (x/z horizontal, y up). Immutable.</summary>
    public sealed class Surface
    {
        public SurfaceKind Kind { get; }
        public int MinX { get; }
        public int MinZ { get; }
        public int MaxX { get; }
        public int MaxZ { get; }
        public int BottomY { get; }
        public int TopY { get; }

        public Surface(SurfaceKind kind, int minX, int minZ, int maxX, int maxZ, int bottomY, int topY)
        {
            if (maxX < minX || maxZ < minZ || topY < bottomY) throw new ArgumentException("invalid box");
            Kind = kind; MinX = minX; MinZ = minZ; MaxX = maxX; MaxZ = maxZ; BottomY = bottomY; TopY = topY;
        }

        public int WidthX => MaxX - MinX;
        public int DepthZ => MaxZ - MinZ;
        public int Height => TopY - BottomY;
    }

    /// <summary>Raw scan input. Immutable after construction (FR-015).</summary>
    public sealed class ScanResult
    {
        public string Id { get; }
        public string Source { get; }
        public IReadOnlyList<Surface> Surfaces { get; }

        public ScanResult(string id, string source, IEnumerable<Surface> surfaces)
        {
            Id = id; Source = source;
            Surfaces = new List<Surface>(surfaces).AsReadOnly();
        }
    }
}
