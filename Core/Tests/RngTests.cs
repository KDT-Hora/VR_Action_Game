using System.Collections.Generic;
using NUnit.Framework;
using VrAction.Core.Rng;

namespace VrAction.Core.Tests
{
    public class RngTests
    {
        static List<ulong> Take(IDeterministicRng r, int n)
        {
            var l = new List<ulong>();
            for (int i = 0; i < n; i++) l.Add(r.NextUInt64());
            return l;
        }

        [Test]
        public void SameSeed_IdenticalSequence_100Runs()
        {
            var expected = Take(new DeterministicRng(12345), 50);
            for (int run = 0; run < 100; run++)
                CollectionAssert.AreEqual(expected, Take(new DeterministicRng(12345), 50));
        }

        [Test]
        public void DifferentSeeds_Differ()
        {
            CollectionAssert.AreNotEqual(Take(new DeterministicRng(1), 10), Take(new DeterministicRng(2), 10));
        }

        [Test]
        public void Fork_IsStable_AndLabelSensitive()
        {
            var a = new DeterministicRng(7).Fork("enemies");
            var b = new DeterministicRng(7).Fork("enemies");
            var c = new DeterministicRng(7).Fork("items");
            CollectionAssert.AreEqual(Take(a, 10), Take(b, 10));
            CollectionAssert.AreNotEqual(Take(new DeterministicRng(7).Fork("enemies"), 10), Take(c, 10));
        }

        [Test]
        public void Fork_DoesNotAdvanceParent()
        {
            var p = new DeterministicRng(9);
            var first = new DeterministicRng(9).NextUInt64();
            p.Fork("x");
            Assert.AreEqual(first, p.NextUInt64());
        }

        [Test]
        public void Range_StaysInBounds()
        {
            var r = new DeterministicRng(3);
            for (int i = 0; i < 1000; i++)
            {
                int v = r.Range(-5, 5);
                Assert.That(v, Is.InRange(-5, 4));
            }
        }

        [Test]
        public void KnownVector_SplitMix64_Seed0()
        {
            // SplitMix64 reference: seed 0 first output
            Assert.AreEqual(0xE220A8397B1DCDAFUL, new DeterministicRng(0).NextUInt64());
        }
    }
}
