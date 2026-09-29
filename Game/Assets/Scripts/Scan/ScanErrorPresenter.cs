using VrAction.Core;

namespace VrAction.Game.Scan
{
    /// <summary>Player-facing text for scan failures (FR-004 edge cases).</summary>
    public static class ScanErrorPresenter
    {
        public static string Message(ScanError e)
        {
            switch (e)
            {
                case ScanError.NoSurfaces: return "Nothing was detected. Look around slowly and scan again.";
                case ScanError.NoTable: return "No table found. Point at a desk or table and scan again.";
                case ScanError.NoFloor: return "No floor found. Scan the floor of your room and try again.";
                case ScanError.TooSmall: return "The surface is too small to play on. Try a larger table or room.";
                default: return string.Empty;
            }
        }
    }
}
