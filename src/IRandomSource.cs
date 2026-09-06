// Abyss 프로젝트에서 발췌한 실제 구현입니다. 제출용으로 설명 주석만 정리했습니다.
using System;

namespace Abyss.Core
{
    /// <summary>
    /// 규칙 계층이 쓰는 유일한 난수원 (A03).
    ///
    /// 구현체는 반드시 결정론적이어야 한다. 같은 시드에서 같은 호출 순서면
    /// 같은 값이 나와야 한다 (A04).
    ///
    /// 스냅샷을 뜰 수 있어야 세이브 재개 시 결과가 달라지지 않는다 (S07).
    /// </summary>
    public interface IRandomSource
    {
        /// <summary><paramref name="minInclusive"/> 이상 <paramref name="maxExclusive"/> 미만의 정수.</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>[0, 1) 범위의 실수.</summary>
        double NextDouble();

        /// <summary>현재 내부 상태. 저장에 쓴다.</summary>
        RandomSnapshot Capture();

        /// <summary>저장된 상태로 되돌린다.</summary>
        void Restore(RandomSnapshot snapshot);
    }

    /// <summary>난수원의 내부 상태 스냅샷.</summary>
    public readonly struct RandomSnapshot : IEquatable<RandomSnapshot>
    {
        public ulong State { get; }
        public ulong Sequence { get; }

        public RandomSnapshot(ulong state, ulong sequence)
        {
            State = state;
            Sequence = sequence;
        }

        public bool Equals(RandomSnapshot other) { return State == other.State && Sequence == other.Sequence; }
        public override bool Equals(object obj) { return obj is RandomSnapshot s && Equals(s); }
        public override int GetHashCode() { return State.GetHashCode() ^ Sequence.GetHashCode(); }
        public override string ToString() { return "state=" + State + " seq=" + Sequence; }
    }

    public static class RandomSourceExtensions
    {
        /// <summary>확률 판정. <paramref name="chance"/> 는 0~1.</summary>
        public static bool Check(this IRandomSource random, double chance)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (chance <= 0d) return false;
            if (chance >= 1d) return true;
            return random.NextDouble() < chance;
        }

        /// <summary>상한을 포함하는 정수 난수. 호출부에서 포함 여부를 명시한다.</summary>
        public static int NextInclusive(this IRandomSource random, int minInclusive, int maxInclusive)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (maxInclusive < minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxInclusive), "최댓값이 최솟값보다 작다.");
            if (maxInclusive == int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxInclusive), "int.MaxValue 는 포함형 상한으로 쓸 수 없다.");
            return random.NextInt(minInclusive, maxInclusive + 1);
        }
    }
}
