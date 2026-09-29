using NUnit.Framework;
using VrAction.Core.Model;
using VrAction.Core.SpatialAbstraction;
using VrAction.Core.StageGeneration;

namespace VrAction.Core.Tests
{
    public class SemanticMapperTests
    {
        static Surface S(SurfaceKind k, int top) => new Surface(k, 0, 0, 1000, 1000, 0, top);

        [Test]
        public void Table_BecomesPlatform()
        {
            var m = SemanticMapper.Map(S(SurfaceKind.Table, 720));
            Assert.AreEqual(CellKind.Platform, m.Kind);
            Assert.AreEqual(720, m.HeightMm);
        }

        [Test]
        public void Step_BecomesRaisedPlatform_Cliff()
        {
            var m = SemanticMapper.Map(S(SurfaceKind.Step, 300));
            Assert.AreEqual(CellKind.Platform, m.Kind);
            Assert.AreEqual(300, m.HeightMm);
        }

        [Test]
        public void TallFurniture_BecomesWallRuin()
            => Assert.AreEqual(CellKind.Wall, SemanticMapper.Map(S(SurfaceKind.Furniture, 1800)).Kind);

        [Test]
        public void LowFurniture_BecomesObstacleMountain()
        {
            var m = SemanticMapper.Map(S(SurfaceKind.Furniture, 850));
            Assert.AreEqual(CellKind.Obstacle, m.Kind);
            Assert.AreEqual(850, m.HeightMm);
        }

        [Test]
        public void Wall_StaysWall_CeilingAndOtherIgnored()
        {
            Assert.AreEqual(CellKind.Wall, SemanticMapper.Map(S(SurfaceKind.Wall, 2400)).Kind);
            Assert.IsNull(SemanticMapper.Map(S(SurfaceKind.Ceiling, 2400)));
            Assert.IsNull(SemanticMapper.Map(S(SurfaceKind.Other, 100)));
        }
    }

    public class RoomAbstractionTests
    {
        [Test]
        public void RoomFixture_HasExpectedCellKinds()
        {
            var r = TestData.Room();
            Assert.AreEqual(SpaceMode.Room, r.Mode);
            Assert.AreEqual(80, r.Width);
            Assert.AreEqual(60, r.Depth);
            Assert.AreEqual(CellKind.Wall, r.KindAt(0, 30), "perimeter wall");
            Assert.AreEqual(CellKind.Ground, r.KindAt(40, 30), "open floor");
            Assert.AreEqual(CellKind.Obstacle, r.KindAt(20, 45), "sofa");
            Assert.AreEqual(CellKind.Platform, r.KindAt(60, 10), "desk top");
            Assert.AreEqual(720, r.HeightAt(60, 10));
            Assert.AreEqual(CellKind.Platform, r.KindAt(8, 10), "step");
            Assert.AreEqual(300, r.HeightAt(8, 10));
            Assert.AreEqual(CellKind.Wall, r.KindAt(75, 40), "shelf");
        }

        [Test]
        public void AllPlatformCells_AreReachableFromTheFloor()
        {
            var r = TestData.Room();
            var dist = Pathing.Distances(r, new Cell(40, 30));
            int platforms = 0;
            for (int z = 0; z < r.Depth; z++)
                for (int x = 0; x < r.Width; x++)
                    if (r.KindAt(x, z) == CellKind.Platform)
                    {
                        platforms++;
                        Assert.GreaterOrEqual(dist[z * r.Width + x], 0, "platform cell " + x + "," + z + " must be bridged by a ramp");
                    }
            Assert.Greater(platforms, 0);
        }

        [Test]
        public void PlatformEdges_AreHazards()
        {
            var r = TestData.Room();
            Assert.IsTrue(r.IsHazard(60, 17), "desk edge facing the open floor (drop > step)");
        }

        [Test]
        public void SameInput_SameOutput()
        {
            string a = VrAction.Core.Serialization.SpatialDataJson.Serialize(TestData.Room());
            for (int i = 0; i < 20; i++)
                Assert.AreEqual(a, VrAction.Core.Serialization.SpatialDataJson.Serialize(TestData.Room()));
        }
    }

    public class RoomGenerationTests
    {
        [Test]
        public void Deterministic_And_Reachable_Over200Seeds()
        {
            var space = TestData.Room();
            var g = new ExplorationGenerator();
            string first = null;
            for (ulong s = 0; s < 200; s++)
            {
                var req = new GenerationRequest(s, StageType.Exploration, DifficultyMode.NoDeath);
                var o = g.Generate(space, req);
                Assert.IsTrue(o.IsSuccess, "seed " + s + ": " + o.Failure);
                ReachabilityTests.AssertValidPath(space, o.Stage);
                if (s == 5) first = o.Stage.Fingerprint();
            }
            Assert.AreEqual(first, g.Generate(space, new GenerationRequest(5, StageType.Exploration, DifficultyMode.NoDeath)).Stage.Fingerprint());
        }
    }
}
