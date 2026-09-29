using VrAction.Core.Model;

namespace VrAction.Core.Fixtures
{
    /// <summary>
    /// Fixed pseudo-scan data used for development and tests (no headset required).
    /// Units are millimetres.
    /// </summary>
    public static class ScanFixtures
    {
        /// <summary>A 1200x600 desk (top at 720mm) standing on a floor, with a small mug on it.</summary>
        public static ScanResult Desk()
        {
            return new ScanResult("fixture-desk", "Fixture", new[]
            {
                new Surface(SurfaceKind.Floor, -600, -600, 1800, 1200, 0, 0),
                new Surface(SurfaceKind.Table, 0, 0, 1200, 600, 700, 720),
                // small item on the desk (mug)
                new Surface(SurfaceKind.Furniture, 900, 300, 960, 360, 720, 800),
            });
        }

        /// <summary>A 4.0m x 3.0m room (12 sq m) with walls, a desk, a low sofa, a tall shelf and a step.</summary>
        public static ScanResult Room()
        {
            return new ScanResult("fixture-room", "Fixture", new[]
            {
                new Surface(SurfaceKind.Floor, 0, 0, 4000, 3000, 0, 0),
                new Surface(SurfaceKind.Ceiling, 0, 0, 4000, 3000, 2400, 2400),
                // walls along the perimeter (100mm thick, inside the floor rectangle)
                new Surface(SurfaceKind.Wall, 0, 0, 4000, 100, 0, 2400),
                new Surface(SurfaceKind.Wall, 0, 2900, 4000, 3000, 0, 2400),
                new Surface(SurfaceKind.Wall, 0, 0, 100, 3000, 0, 2400),
                new Surface(SurfaceKind.Wall, 3900, 0, 4000, 3000, 0, 2400),
                // desk: becomes a raised platform
                new Surface(SurfaceKind.Table, 2500, 300, 3700, 900, 700, 720),
                // low wide sofa: becomes an obstacle (mountain)
                new Surface(SurfaceKind.Furniture, 400, 2000, 2200, 2800, 0, 850),
                // tall shelf: becomes a wall-like ruin
                new Surface(SurfaceKind.Furniture, 3700, 1300, 3900, 2500, 0, 1800),
                // small step: becomes a raised platform (cliff)
                new Surface(SurfaceKind.Step, 200, 300, 800, 700, 0, 300),
            });
        }

        /// <summary>Too small to play on (30cm x 30cm table).</summary>
        public static ScanResult TinyTable()
        {
            return new ScanResult("fixture-tiny", "Fixture", new[]
            {
                new Surface(SurfaceKind.Floor, 0, 0, 1000, 1000, 0, 0),
                new Surface(SurfaceKind.Table, 0, 0, 300, 300, 700, 720),
            });
        }

        public static ScanResult Empty() => new ScanResult("fixture-empty", "Fixture", new Surface[0]);
    }
}
