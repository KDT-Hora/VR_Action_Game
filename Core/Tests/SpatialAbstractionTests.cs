using NUnit.Framework;
using VrAction.Core.Fixtures;
using VrAction.Core.Model;
using VrAction.Core.Serialization;
using VrAction.Core.SpatialAbstraction;

namespace VrAction.Core.Tests
{
    public class SpatialAbstractorTests
    {
        [Test]
        public void DeskFixture_ProducesExpectedGrid()
        {
            var d = TestData.Desk();
            Assert.AreEqual(SpaceMode.Tabletop, d.Mode);
            Assert.AreEqual(50, d.CellSizeMm);
            Assert.AreEqual(24, d.Width);
            Assert.AreEqual(12, d.Depth);
            Assert.AreEqual(0, d.OriginXMm);
            Assert.AreEqual(0, d.OriginZMm);

            int obstacles = 0, ground = 0, voids = 0;
            for (int z = 0; z < d.Depth; z++)
                for (int x = 0; x < d.Width; x++)
                {
                    switch (d.KindAt(x, z))
                    {
                        case CellKind.Obstacle: obstacles++; break;
                        case CellKind.Ground: ground++; break;
                        case CellKind.Void: voids++; break;
                    }
                }
            Assert.AreEqual(1, obstacles, "the mug is one obstacle cell");
            Assert.AreEqual(0, voids);
            Assert.AreEqual(24 * 12 - 1, ground);
            Assert.AreEqual(CellKind.Obstacle, d.KindAt(18, 6));
            Assert.AreEqual(80, d.HeightAt(18, 6), "obstacle height is relative to the table top");
        }

        [Test]
        public void SameInput_IdenticalOutput_100Runs()
        {
            string expected = SpatialDataJson.Serialize(TestData.Desk());
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(expected, SpatialDataJson.Serialize(TestData.Desk()));
        }
    }

    public class TabletopPlaySpaceTests
    {
        [Test]
        public void EdgeCells_AreHazards_CenterIsNot()
        {
            var ps = TabletopPlaySpace.From(TestData.Desk());
            Assert.AreEqual(2 * (24 + 12) - 4, ps.HazardCells.Count);
            Assert.IsTrue(ps.IsHazard(0, 0));
            Assert.IsTrue(ps.IsHazard(23, 5));
            Assert.IsFalse(ps.IsHazard(12, 6));
        }

        [Test]
        public void Bounds_MatchTable_AndDistanceIsMeasured()
        {
            var ps = TabletopPlaySpace.From(TestData.Desk());
            Assert.AreEqual(0, ps.MinXMm);
            Assert.AreEqual(1200, ps.MaxXMm);
            Assert.AreEqual(0, ps.MinZMm);
            Assert.AreEqual(600, ps.MaxZMm);
            Assert.AreEqual(300, ps.DistanceToBoundaryMm(600, 300));
            Assert.AreEqual(100, ps.DistanceToBoundaryMm(100, 300));
        }
    }

    public class ScanFailureTests
    {
        [Test]
        public void EmptyScan_YieldsTypedError()
        {
            var r = new SpatialAbstractor().Abstract(ScanFixtures.Empty());
            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(ScanError.NoSurfaces, r.Error);
        }

        [Test]
        public void TinyTable_IsTooSmall()
        {
            var r = new SpatialAbstractor().Abstract(ScanFixtures.TinyTable());
            Assert.IsFalse(r.IsSuccess);
            Assert.AreEqual(ScanError.TooSmall, r.Error);
        }

        [Test]
        public void FloorOnly_NoTable_ForTabletop()
        {
            var floorOnly = new ScanResult("f", "t", new[] { new Surface(SurfaceKind.Floor, 0, 0, 3000, 3000, 0, 0) });
            var r = new SpatialAbstractor().Abstract(floorOnly);
            Assert.AreEqual(ScanError.NoTable, r.Error);
        }

        [Test]
        public void Room_WithoutFloor_NoFloor()
        {
            var noFloor = new ScanResult("f", "t", new[] { new Surface(SurfaceKind.Wall, 0, 0, 3000, 100, 0, 2400) });
            Assert.AreEqual(ScanError.NoFloor, new RoomAbstractor().Abstract(noFloor).Error);
        }
    }
}
