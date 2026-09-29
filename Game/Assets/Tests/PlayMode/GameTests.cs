using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VrAction.Core.Character;
using VrAction.Core.Fixtures;
using VrAction.Core.Model;
using VrAction.Core.Serialization;
using VrAction.Game.Character;
using VrAction.Game.Game;
using VrAction.Game.Presentation;
using VrAction.Game.Save;
using VrAction.Game.Scan;

namespace VrAction.Game.Tests
{
    static class H
    {
        public static SpatialData Desk() => ModeSelect.LoadSpace(SpaceMode.Tabletop, new FixtureScanProvider()).Data;
        public static SpatialData Room() => ModeSelect.LoadSpace(SpaceMode.Room, new FixtureScanProvider()).Data;

        public static StageFlow NewFlow(SpaceMode mode = SpaceMode.Tabletop, StageType type = StageType.Exploration,
                                        DifficultyMode diff = DifficultyMode.CheckpointRespawn)
        {
            var go = new GameObject("flow");
            var flow = go.AddComponent<StageFlow>();
            var space = mode == SpaceMode.Tabletop ? Desk() : Room();
            Assert.IsTrue(flow.Begin(space, new GenerationRequest(1, type, diff), new ScaleSettings(10), go.transform));
            return flow;
        }
    }

    public class FixtureScanTests
    {
        [Test]
        public void FixtureProvider_ReturnsDeskAndRoom()
        {
            var p = new FixtureScanProvider();
            Assert.AreEqual("fixture-desk", p.ScanAsync(ScanMode.Tabletop).Result.Id);
            Assert.AreEqual("fixture-room", p.ScanAsync(ScanMode.Room).Result.Id);
        }
    }

    public class CharacterSmokeTests
    {
        static SmallHero Hero(out GameObject root, SpatialData space, Cell start)
        {
            root = new GameObject("stage");
            return SmallHero.Create(root.transform, space, new ScaleSettings(10), start, DifficultyMode.CheckpointRespawn);
        }

        static SpatialData Flat()
        {
            int w = 20, d = 20;
            var kinds = new CellKind[w * d];
            for (int i = 0; i < kinds.Length; i++) kinds[i] = CellKind.Ground;
            kinds[10 * w + 15] = CellKind.Wall;
            return new SpatialData(SpaceMode.Tabletop, 50, w, d, 0, 0, kinds, new int[w * d], new bool[w * d]);
        }

        [Test]
        public void Moves_FromInput()
        {
            var h = Hero(out _, Flat(), new Cell(5, 10));
            float x0 = h.Motor.X;
            h.MoveInput = new Vector2(1, 0);
            for (int i = 0; i < 20; i++) h.Step(0.016f);
            Assert.Greater(h.Motor.X, x0);
        }

        [Test]
        public void Jumps_AndLands()
        {
            var h = Hero(out _, Flat(), new Cell(5, 10));
            h.JumpInput = true; h.Step(0.016f); h.JumpInput = false;
            Assert.Greater(h.Motor.Y, 0f);
            for (int i = 0; i < 100; i++) h.Step(0.016f);
            Assert.IsTrue(h.Motor.IsGrounded);
        }

        [Test]
        public void Walls_BlockMovement()
        {
            var h = Hero(out _, Flat(), new Cell(13, 10));
            h.MoveInput = new Vector2(1, 0);
            for (int i = 0; i < 200; i++) h.Step(0.016f);
            Assert.Less(h.Motor.X * 1000f, 15 * 50f, "must not enter the wall cell at x=15");
        }

        [Test]
        public void Paused_HeroDoesNotMove()
        {
            var h = Hero(out _, Flat(), new Cell(5, 10));
            h.Paused = true; h.MoveInput = new Vector2(1, 0);
            float x0 = h.Motor.X;
            for (int i = 0; i < 20; i++) h.Step(0.016f);
            Assert.AreEqual(x0, h.Motor.X);
        }
    }

    public class PeekRigTests
    {
        [Test]
        public void HeadMovement_DoesNotMoveCharacter_AndRigKeepsRealScale()
        {
            var rig = PeekRig.Create();
            var stage = new GameObject("stage");
            var hero = SmallHero.Create(stage.transform, H.Desk(), new ScaleSettings(10), new Cell(5, 5), DifficultyMode.NoDeath);
            var before = hero.transform.position;
            rig.HeadCamera.transform.position += new Vector3(0.3f, 0.2f, -0.4f);
            hero.Step(0.016f);
            Assert.AreEqual(before, hero.transform.position);
            Assert.AreEqual(Vector3.one, rig.transform.localScale);
        }
    }

    public class PostureTests
    {
        [Test]
        public void Seated_LowersRig_Standing_Restores_Recenter_Keeps()
        {
            var rig = PeekRig.Create();
            var p = rig.gameObject.AddComponent<PostureSetup>();
            p.Apply(PostureMode.Standing);
            float standingY = rig.transform.position.y;
            p.Apply(PostureMode.Seated);
            Assert.Less(rig.transform.position.y, standingY);
            p.Recenter();
            Assert.AreEqual(PostureMode.Seated, p.Mode);
            Assert.Less(rig.transform.position.y, standingY);
        }
    }

    public class StageFlowTests
    {
        [Test]
        public void GoalTrigger_RaisesClearedExactlyOnce()
        {
            var flow = H.NewFlow();
            int count = 0;
            flow.Cleared += _ => count++;
            flow.Tick(0.016f);
            Assert.AreEqual(0, count);
            flow.Hero.PlaceAtCell(flow.Stage.Goal);
            flow.Tick(0.016f); flow.Tick(0.016f); flow.Tick(0.016f);
            Assert.AreEqual(1, count);
            Assert.AreEqual(StageFlow.FlowState.Cleared, flow.State);
        }

        [Test]
        public void Combat_Requires_DefeatingAllEnemies()
        {
            var flow = H.NewFlow(type: StageType.Combat);
            flow.Hero.PlaceAtCell(flow.Stage.Goal);
            flow.Tick(0.016f);
            Assert.AreEqual(StageFlow.FlowState.Playing, flow.State, "reaching the goal must not clear a Combat stage");
            foreach (var e in flow.Enemies.ToList()) e.Hit(999);
            flow.Tick(0.016f);
            Assert.AreEqual(StageFlow.FlowState.Cleared, flow.State);
        }
    }

    public class EdgeCaseTests
    {
        [Test]
        public void FallingOffTheTable_ReturnsToLastSafeCell()
        {
            var flow = H.NewFlow();
            var hero = flow.Hero;
            hero.Step(0.016f); // records the safe start cell
            var safe = hero.CurrentCell;
            hero.Motor.Teleport(-0.5f, 0f, 0.3f); // well outside the table
            for (int i = 0; i < 200; i++) hero.Step(0.016f);
            Assert.AreEqual(safe, hero.CurrentCell);
            Assert.IsTrue(hero.Motor.IsGrounded);
        }

        [Test]
        public void TrackingLoss_PausesAndResumes()
        {
            var flow = H.NewFlow();
            flow.SetTrackingLost(true);
            Assert.IsTrue(flow.IsPaused);
            Assert.IsTrue(flow.Hero.Paused);
            flow.SetTrackingLost(false);
            Assert.IsFalse(flow.IsPaused);
            Assert.IsFalse(flow.Hero.Paused);
        }
    }

    public class ModeSelectTests
    {
        [Test]
        public void ModeChoice_LoadsMatchingPipeline()
        {
            var p = new FixtureScanProvider();
            Assert.AreEqual(SpaceMode.Tabletop, ModeSelect.LoadSpace(SpaceMode.Tabletop, p).Data.Mode);
            Assert.AreEqual(SpaceMode.Room, ModeSelect.LoadSpace(SpaceMode.Room, p).Data.Mode);
        }
    }

    public class CombatTests
    {
        [Test]
        public void Attack_HitsEnemyInFront_OncePerSwing()
        {
            var flow = H.NewFlow();
            var enemy = flow.Enemies.First();
            enemy.PlaceInFrontOf(flow.Hero, 0.06f);
            var combat = flow.Combat;
            int hp = enemy.Hp;
            Assert.AreEqual(1, combat.Attack());
            Assert.AreEqual(hp - 1, enemy.Hp);
            Assert.AreEqual(0, combat.Attack(), "cooldown: second call in the same swing does nothing");
        }

        [Test]
        public void Attack_MissesEnemyOutOfRange()
        {
            var flow = H.NewFlow();
            var enemy = flow.Enemies.First();
            enemy.PlaceInFrontOf(flow.Hero, 0.6f);
            Assert.AreEqual(0, flow.Combat.Attack());
        }

        [Test]
        public void Dodge_GrantsBriefInvulnerability_ThatExpires()
        {
            var flow = H.NewFlow();
            flow.Combat.Dodge();
            Assert.IsTrue(flow.Combat.IsInvulnerable);
            flow.Combat.Tick(1f);
            Assert.IsFalse(flow.Combat.IsInvulnerable);
        }
    }

    public class MenuFlowTests
    {
        [Test]
        public void DifficultySelect_AppliesChosenMode()
        {
            var session = new GameSession();
            var menu = new DifficultySelect(session);
            menu.Choose(DifficultyMode.NoDeath);
            Assert.AreEqual(DifficultyMode.NoDeath, session.Difficulty);
        }

        [Test]
        public void NextStageMenu_AppearsAfterClear_ListsAllTypes_AndStartsChosenType()
        {
            var flow = H.NewFlow();
            var session = new GameSession();
            var menu = new NextStageMenu(flow, session);
            Assert.IsFalse(menu.Visible);
            flow.Hero.PlaceAtCell(flow.Stage.Goal);
            flow.Tick(0.016f);
            Assert.IsTrue(menu.Visible);
            Assert.AreEqual(5, menu.Options.Count);
            menu.Choose(StageType.Boss);
            Assert.IsFalse(menu.Visible);
            Assert.AreEqual(StageType.Boss, flow.Stage.Request.StageType);
            Assert.AreEqual(StageFlow.FlowState.Playing, flow.State);
        }
    }

    public class SaveStoreTests
    {
        [Test]
        public void SaveThenLoad_RestoresWithoutRescan_AndDiscardForcesRescan()
        {
            var store = new SaveStore(System.IO.Path.Combine(Application.temporaryCachePath, "save-test-" + System.Guid.NewGuid() + ".json"));
            var space = H.Room();
            store.Save(new SaveFile(space, new[] { new ClearedStage(5, StageType.Combat) }, 12, DifficultyMode.NoDeath));
            var loaded = store.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(SpatialDataJson.Hash(space), SpatialDataJson.Hash(loaded.SpatialData));
            Assert.AreEqual(12, loaded.CharacterHeightCm);

            var rescan = new RescanFlow(store);
            rescan.DiscardSavedScan();
            Assert.IsNull(store.Load());
            Assert.IsTrue(rescan.NeedsScan);
        }
    }

    public class ShareServiceTests
    {
        [Test]
        public void Export_RequiresConsent_ImportRestoresRequest()
        {
            string dir = Application.temporaryCachePath;
            var svc = new ShareService(dir);
            var req = new GenerationRequest(99, StageType.Treasure, DifficultyMode.NoDeath, 8);
            Assert.Throws<System.InvalidOperationException>(() => svc.Export(req, H.Desk(), consent: false));
            string path = svc.Export(req, H.Desk(), consent: true);
            var back = svc.Import(path);
            Assert.AreEqual(99UL, back.Request.Seed);
            Assert.AreEqual(StageType.Treasure, back.Request.StageType);
        }
    }

    public class ShareConsentTests
    {
        [Test]
        public void ConsentDialog_ExplainsRoomShapeIsShared_AndReportsChoice()
        {
            var panel = MenuPanel.Create(new GameObject("head").transform);
            bool? result = null;
            panel.ShowShareConsent(r => result = r);
            StringAssert.Contains("room", panel.Title.ToLower());
            Assert.AreEqual(2, panel.Labels.Count);
            panel.Select(1);
            Assert.AreEqual(false, result);
            panel.ShowShareConsent(r => result = r);
            panel.Select(0);
            Assert.AreEqual(true, result);
        }
    }

    public class SafetyBehaviourTests
    {
        [Test]
        public void NearBoundary_TurnsPassthroughOn()
        {
            var rig = PeekRig.Create();
            var sm = rig.gameObject.AddComponent<SafetyMonitorBehaviour>();
            sm.SetBoundaryMm(-1000, -1000, 1000, 1000);
            rig.HeadCamera.transform.position = new Vector3(0f, 1.5f, 0f);
            sm.Evaluate();
            Assert.IsFalse(sm.PassthroughOn);
            rig.HeadCamera.transform.position = new Vector3(0.8f, 1.5f, 0f); // 20cm from the 1m boundary
            sm.Evaluate();
            Assert.IsTrue(sm.PassthroughOn);
        }
    }
}
