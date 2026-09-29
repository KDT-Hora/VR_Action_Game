using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VrAction.Core.Model;
using VrAction.Game.Game;

namespace VrAction.Game.Tests
{
    public class BootstrapTests
    {
        static GameBootstrap Make(GameBootstrap.Source source, bool autoStart = true)
        {
            var go = new GameObject("bootstrap");
            var b = go.AddComponent<GameBootstrap>();
            b.SourceMode = source;
            b.AutoStart = false;
            b.StartFlow(chooseDifficultyFirst: false);
            return b;
        }

        [Test]
        public void Tabletop_StartsPlayableStage()
        {
            var b = Make(GameBootstrap.Source.Tabletop);
            Assert.IsNotNull(b.Flow.Stage);
            Assert.AreEqual(SpaceMode.Tabletop, b.Flow.Space.Mode);
            Assert.AreEqual(StageFlow.FlowState.Playing, b.Flow.State);
        }

        [Test]
        public void Room_StartsPlayableStage()
        {
            var b = Make(GameBootstrap.Source.Room);
            Assert.AreEqual(SpaceMode.Room, b.Flow.Space.Mode);
            Assert.AreEqual(StageFlow.FlowState.Playing, b.Flow.State);
        }

        [Test]
        public void Sandbox_HasFlatFixedFloor()
        {
            var b = Make(GameBootstrap.Source.Sandbox);
            Assert.AreEqual(StageFlow.FlowState.Playing, b.Flow.State);
        }

        [Test]
        public void BuildSettings_ContainThe3Scenes()
        {
            Assert.GreaterOrEqual(SceneManager.sceneCountInBuildSettings, 3);
        }

        [Test]
        public void AfterClear_MenuIsShownAndProgressIsSaved()
        {
            var b = Make(GameBootstrap.Source.Tabletop);
            b.Flow.Hero.PlaceAtCell(b.Flow.Stage.Goal);
            b.Flow.Tick(0.016f);
            Assert.IsTrue(b.NextMenu.Visible);
            Assert.IsNotNull(b.Store.Load());
            Assert.AreEqual(1, b.Store.Load().ClearedStages.Count);
            b.Store.Delete();
        }
    }
}
