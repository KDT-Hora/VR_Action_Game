namespace VrAction.Core.Model
{
    /// <summary>Shared constants for the abstraction and generation rules.</summary>
    public static class SpatialRules
    {
        public const int CellSizeMm = 50;
        /// <summary>Largest height difference the small character can step or jump between adjacent cells.</summary>
        public const int MaxStepMm = 150;
        /// <summary>Minimum connected walkable area (25 cm x 25 cm x 4 = 0.25 sq m at 5cm cells... 100 cells).</summary>
        public const int MinPlayableCells = 100;
        public const int MinPathCells = 8;
    }
}
