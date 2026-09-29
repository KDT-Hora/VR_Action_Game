using System.Collections.Generic;
using System.Linq;
using VrAction.Core;
using VrAction.Core.Model;
using VrAction.Core.SpatialAbstraction;
using VrAction.Game.Scan;

namespace VrAction.Game.Game
{
    /// <summary>Tracking-loss pause (edge case): the game freezes and resumes when tracking returns.</summary>
    public sealed class SafetyNet
    {
        public bool IsPaused { get; private set; }
        public void SetTrackingLost(bool lost) { IsPaused = lost; }
    }

    /// <summary>Player choices that persist between stages.</summary>
    public sealed class GameSession
    {
        ulong _seed = 1;
        public DifficultyMode Difficulty { get; set; } = DifficultyMode.CheckpointRespawn;
        public int CharacterHeightCm { get; set; } = 10;
        public ulong NextSeed() => _seed++;
    }

    /// <summary>Chooses Tabletop or Room, scans (or fixture), and abstracts the result (FR-006).</summary>
    public static class ModeSelect
    {
        public static AbstractionResult LoadSpace(SpaceMode mode, IScanProvider provider)
        {
            var scan = provider.ScanAsync(mode == SpaceMode.Tabletop ? ScanMode.Tabletop : ScanMode.Room).Result;
            ISpatialAbstractor abstractor = mode == SpaceMode.Tabletop ? (ISpatialAbstractor)new SpatialAbstractor() : new RoomAbstractor();
            return abstractor.Abstract(scan);
        }
    }

    /// <summary>Difficulty is chosen before a stage starts (FR-026).</summary>
    public sealed class DifficultySelect
    {
        readonly GameSession _session;
        public IReadOnlyList<DifficultyMode> Options { get; } = new[] { DifficultyMode.CheckpointRespawn, DifficultyMode.NoDeath };
        public DifficultySelect(GameSession session) { _session = session; }
        public void Choose(DifficultyMode mode) { _session.Difficulty = mode; }
    }

    /// <summary>After a clear, lets the player pick the next stage type from the same space (FR-024).</summary>
    public sealed class NextStageMenu
    {
        readonly StageFlow _flow;
        readonly GameSession _session;
        public bool Visible { get; private set; }
        public IReadOnlyList<StageType> Options { get; } =
            System.Enum.GetValues(typeof(StageType)).Cast<StageType>().ToList();

        public NextStageMenu(StageFlow flow, GameSession session)
        {
            _flow = flow; _session = session;
            flow.Cleared += _ => Visible = true;
        }

        public bool Choose(StageType type)
        {
            var req = new GenerationRequest(_session.NextSeed(), type, _session.Difficulty, _session.CharacterHeightCm);
            bool ok = _flow.BeginNext(req);
            if (ok) Visible = false;
            return ok;
        }
    }
}
