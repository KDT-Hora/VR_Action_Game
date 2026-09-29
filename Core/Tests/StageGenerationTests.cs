using System.Collections.Generic;
using NUnit.Framework;
using VrAction.Core.Model;
using VrAction.Core.StageGeneration;

namespace VrAction.Core.Tests
{
    public class DeterminismTests
    {
        [Test]
        public void SameSpaceAndSeed_IdenticalStage_100Runs()
        {
            var space = TestData.Desk();
            var req = new GenerationRequest(2024UL, StageType.Exploration, DifficultyMode.CheckpointRespawn);
            var first = new ExplorationGenerator().Generate(space, req);
            Assert.IsTrue(first.IsSuccess);
            string expected = first.Stage.Fingerprint();
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(expected, new ExplorationGenerator().Generate(TestData.Desk(), req).Stage.Fingerprint());
        }

        [Test]
        public void DifferentSeeds_ProduceVariety()
        {
            var space = TestData.Desk();
            var set = new HashSet<string>();
            for (ulong s = 1; s <= 20; s++)
                set.Add(new ExplorationGenerator().Generate(space, new GenerationRequest(s, StageType.Exploration, DifficultyMode.NoDeath)).Stage.Fingerprint());
            Assert.Greater(set.Count, 5);
        }
    }

    public class ReachabilityTests
    {
        [Test]
        public void GoalReachable_AcrossSeeds_OnDesk()
        {
            var space = TestData.Desk();
            for (ulong s = 0; s < 200; s++)
            {
                var o = new ExplorationGenerator().Generate(space, new GenerationRequest(s, StageType.Exploration, DifficultyMode.NoDeath));
                Assert.IsTrue(o.IsSuccess, "seed " + s + " failed: " + o.Failure);
                AssertValidPath(space, o.Stage);
            }
        }

        public static void AssertValidPath(SpatialData space, Stage st)
        {
            Assert.AreNotEqual(st.Start, st.Goal);
            Assert.IsTrue(space.IsWalkable(st.Start.X, st.Start.Z));
            Assert.IsTrue(space.IsWalkable(st.Goal.X, st.Goal.Z));
            Assert.AreEqual(st.Start, st.Path[0]);
            Assert.AreEqual(st.Goal, st.Path[st.Path.Count - 1]);
            for (int i = 1; i < st.Path.Count; i++)
                Assert.IsTrue(Pathing.CanStep(space, st.Path[i - 1], st.Path[i]), "bad step " + st.Path[i - 1] + "->" + st.Path[i]);
            Assert.GreaterOrEqual(st.Path.Count, SpatialRules.MinPathCells);
        }
    }

    public class StageValidatorTests
    {
        [Test]
        public void ValidStage_Passes()
        {
            var space = TestData.Desk();
            var st = new ExplorationGenerator().Generate(space, new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath)).Stage;
            Assert.IsTrue(new StageValidator().Validate(st, space).IsValid);
        }

        [Test]
        public void UnreachableGoal_Rejected()
        {
            // two ground areas separated by a wall column
            int w = 25, d = 8;
            var kinds = new CellKind[w * d];
            for (int z = 0; z < d; z++)
                for (int x = 0; x < w; x++) kinds[z * w + x] = x == 12 ? CellKind.Wall : CellKind.Ground;
            var space = new SpatialData(PlayMode.Tabletop, 50, w, d, 0, 0, kinds, new int[w * d], new bool[w * d]);
            var req = new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath);
            var path = new List<Cell> { new Cell(1, 1), new Cell(22, 1) };
            var st = new Stage(req, new Cell(1, 1), new Cell(22, 1), path, new StageElement[0], ObjectiveKind.ReachGoal);
            var rep = new StageValidator().Validate(st, space);
            Assert.IsFalse(rep.IsValid);
        }

        [Test]
        public void TooSmallArea_Rejected()
        {
            var space = TestData.Flat(5, 5);
            var req = new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath);
            var st = new Stage(req, new Cell(0, 0), new Cell(4, 4), Pathing.ShortestPath(space, new Cell(0, 0), new Cell(4, 4)),
                new StageElement[0], ObjectiveKind.ReachGoal);
            Assert.IsFalse(new StageValidator().Validate(st, space).IsValid);
        }

        [Test]
        public void Generator_OnTinyGrid_FailsWithTooSmall()
        {
            var o = new ExplorationGenerator().Generate(TestData.Flat(5, 5), new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath));
            Assert.IsFalse(o.IsSuccess);
            Assert.AreEqual(GenerationFailure.TooSmall, o.Failure);
        }
    }
}
