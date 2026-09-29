using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VrAction.Core.Character;
using VrAction.Core.Fixtures;
using VrAction.Core.Model;
using VrAction.Core.Safety;
using VrAction.Core.Serialization;
using VrAction.Core.StageGeneration;

namespace VrAction.Core.Tests
{
    public class PlacementTests
    {
        static IEnumerable<SpatialData> Spaces() { yield return TestData.Desk(); yield return TestData.Room(); }

        [Test]
        public void Elements_NeverSitOnPath_OrNearStart_AndAreOnWalkableCells()
        {
            foreach (var space in Spaces())
                for (ulong s = 0; s < 100; s++)
                {
                    var o = new StageGenerator().Generate(space, new GenerationRequest(s, StageType.Exploration, DifficultyMode.CheckpointRespawn));
                    Assert.IsTrue(o.IsSuccess);
                    var st = o.Stage;
                    var path = new HashSet<Cell>(st.Path);
                    var used = new HashSet<Cell>();
                    foreach (var e in st.Elements)
                    {
                        Assert.IsTrue(space.IsWalkable(e.Cell.X, e.Cell.Z), e.Kind + " on non-walkable " + e.Cell);
                        if (e.Kind != ElementKind.Checkpoint)
                        {
                            Assert.IsFalse(path.Contains(e.Cell), e.Kind + " blocks the path at " + e.Cell);
                            Assert.IsTrue(used.Add(e.Cell), "two elements on " + e.Cell);
                        }
                        if (e.Kind == ElementKind.Enemy)
                            Assert.GreaterOrEqual(System.Math.Abs(e.Cell.X - st.Start.X) + System.Math.Abs(e.Cell.Z - st.Start.Z), 4);
                    }
                    ReachabilityTests.AssertValidPath(space, st);
                }
        }

        [Test]
        public void Exploration_HasEnemiesItemsAndACheckpoint()
        {
            var st = new StageGenerator().Generate(TestData.Desk(), new GenerationRequest(3, StageType.Exploration, DifficultyMode.CheckpointRespawn)).Stage;
            Assert.Greater(st.Elements.Count(e => e.Kind == ElementKind.Enemy), 0);
            Assert.Greater(st.Elements.Count(e => e.Kind == ElementKind.Item), 0);
            Assert.GreaterOrEqual(st.Elements.Count(e => e.Kind == ElementKind.Checkpoint), 1);
            Assert.AreEqual(ObjectiveKind.ReachGoal, st.Objective);
        }

        [Test]
        public void Placement_IsDeterministic()
        {
            var req = new GenerationRequest(77, StageType.Exploration, DifficultyMode.NoDeath);
            string a = new StageGenerator().Generate(TestData.Room(), req).Stage.Fingerprint();
            Assert.AreEqual(a, new StageGenerator().Generate(TestData.Room(), req).Stage.Fingerprint());
        }
    }

    public class StageTypeTests
    {
        [TestCase(StageType.Treasure, ObjectiveKind.OpenChest, ElementKind.Chest)]
        [TestCase(StageType.Combat, ObjectiveKind.DefeatAll, ElementKind.Enemy)]
        [TestCase(StageType.Defense, ObjectiveKind.DefendPoint, ElementKind.Gimmick)]
        [TestCase(StageType.Boss, ObjectiveKind.DefeatBoss, ElementKind.Boss)]
        [TestCase(StageType.Exploration, ObjectiveKind.ReachGoal, ElementKind.Item)]
        public void EachType_IsReachable_WithItsObjective(StageType type, ObjectiveKind objective, ElementKind mustHave)
        {
            foreach (var space in new[] { TestData.Desk(), TestData.Room() })
                for (ulong s = 0; s < 30; s++)
                {
                    var o = new StageGenerator().Generate(space, new GenerationRequest(s, type, DifficultyMode.NoDeath));
                    Assert.IsTrue(o.IsSuccess, type + " seed " + s);
                    Assert.AreEqual(objective, o.Stage.Objective);
                    Assert.IsTrue(o.Stage.Elements.Any(e => e.Kind == mustHave), type + " needs " + mustHave);
                    ReachabilityTests.AssertValidPath(space, o.Stage);
                }
        }

        [Test]
        public void Boss_SitsOnGoal_Chest_SitsOnGoal()
        {
            var space = TestData.Room();
            var boss = new StageGenerator().Generate(space, new GenerationRequest(1, StageType.Boss, DifficultyMode.NoDeath)).Stage;
            Assert.AreEqual(boss.Goal, boss.Elements.Single(e => e.Kind == ElementKind.Boss).Cell);
            var chest = new StageGenerator().Generate(space, new GenerationRequest(1, StageType.Treasure, DifficultyMode.NoDeath)).Stage;
            Assert.AreEqual(chest.Goal, chest.Elements.Single(e => e.Kind == ElementKind.Chest).Cell);
        }

        [Test]
        public void Combat_HasMoreEnemiesThanExploration()
        {
            var space = TestData.Room();
            var c = new StageGenerator().Generate(space, new GenerationRequest(9, StageType.Combat, DifficultyMode.NoDeath)).Stage;
            var e = new StageGenerator().Generate(space, new GenerationRequest(9, StageType.Exploration, DifficultyMode.NoDeath)).Stage;
            Assert.Greater(c.Elements.Count(x => x.Kind == ElementKind.Enemy), e.Elements.Count(x => x.Kind == ElementKind.Enemy));
        }
    }

    public class HealthSystemTests
    {
        [Test]
        public void CheckpointMode_DamageThenDown_ThenRespawnAtCheckpoint()
        {
            var h = new HealthSystem(DifficultyMode.CheckpointRespawn, 5, new Cell(1, 1));
            h.ApplyDamage(2);
            Assert.AreEqual(3, h.Hp);
            h.ReachCheckpoint(new Cell(7, 3));
            h.ApplyDamage(10);
            Assert.IsTrue(h.IsDown);
            Assert.AreEqual(0, h.Hp);
            var at = h.Respawn();
            Assert.AreEqual(new Cell(7, 3), at);
            Assert.AreEqual(5, h.Hp);
            Assert.IsFalse(h.IsDown);
        }

        [Test]
        public void CheckpointMode_RespawnWithoutCheckpoint_GoesToStart()
        {
            var h = new HealthSystem(DifficultyMode.CheckpointRespawn, 3, new Cell(2, 2));
            h.ApplyDamage(3);
            Assert.AreEqual(new Cell(2, 2), h.Respawn());
        }

        [Test]
        public void NoDeathMode_IgnoresDamage_NeverDown()
        {
            var h = new HealthSystem(DifficultyMode.NoDeath, 5, new Cell(0, 0));
            h.ApplyDamage(100);
            Assert.AreEqual(5, h.Hp);
            Assert.IsFalse(h.IsDown);
        }

        [Test]
        public void Fall_ReturnsToLastSafeCell_InBothModes_AndOnlyCostsHpWhenCheckpointMode()
        {
            var a = new HealthSystem(DifficultyMode.NoDeath, 5, new Cell(0, 0));
            a.NotifySafeCell(new Cell(4, 4));
            Assert.AreEqual(new Cell(4, 4), a.OnFall());
            Assert.AreEqual(5, a.Hp);

            var b = new HealthSystem(DifficultyMode.CheckpointRespawn, 5, new Cell(0, 0));
            b.NotifySafeCell(new Cell(4, 4));
            Assert.AreEqual(new Cell(4, 4), b.OnFall());
            Assert.AreEqual(4, b.Hp);
        }

        [Test]
        public void DownCharacter_CannotBeDamagedFurther()
        {
            var h = new HealthSystem(DifficultyMode.CheckpointRespawn, 2, new Cell(0, 0));
            h.ApplyDamage(5);
            h.ApplyDamage(5);
            Assert.AreEqual(0, h.Hp);
        }
    }

    public class EnemyStateTests
    {
        [Test]
        public void Idle_Chase_Attack_Transitions()
        {
            var b = new EnemyBrain(sightRange: 6f, attackRange: 1.5f, attackCooldown: 1f);
            Assert.AreEqual(EnemyState.Idle, b.Update(0.1f, 10f));
            Assert.AreEqual(EnemyState.Chase, b.Update(0.1f, 5f));
            Assert.AreEqual(EnemyState.Attack, b.Update(0.1f, 1f));
            Assert.AreEqual(EnemyState.Idle, b.Update(0.1f, 20f));
        }

        [Test]
        public void Attack_FiresOncePerCooldown()
        {
            var b = new EnemyBrain(6f, 1.5f, 1f);
            b.Update(0.1f, 1f);
            Assert.IsFalse(b.ConsumeAttack());
            int hits = 0;
            for (int i = 0; i < 30; i++) { b.Update(0.1f, 1f); if (b.ConsumeAttack()) hits++; }
            Assert.That(hits, Is.InRange(2, 3));
        }
    }

    public class SafetyMonitorTests
    {
        [Test]
        public void TurnsOnWithin30cm_StaysOnUntil40cm()
        {
            var m = new SafetyMonitor();
            Assert.IsFalse(m.Update(1000));
            Assert.IsTrue(m.Update(300));
            Assert.IsTrue(m.Update(350));
            Assert.IsFalse(m.Update(400));
        }

        [Test]
        public void ManualToggle_OverridesAndRestores()
        {
            var m = new SafetyMonitor();
            m.ToggleManual();
            Assert.IsTrue(m.Update(2000));
            m.ToggleManual();
            Assert.IsFalse(m.Update(2000));
        }

        [Test]
        public void ManualOn_DoesNotBlockAutomaticSafety()
        {
            var m = new SafetyMonitor();
            m.ToggleManual(); m.ToggleManual();
            Assert.IsTrue(m.Update(100));
        }
    }

    public class ImmutabilityTests
    {
        [Test]
        public void ScanResult_SurfaceList_CannotBeModified()
        {
            var scan = ScanFixtures.Desk();
            var list = (IList<Surface>)scan.Surfaces;
            Assert.Throws<System.NotSupportedException>(() => list.Add(scan.Surfaces[0]));
        }

        [Test]
        public void SpatialData_CopiesInputAndOutputArrays()
        {
            var kinds = new[] { CellKind.Ground, CellKind.Ground };
            var d = new SpatialData(PlayMode.Tabletop, 50, 2, 1, 0, 0, kinds, new[] { 0, 0 }, new[] { false, false });
            kinds[0] = CellKind.Wall;
            Assert.AreEqual(CellKind.Ground, d.KindAt(0, 0));
            var copy = d.KindsCopy(); copy[1] = CellKind.Wall;
            Assert.AreEqual(CellKind.Ground, d.KindAt(1, 0));
        }

        [Test]
        public void Stage_PathAndElements_CannotBeModified()
        {
            var st = new StageGenerator().Generate(TestData.Desk(), new GenerationRequest(1, StageType.Exploration, DifficultyMode.NoDeath)).Stage;
            Assert.Throws<System.NotSupportedException>(() => ((IList<Cell>)st.Path).Add(new Cell(0, 0)));
            Assert.Throws<System.NotSupportedException>(() => ((IList<StageElement>)st.Elements).Clear());
        }

        [Test]
        public void Generation_DoesNotChangeTheSpatialData()
        {
            var space = TestData.Room();
            string before = SpatialDataJson.Serialize(space);
            for (ulong s = 0; s < 10; s++) new StageGenerator().Generate(space, new GenerationRequest(s, StageType.Boss, DifficultyMode.NoDeath));
            Assert.AreEqual(before, SpatialDataJson.Serialize(space));
        }
    }

    public class ShareRoundTripTests
    {
        [Test]
        public void ImportedShareFile_ReproducesIdenticalStage()
        {
            var space = TestData.Room();
            foreach (StageType type in System.Enum.GetValues(typeof(StageType)))
            {
                var req = new GenerationRequest(123456789UL, type, DifficultyMode.CheckpointRespawn, 12);
                var original = new StageGenerator().Generate(space, req).Stage;
                var back = ShareFileJson.Import(ShareFileJson.Export(req, space, consent: true));
                var again = new StageGenerator().Generate(back.SpatialData, back.Request).Stage;
                Assert.AreEqual(original.Fingerprint(), again.Fingerprint(), type.ToString());
            }
        }
    }
}
