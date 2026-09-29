using System.Threading.Tasks;
using VrAction.Core.Fixtures;
using VrAction.Core.Model;

namespace VrAction.Game.Scan
{
    public enum ScanMode { Tabletop, Room }

    /// <summary>Source of raw scan data. Development uses fixtures; the headset provider is added in Phase 9 (T067-T069).</summary>
    public interface IScanProvider
    {
        Task<ScanResult> ScanAsync(ScanMode mode);
    }

    /// <summary>Fixed pseudo-scan data so the game runs without a headset (desk or room).</summary>
    public sealed class FixtureScanProvider : IScanProvider
    {
        public Task<ScanResult> ScanAsync(ScanMode mode)
            => Task.FromResult(mode == ScanMode.Tabletop ? ScanFixtures.Desk() : ScanFixtures.Room());
    }
}
