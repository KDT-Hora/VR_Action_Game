using NUnit.Framework;
using VrAction.Core.Fixtures;
using VrAction.Core.Model;
using VrAction.Core.SpatialAbstraction;

namespace VrAction.Core.Tests
{
    public static class TestData
    {
        public static SpatialData Desk()
        {
            var r = new SpatialAbstractor().Abstract(ScanFixtures.Desk());
            Assert.IsTrue(r.IsSuccess, "desk fixture must abstract: " + r.Error);
            return r.Data;
        }

        public static SpatialData Room()
        {
            var r = new RoomAbstractor().Abstract(ScanFixtures.Room());
            Assert.IsTrue(r.IsSuccess, "room fixture must abstract: " + r.Error);
            return r.Data;
        }

        /// <summary>Flat all-Ground grid for unit tests of small logic.</summary>
        public static SpatialData Flat(int w, int d, PlayMode mode = PlayMode.Tabletop)
        {
            var kinds = new CellKind[w * d];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = CellKind.Ground;
            return new SpatialData(mode, 50, w, d, 0, 0, kinds, new int[w * d], new bool[w * d]);
        }
    }
}
