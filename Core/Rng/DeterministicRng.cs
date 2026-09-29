using System;

namespace VrAction.Core.Rng
{
    public interface IDeterministicRng
    {
        ulong NextUInt64();
        /// <summary>Integer in [minInclusive, maxExclusive).</summary>
        int Range(int minInclusive, int maxExclusive);
        /// <summary>Independent child generator derived from current seed state and label; does not advance this one.</summary>
        IDeterministicRng Fork(string label);
    }

    /// <summary>SplitMix64. Pure integer arithmetic so results are identical on every platform.</summary>
    public sealed class DeterministicRng : IDeterministicRng
    {
        ulong _state;

        public DeterministicRng(ulong seed) { _state = seed; }

        public ulong NextUInt64()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentException("empty range");
            ulong span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextUInt64() % span));
        }

        public IDeterministicRng Fork(string label)
        {
            // FNV-1a over label, mixed with current state; does not touch _state.
            ulong h = 14695981039346656037UL;
            unchecked
            {
                foreach (char c in label ?? string.Empty) { h ^= c; h *= 1099511628211UL; }
                return new DeterministicRng(new DeterministicRng(h ^ _state).NextUInt64());
            }
        }
    }
}
