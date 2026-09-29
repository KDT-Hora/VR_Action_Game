using System.Linq;
using VrAction.Core.Model;

namespace VrAction.Core.StageGeneration
{
    public sealed class StageValidator : IStageValidator
    {
        public ValidationReport Validate(Stage stage, SpatialData space)
        {
            if (!space.IsWalkable(stage.Start.X, stage.Start.Z)) return new ValidationReport(false, "start is not walkable");
            if (!space.IsWalkable(stage.Goal.X, stage.Goal.Z)) return new ValidationReport(false, "goal is not walkable");
            if (stage.Start.Equals(stage.Goal)) return new ValidationReport(false, "start equals goal");
            if (stage.Path.Count == 0 || !stage.Path[0].Equals(stage.Start) || !stage.Path[stage.Path.Count - 1].Equals(stage.Goal))
                return new ValidationReport(false, "path does not connect start to goal");
            for (int i = 1; i < stage.Path.Count; i++)
                if (!Pathing.CanStep(space, stage.Path[i - 1], stage.Path[i]))
                    return new ValidationReport(false, "path has an impassable step at index " + i);

            var dist = Pathing.Distances(space, stage.Start);
            if (dist[stage.Goal.Z * space.Width + stage.Goal.X] < 0) return new ValidationReport(false, "goal unreachable");
            int area = dist.Count(d => d >= 0);
            if (area < SpatialRules.MinPlayableCells) return new ValidationReport(false, "playable area too small (" + area + " cells)");
            return new ValidationReport(true);
        }
    }
}
