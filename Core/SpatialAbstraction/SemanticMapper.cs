using VrAction.Core.Model;

namespace VrAction.Core.SpatialAbstraction
{
    public sealed class SemanticMapping
    {
        public CellKind Kind { get; }
        public int HeightMm { get; }
        public SemanticMapping(CellKind kind, int heightMm) { Kind = kind; HeightMm = heightMm; }
    }

    /// <summary>
    /// Gives real-world things a different meaning in the game (FR-011).
    /// Uses only coarse class and size (spec Q6), never object labels.
    /// </summary>
    public static class SemanticMapper
    {
        /// <summary>Furniture at least this tall reads as a ruin (wall); lower furniture reads as a mountain (obstacle).</summary>
        public const int TallFurnitureMm = 1200;

        /// <returns>null when the surface has no ground-plane meaning (ceiling, other).</returns>
        public static SemanticMapping Map(Surface s)
        {
            switch (s.Kind)
            {
                case SurfaceKind.Table: return new SemanticMapping(CellKind.Platform, s.TopY);   // desk -> plateau
                case SurfaceKind.Step: return new SemanticMapping(CellKind.Platform, s.TopY);    // step -> cliff
                case SurfaceKind.Furniture:
                    return s.TopY >= TallFurnitureMm
                        ? new SemanticMapping(CellKind.Wall, s.TopY)                            // shelf -> ruin
                        : new SemanticMapping(CellKind.Obstacle, s.TopY);                       // sofa -> mountain
                case SurfaceKind.Wall: return new SemanticMapping(CellKind.Wall, s.TopY);
                case SurfaceKind.Floor: return new SemanticMapping(CellKind.Ground, 0);
                default: return null;
            }
        }
    }
}
