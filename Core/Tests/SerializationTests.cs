using System;
using NUnit.Framework;
using VrAction.Core.Model;
using VrAction.Core.Serialization;

namespace VrAction.Core.Tests
{
    public class SerializationTests
    {
        static SpatialData Sample()
        {
            var kinds = new[] { CellKind.Ground, CellKind.Ground, CellKind.Wall, CellKind.Platform, CellKind.Void, CellKind.Obstacle };
            var heights = new[] { 0, 0, 0, 300, 0, 100 };
            var hazards = new[] { false, true, false, false, false, false };
            return new SpatialData(SpaceMode.Room, 50, 3, 2, -100, 250, kinds, heights, hazards);
        }

        [Test]
        public void SpatialData_RoundTrip_PreservesAllFields()
        {
            var a = Sample();
            var b = SpatialDataJson.Deserialize(SpatialDataJson.Serialize(a));
            Assert.AreEqual(SpatialDataJson.Serialize(a), SpatialDataJson.Serialize(b));
            Assert.AreEqual(a.Mode, b.Mode);
            Assert.AreEqual(CellKind.Platform, b.KindAt(0, 1));
            Assert.AreEqual(300, b.HeightAt(0, 1));
            Assert.IsTrue(b.IsHazard(1, 0));
            Assert.AreEqual(-100, b.OriginXMm);
        }

        [Test]
        public void Hash_IsStable_AndChangesWithContent()
        {
            var a = Sample();
            Assert.AreEqual(SpatialDataJson.Hash(a), SpatialDataJson.Hash(Sample()));
            var kinds = a.KindsCopy(); kinds[0] = CellKind.Wall;
            var c = new SpatialData(a.Mode, 50, 3, 2, -100, 250, kinds, a.HeightsCopy(), a.HazardsCopy());
            Assert.AreNotEqual(SpatialDataJson.Hash(a), SpatialDataJson.Hash(c));
            StringAssert.StartsWith("sha256:", SpatialDataJson.Hash(a));
        }

        [Test]
        public void SaveFile_RoundTrip()
        {
            var save = new SaveFile(Sample(), new[] { new ClearedStage(42, StageType.Exploration) }, 12, DifficultyMode.NoDeath);
            var back = SaveFileJson.Deserialize(SaveFileJson.Serialize(save));
            Assert.AreEqual(1, back.ClearedStages.Count);
            Assert.AreEqual(42UL, back.ClearedStages[0].Seed);
            Assert.AreEqual(12, back.CharacterHeightCm);
            Assert.AreEqual(DifficultyMode.NoDeath, back.Difficulty);
            Assert.AreEqual(SpatialDataJson.Hash(save.SpatialData), SpatialDataJson.Hash(back.SpatialData));
        }

        [Test]
        public void SaveFile_UnknownVersion_Rejected()
        {
            var json = SaveFileJson.Serialize(new SaveFile(Sample(), new ClearedStage[0], 10, DifficultyMode.CheckpointRespawn));
            var bad = json.Replace("\"version\":1", "\"version\":99");
            var ex = Assert.Throws<FormatException>(() => SaveFileJson.Deserialize(bad));
            StringAssert.Contains("version", ex.Message);
        }

        [Test]
        public void ShareFile_RoundTrip_AndHashMismatchRejected()
        {
            var req = new GenerationRequest(123456789UL, StageType.Exploration, DifficultyMode.NoDeath, 10);
            var json = ShareFileJson.Export(req, Sample(), consent: true);
            var back = ShareFileJson.Import(json);
            Assert.AreEqual(123456789UL, back.Request.Seed);
            Assert.AreEqual(StageType.Exploration, back.Request.StageType);

            // tamper with the spatial data without updating the hash
            var tampered = json.Replace("\"kinds\":\"1124", "\"kinds\":\"2224");
            Assert.AreNotEqual(json, tampered);
            var ex = Assert.Throws<FormatException>(() => ShareFileJson.Import(tampered));
            StringAssert.Contains("hash", ex.Message);
        }

        [Test]
        public void ShareFile_Export_RequiresConsent()
        {
            var req = new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath);
            Assert.Throws<InvalidOperationException>(() => ShareFileJson.Export(req, Sample(), consent: false));
        }

        [Test]
        public void ShareFile_UnknownVersion_Rejected()
        {
            var req = new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath);
            var bad = ShareFileJson.Export(req, Sample(), true).Replace("\"version\":1", "\"version\":7");
            Assert.Throws<FormatException>(() => ShareFileJson.Import(bad));
        }
    }
}
