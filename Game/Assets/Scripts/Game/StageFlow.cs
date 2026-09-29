using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VrAction.Core.Character;
using VrAction.Core.Model;
using VrAction.Core.StageGeneration;
using VrAction.Game.Character;
using VrAction.Game.Presentation;

namespace VrAction.Game.Game
{
    /// <summary>Generates, renders and runs one stage at a time; decides when the objective is met (FR-013).</summary>
    public sealed class StageFlow : MonoBehaviour
    {
        public enum FlowState { Idle, Playing, Cleared }

        readonly List<EnemyController> _enemies = new List<EnemyController>();
        readonly List<GameObject> _spawned = new List<GameObject>();
        readonly Dictionary<Cell, GameObject> _items = new Dictionary<Cell, GameObject>();
        readonly SafetyNet _safety = new SafetyNet();
        GameObject _visual;
        Transform _stageRoot;
        ScaleSettings _scale;

        public SpatialData Space { get; private set; }
        public Stage Stage { get; private set; }
        public SmallHero Hero { get; private set; }
        public CombatController Combat { get; private set; }
        public FlowState State { get; private set; } = FlowState.Idle;
        public IReadOnlyList<EnemyController> Enemies => _enemies;
        public int ItemsCollected { get; private set; }
        public bool IsPaused => _safety.IsPaused;

        public event System.Action<Stage> Cleared;

        /// <returns>false when no valid stage could be generated (nothing changes).</returns>
        public bool Begin(SpatialData space, GenerationRequest request, ScaleSettings scale, Transform stageRoot)
        {
            var outcome = new StageGenerator().Generate(space, request);
            if (!outcome.IsSuccess) return false;

            Clear();
            Space = space; Stage = outcome.Stage; _scale = scale; _stageRoot = stageRoot;
            _visual = StageRenderer.Build(stageRoot, space, Stage);

            float h = scale.CharacterHeightMm / 1000f;
            if (Hero == null) Hero = SmallHero.Create(stageRoot, space, scale, Stage.Start, request.Difficulty);
            else Hero.Rebind(space, Stage.Start, request.Difficulty);
            Hero.Paused = _safety.IsPaused;
            Combat = new CombatController(Hero, () => _enemies);

            foreach (var e in Stage.Elements) Spawn(e, space, h);
            State = FlowState.Playing;
            return true;
        }

        public bool BeginNext(GenerationRequest request) => Begin(Space, request, _scale, _stageRoot);

        void Spawn(StageElement e, SpatialData space, float h)
        {
            var pos = StageRenderer.CellCenter(space, e.Cell, space.HeightAt(e.Cell.X, e.Cell.Z) / 1000f);
            switch (e.Kind)
            {
                case ElementKind.Enemy:
                case ElementKind.Boss:
                    var en = EnemyController.Spawn(_stageRoot, pos, h, e.Kind == ElementKind.Boss, e.Param);
                    en.Bind(Hero, () => Combat != null && Combat.IsInvulnerable);
                    _enemies.Add(en); _spawned.Add(en.gameObject);
                    break;
                case ElementKind.Item:
                    var it = StageRenderer.Marker(_stageRoot, space, e.Cell, new Color(1f, 0.9f, 0.2f), 0.1f, 0.5f, "Item");
                    _items[e.Cell] = it; _spawned.Add(it);
                    break;
                case ElementKind.Chest:
                    _spawned.Add(StageRenderer.Marker(_stageRoot, space, e.Cell, new Color(0.6f, 0.35f, 0.1f), 0.18f, 0.6f, "Chest"));
                    break;
                case ElementKind.Gimmick:
                    _spawned.Add(StageRenderer.Marker(_stageRoot, space, e.Cell, e.Param == 99 ? new Color(0.2f, 0.9f, 0.9f) : new Color(0.3f, 0.6f, 1f), 0.15f, 0.6f, "Gimmick"));
                    break;
                case ElementKind.Checkpoint:
                    _spawned.Add(StageRenderer.Marker(_stageRoot, space, e.Cell, new Color(0.2f, 1f, 0.3f), 0.08f, 0.3f, "Checkpoint"));
                    break;
            }
        }

        void Clear()
        {
            foreach (var g in _spawned) if (g != null) Destroy(g);
            _spawned.Clear(); _enemies.Clear(); _items.Clear();
            if (_visual != null) Destroy(_visual);
            ItemsCollected = 0;
        }

        public void SetTrackingLost(bool lost)
        {
            _safety.SetTrackingLost(lost);
            if (Hero != null) Hero.Paused = _safety.IsPaused;
        }

        void Update() => Tick(Time.deltaTime);

        public void Tick(float dt)
        {
            if (State != FlowState.Playing || _safety.IsPaused) return;

            Combat.Tick(dt);
            foreach (var e in _enemies) e.Tick(dt);

            var cell = Hero.CurrentCell;
            foreach (var el in Stage.Elements)
                if (el.Kind == ElementKind.Checkpoint && el.Cell.Equals(cell)) Hero.Health.ReachCheckpoint(cell);
            if (_items.TryGetValue(cell, out var item))
            {
                _items.Remove(cell); Destroy(item); ItemsCollected++;
            }
            if (Hero.Health.IsDown) Hero.PlaceAtCell(Hero.Health.Respawn());

            if (ObjectiveMet(cell))
            {
                State = FlowState.Cleared;
                Cleared?.Invoke(Stage);
            }
        }

        bool ObjectiveMet(Cell heroCell)
        {
            switch (Stage.Objective)
            {
                case ObjectiveKind.ReachGoal:
                case ObjectiveKind.OpenChest:
                    return heroCell.Equals(Stage.Goal);
                case ObjectiveKind.DefeatAll:
                case ObjectiveKind.DefendPoint:
                    return _enemies.Count > 0 && _enemies.All(e => e.Hp <= 0);
                case ObjectiveKind.DefeatBoss:
                    return _enemies.Any(e => e.IsBoss) && _enemies.Where(e => e.IsBoss).All(e => e.Hp <= 0);
                default: return false;
            }
        }
    }
}
