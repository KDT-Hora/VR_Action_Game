using System.Collections.Generic;
using System.Linq;
using VrAction.Core.Model;
using VrAction.Core.Rng;

namespace VrAction.Core.StageGeneration
{
    /// <summary>
    /// Rule-based, deterministic layout generator: picks start and goal in the largest connected
    /// walkable area and verifies reachability with BFS. No randomness other than the seeded PRNG.
    /// </summary>
    public sealed class ExplorationGenerator : IStageGenerator
    {
        const int MaxAttempts = 8;
        readonly StageValidator _validator = new StageValidator();

        public GenerationOutcome Generate(SpatialData space, GenerationRequest request)
        {
            var comps = Pathing.Components(space);
            if (comps.Count == 0) return GenerationOutcome.Fail(GenerationFailure.TooSmall);
            var largest = comps.OrderByDescending(c => c.Count).ThenBy(c => c[0]).First();
            if (largest.Count < SpatialRules.MinPlayableCells) return GenerationOutcome.Fail(GenerationFailure.TooSmall);

            var safe = largest.Where(i => !space.IsHazard(i % space.Width, i / space.Width)).ToList();
            if (safe.Count == 0) safe = largest;
            var safeGoal = safe;

            var root = new DeterministicRng(request.Seed);
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var rng = root.Fork("layout" + attempt);
                int si = safe[rng.Range(0, safe.Count)];
                var start = new Cell(si % space.Width, si / space.Width);
                var dist = Pathing.Distances(space, start);

                int best = -1, bestDist = -1;
                foreach (int i in safeGoal)
                    if (dist[i] > bestDist) { bestDist = dist[i]; best = i; }
                if (best < 0 || bestDist < 1) continue;

                var goal = new Cell(best % space.Width, best / space.Width);
                var path = Pathing.ShortestPath(space, start, goal);
                if (path.Count < SpatialRules.MinPathCells) continue;

                var stage = new Stage(request, start, goal, path, new StageElement[0], ObjectiveKind.ReachGoal);
                if (_validator.Validate(stage, space).IsValid) return GenerationOutcome.Success(stage);
            }
            return GenerationOutcome.Fail(GenerationFailure.NoValidLayout);
        }
    }
}
