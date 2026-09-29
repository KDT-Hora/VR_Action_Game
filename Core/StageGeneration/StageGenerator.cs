using System;
using System.Collections.Generic;
using System.Linq;
using VrAction.Core.Model;
using VrAction.Core.Rng;

namespace VrAction.Core.StageGeneration
{
    /// <summary>
    /// Full deterministic generation: layout (start/goal/path) + rule-based element placement + stage-type rules.
    /// </summary>
    public sealed class StageGenerator : IStageGenerator
    {
        readonly ExplorationGenerator _layout = new ExplorationGenerator();
        readonly StageValidator _validator = new StageValidator();

        struct Plan
        {
            public int EnemyDiv, EnemyCap, ItemDiv, GimmickDiv;
            public ObjectiveKind Objective;
        }

        static Plan PlanFor(StageType t)
        {
            switch (t)
            {
                case StageType.Treasure: return new Plan { EnemyDiv = 90, EnemyCap = 8, ItemDiv = 100, GimmickDiv = 100, Objective = ObjectiveKind.OpenChest };
                case StageType.Combat: return new Plan { EnemyDiv = 30, EnemyCap = 30, ItemDiv = 120, GimmickDiv = 0, Objective = ObjectiveKind.DefeatAll };
                case StageType.Defense: return new Plan { EnemyDiv = 50, EnemyCap = 18, ItemDiv = 120, GimmickDiv = 0, Objective = ObjectiveKind.DefendPoint };
                case StageType.Boss: return new Plan { EnemyDiv = 100, EnemyCap = 6, ItemDiv = 100, GimmickDiv = 150, Objective = ObjectiveKind.DefeatBoss };
                default: return new Plan { EnemyDiv = 60, EnemyCap = 12, ItemDiv = 80, GimmickDiv = 100, Objective = ObjectiveKind.ReachGoal };
            }
        }

        public GenerationOutcome Generate(SpatialData space, GenerationRequest request)
        {
            var baseOutcome = _layout.Generate(space, request);
            if (!baseOutcome.IsSuccess) return baseOutcome;
            var layout = baseOutcome.Stage;

            var dist = Pathing.Distances(space, layout.Start);
            var plan = PlanFor(request.StageType);
            int area = dist.Count(d => d >= 0);

            var onPath = new HashSet<Cell>(layout.Path);
            var candidates = new List<Cell>();
            for (int i = 0; i < dist.Length; i++)
            {
                if (dist[i] < 0) continue;
                var c = new Cell(i % space.Width, i / space.Width);
                if (onPath.Contains(c) || space.IsHazard(c.X, c.Z)) continue;
                if (Math.Abs(c.X - layout.Start.X) + Math.Abs(c.Z - layout.Start.Z) < 4) continue;
                candidates.Add(c);
            }

            // deterministic partial Fisher-Yates shuffle
            var rng = new DeterministicRng(request.Seed).Fork("elements");
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                var t = candidates[i]; candidates[i] = candidates[j]; candidates[j] = t;
            }

            var elements = new List<StageElement>();
            int next = 0;
            Cell? Take() => next < candidates.Count ? candidates[next++] : (Cell?)null;

            int enemies = Math.Min(plan.EnemyCap, Math.Max(request.StageType == StageType.Combat ? 6 : 1, area / plan.EnemyDiv));
            for (int i = 0; i < enemies; i++)
            {
                var c = Take(); if (c == null) break;
                // Param = wave number (used by Defense stages), 1..3
                elements.Add(new StageElement(ElementKind.Enemy, c.Value, request.StageType == StageType.Defense ? 1 + i % 3 : 0));
            }
            int items = Math.Max(1, area / plan.ItemDiv);
            for (int i = 0; i < items; i++)
            {
                var c = Take(); if (c == null) break;
                elements.Add(new StageElement(ElementKind.Item, c.Value));
            }
            if (plan.GimmickDiv > 0)
            {
                int gimmicks = Math.Max(1, area / plan.GimmickDiv);
                for (int i = 0; i < gimmicks; i++)
                {
                    var c = Take(); if (c == null) break;
                    elements.Add(new StageElement(ElementKind.Gimmick, c.Value));
                }
            }

            // checkpoint halfway along the main path (may share cells with the path)
            elements.Add(new StageElement(ElementKind.Checkpoint, layout.Path[layout.Path.Count / 2]));

            switch (request.StageType)
            {
                case StageType.Treasure: elements.Add(new StageElement(ElementKind.Chest, layout.Goal)); break;
                case StageType.Boss: elements.Add(new StageElement(ElementKind.Boss, layout.Goal)); break;
                case StageType.Defense: elements.Add(new StageElement(ElementKind.Gimmick, layout.Goal, 99)); break; // defend point
            }

            var stage = layout.WithElements(elements, plan.Objective);
            if (!_validator.Validate(stage, space).IsValid) return GenerationOutcome.Fail(GenerationFailure.Invalid);
            return GenerationOutcome.Success(stage);
        }
    }
}
