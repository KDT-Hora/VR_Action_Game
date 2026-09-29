using NUnit.Framework;
using VrAction.Core.Character;
using VrAction.Core.Model;

namespace VrAction.Core.Tests
{
    public class ScaleSettingsTests
    {
        [Test] public void Default_Is10cm() => Assert.AreEqual(10, new ScaleSettings().CharacterHeightCm);

        [TestCase(1, 5)]
        [TestCase(5, 5)]
        [TestCase(12, 12)]
        [TestCase(20, 20)]
        [TestCase(99, 20)]
        public void Clamps_To5to20cm(int input, int expected)
            => Assert.AreEqual(expected, new ScaleSettings(input).CharacterHeightCm);

        [Test] public void Height_InMillimetres() => Assert.AreEqual(150, new ScaleSettings(15).CharacterHeightMm);
    }

    public class PostureTests
    {
        [Test]
        public void Standing_HasNoOffset_SeatedIsLower()
        {
            var p = new PostureState(PostureMode.Standing, 1600, 1150);
            Assert.AreEqual(0, p.ViewOffsetMm);
            p.SetMode(PostureMode.Seated);
            Assert.AreEqual(-450, p.ViewOffsetMm);
        }

        [Test]
        public void Offset_SurvivesRecenter()
        {
            var p = new PostureState(PostureMode.Seated, 1600, 1150);
            p.Recenter();
            Assert.AreEqual(PostureMode.Seated, p.Mode);
            Assert.AreEqual(-450, p.ViewOffsetMm);
        }

        [Test]
        public void SwitchingMidSession_UpdatesOffset()
        {
            var p = new PostureState(PostureMode.Seated, 1600, 1150);
            p.SetMode(PostureMode.Standing);
            Assert.AreEqual(0, p.ViewOffsetMm);
        }
    }

    public class CharacterMotorTests
    {
        [Test]
        public void Move_ChangesHorizontalPosition_ProportionalToDt()
        {
            var m = CharacterMotor.ForHeight(0.10f);
            m.Step(0.5f, 1f, 0f, false, 0f);
            Assert.Greater(m.X, 0f);
            Assert.AreEqual(0f, m.Z, 1e-6);
            float x1 = m.X;
            m.Step(0.5f, 1f, 0f, false, 0f);
            Assert.AreEqual(2 * x1, m.X, 1e-4);
        }

        [Test]
        public void Jump_OnlyWhenGrounded()
        {
            var m = CharacterMotor.ForHeight(0.10f);
            m.Step(0.016f, 0, 0, true, 0f);
            Assert.IsFalse(m.IsGrounded);
            float vy = m.VelocityY;
            Assert.Greater(vy, 0f);
            m.Step(0.016f, 0, 0, true, 0f); // pressed again in mid-air: ignored
            Assert.Less(m.VelocityY, vy);
        }

        [Test]
        public void Jump_LandsBackOnGround()
        {
            var m = CharacterMotor.ForHeight(0.10f);
            m.Step(0.016f, 0, 0, true, 0f);
            float apex = 0f;
            for (int i = 0; i < 200; i++) { m.Step(0.016f, 0, 0, false, 0f); if (m.Y > apex) apex = m.Y; }
            Assert.IsTrue(m.IsGrounded);
            Assert.AreEqual(0f, m.Y, 1e-6);
            Assert.That(apex, Is.InRange(0.05f, 0.2f)); // roughly 0.5x-2x of a 10cm body
        }

        [Test]
        public void Gravity_KeepsCharacterOnRaisedGround()
        {
            var m = CharacterMotor.ForHeight(0.10f);
            m.Step(0.016f, 0, 0, false, 0.5f);
            for (int i = 0; i < 10; i++) m.Step(0.016f, 0, 0, false, 0.5f);
            Assert.AreEqual(0.5f, m.Y, 1e-6);
            Assert.IsTrue(m.IsGrounded);
        }
    }
}
