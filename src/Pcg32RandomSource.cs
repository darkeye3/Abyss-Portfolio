// Abyss 프로젝트에서 발췌한 실제 구현입니다. 제출용으로 설명 주석만 정리했습니다.
using System;

namespace Abyss.Core
{
    /// <summary>
    /// PCG32 난수원. 결정론적이고 상태가 16바이트라 저장이 싸다.
    ///
    /// System.Random 을 쓰지 않는 이유:
    ///   - .NET 버전마다 알고리즘이 달라 재현이 보장되지 않는다
    ///   - 내부 상태를 꺼낼 수 없어 세이브 재개가 불가능하다 (S07)
    /// </summary>
    public sealed class Pcg32RandomSource : IRandomSource
    {
        private const ulong Multiplier = 6364136223846793005UL;

        private ulong state;
        private ulong increment;

        public Pcg32RandomSource(ulong seed, ulong sequence = 1UL)
        {
            increment = (sequence << 1) | 1UL;
            state = 0UL;
            NextUInt();
            state = unchecked(state + seed);
            NextUInt();
        }

        public static Pcg32RandomSource FromSnapshot(RandomSnapshot snapshot)
        {
            Pcg32RandomSource source = new Pcg32RandomSource(0UL);
            source.Restore(snapshot);
            return source;
        }

        private uint NextUInt()
        {
            ulong previous = state;
            state = unchecked(previous * Multiplier + increment);
            uint xorShifted = (uint)(((previous >> 18) ^ previous) >> 27);
            int rotation = (int)(previous >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive), "상한은 하한보다 커야 한다.");

            ulong range = (ulong)((long)maxExclusive - minInclusive);

            // 나머지 편향 제거: 균등하지 않은 꼬리 구간이 나오면 다시 뽑는다.
            ulong threshold = (ulong.MaxValue - range + 1UL) % range;
            ulong draw;
            do
            {
                draw = ((ulong)NextUInt() << 32) | NextUInt();
            }
            while (draw < threshold);

            return (int)((long)minInclusive + (long)(draw % range));
        }

        public double NextDouble()
        {
            // 53비트 정밀도. [0, 1) 을 보장한다.
            ulong bits = (((ulong)NextUInt() << 32) | NextUInt()) >> 11;
            return bits * (1.0 / 9007199254740992.0);
        }

        public RandomSnapshot Capture()
        {
            return new RandomSnapshot(state, increment);
        }

        public void Restore(RandomSnapshot snapshot)
        {
            if ((snapshot.Sequence & 1UL) == 0UL)
                throw new ArgumentException("PCG32 증분은 홀수여야 한다.", nameof(snapshot));
            state = snapshot.State;
            increment = snapshot.Sequence;
        }
    }
}
