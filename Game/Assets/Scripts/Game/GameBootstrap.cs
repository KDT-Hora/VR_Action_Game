using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VrAction.Core.Character;
using VrAction.Core.Model;
using VrAction.Core.Serialization;
using VrAction.Game.Character;
using VrAction.Game.Presentation;
using VrAction.Game.Save;
using VrAction.Game.Scan;

namespace VrAction.Game.Game
{
    /// <summary>
    /// Wires a scene together at runtime: rig, scan, stage, hero, menus, save. One instance per scene.
    /// Sandbox = flat fixed floor (no generation from scan); Tabletop/Room use the fixture scan until a headset provider exists.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public enum Source { Sandbox, Tabletop, Room }

        public Source SourceMode = Source.Tabletop;
        public bool AutoStart = true;

        public StageFlow Flow { get; private set; }
        public NextStageMenu NextMenu { get; private set; }
        public GameSession Session { get; } = new GameSession();
        public SaveStore Store { get; private set; }
        public PeekRig Rig { get; private set; }
        public MenuPanel Panel { get; private set; }

        readonly List<ClearedStage> _cleared = new List<ClearedStage>();
        Transform _stageRoot;
        IScanProvider _scan = new FixtureScanProvider();

        void Start()
        {
            if (AutoStart) StartFlow(chooseDifficultyFirst: true);
        }

        public void StartFlow(bool chooseDifficultyFirst)
        {
            Store = new SaveStore(Path.Combine(Application.persistentDataPath, "save_" + SourceMode + ".json"));
            Rig = PeekRig.Create();
            var posture = Rig.gameObject.AddComponent<PostureSetup>();
            posture.Apply(PostureMode.Standing);
            var safety = Rig.gameObject.AddComponent<SafetyMonitorBehaviour>();

            var stageGo = new GameObject("Stage");
            _stageRoot = stageGo.transform;
            Flow = stageGo.AddComponent<StageFlow>();

            var space = LoadOrScan(out string error);
            if (space == null) { Debug.LogWarning(error); return; }

            PlaceStage(space, safety);

            NextMenu = new NextStageMenu(Flow, Session);
            Flow.Cleared += OnCleared;

            var input = stageGo.AddComponent<CharacterInput>();
            Panel = MenuPanel.Create(Rig.HeadCamera.transform);
            Panel.Bind(this);

            if (chooseDifficultyFirst) Panel.ShowDifficulty(new DifficultySelect(Session), () => BeginStage(space, StageType.Exploration, input));
            else BeginStage(space, StageType.Exploration, input);
        }

        SpatialData LoadOrScan(out string error)
        {
            error = null;
            var saved = SafeLoad();
            if (saved != null && saved.SpatialData.Mode == ModeFor(SourceMode))
            {
                foreach (var c in saved.ClearedStages) _cleared.Add(c);
                Session.CharacterHeightCm = saved.CharacterHeightCm;
                Session.Difficulty = saved.Difficulty;
                return saved.SpatialData;
            }
            if (SourceMode == Source.Sandbox) return SandboxSpace();
            var r = ModeSelect.LoadSpace(ModeFor(SourceMode), _scan);
            if (!r.IsSuccess) { error = ScanErrorPresenter.Message(r.Error); return null; }
            return r.Data;
        }

        SaveFile SafeLoad()
        {
            try { return Store.Load(); }
            catch (System.Exception e) { Debug.LogWarning("Save could not be read, ignoring: " + e.Message); return null; }
        }

        static SpaceMode ModeFor(Source s) => s == Source.Room ? SpaceMode.Room : SpaceMode.Tabletop;

        static SpatialData SandboxSpace()
        {
            int w = 24, d = 12;
            var kinds = new CellKind[w * d];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = CellKind.Ground;
            var s = new SpatialData(SpaceMode.Tabletop, 50, w, d, 0, 0, kinds, new int[w * d], new bool[w * d]);
            return s;
        }

        void PlaceStage(SpatialData space, SafetyMonitorBehaviour safety)
        {
            if (space.Mode == SpaceMode.Tabletop)
            {
                // stage sits on a 75cm desk, 60cm in front of the player, centred
                float w = space.Width * space.CellSizeMm / 1000f, d = space.Depth * space.CellSizeMm / 1000f;
                _stageRoot.position = new Vector3(-w / 2f, 0.75f, 0.4f);
                safety.SetBoundaryMm(-2000, -2000, 2000, 2000);
            }
            else
            {
                float w = space.Width * space.CellSizeMm / 1000f, d = space.Depth * space.CellSizeMm / 1000f;
                _stageRoot.position = new Vector3(-w / 2f, 0f, -d / 2f);
                safety.SetBoundaryMm(Mathf.RoundToInt(-w * 500f), Mathf.RoundToInt(-d * 500f), Mathf.RoundToInt(w * 500f), Mathf.RoundToInt(d * 500f));
            }
        }

        void BeginStage(SpatialData space, StageType type, CharacterInput input)
        {
            var req = new GenerationRequest(Session.NextSeed(), type, Session.Difficulty, Session.CharacterHeightCm);
            if (!Flow.Begin(space, req, new ScaleSettings(Session.CharacterHeightCm), _stageRoot))
            {
                Debug.LogWarning("Could not generate a stage from this space.");
                return;
            }
            input.Hero = Flow.Hero;
            input.Combat = () => Flow.Combat;
        }

        void OnCleared(Stage stage)
        {
            _cleared.Add(new ClearedStage(stage.Request.Seed, stage.Request.StageType));
            try { Store.Save(new SaveFile(Flow.Space, _cleared, Session.CharacterHeightCm, Session.Difficulty)); }
            catch (System.Exception e) { Debug.LogWarning("Could not save progress: " + e.Message); }
            Panel?.ShowNextStage(NextMenu);
        }
    }
}
