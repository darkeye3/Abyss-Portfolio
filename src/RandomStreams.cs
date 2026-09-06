// Abyss 프로젝트에서 발췌한 실제 구현입니다. 제출용으로 설명 주석만 정리했습니다.
using System;
using System.Collections.Generic;

namespace Abyss.Core
{
    /// <summary>
    /// 하위 시스템별 난수 스트림. 전리품 난수 소비가 늘어도 전투·던전 수열은 유지한다.
    /// 스트림 이름의 안정 해시를 PCG32 sequence로 사용한다.
    /// 한 시스템 내부의 판정 순서는 같은 스트림 안에서 유지한다.
    /// </summary>
    public sealed class RandomStreams
    {
        /// <summary>
        /// 스트림 이름. 오타로 새 스트림이 조용히 생기지 않도록 상수로 둔다.
        /// </summary>
        public static class Names
        {
            /// <summary>주차 진행 · 마을 이벤트</summary>
            public const string Campaign = "campaign";

            /// <summary>영입 · 활동 · 스트레스 회복 부작용</summary>
            public const string Town = "town";

            /// <summary>던전 생성 · 방 배치</summary>
            public const string Dungeon = "dungeon";

            /// <summary>조우 편성 · 기습 · 증원</summary>
            public const string Encounter = "encounter";

            /// <summary>명중 · 치명 · 피해 · 상태 저항 · 결의 판정</summary>
            public const string Combat = "combat";

            /// <summary>몬스터 욕구 선택 · 대상 선택</summary>
            public const string Ai = "ai";

            /// <summary>전리품 · 기물 · 함정 · 허기</summary>
            public const string Loot = "loot";
        }

        /// <summary>
        /// 알려진 스트림 전부. 순서가 저장 형식에 영향을 주지 않도록 정렬해 둔다.
        /// 배열을 그대로 노출하면 <c>string[]</c>로 되돌려 전역 목록을 바꿀 수 있으므로
        /// 실제 인스턴스도 수정 불가능한 컬렉션으로 감싼다.
        /// </summary>
        public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
        {
            Names.Ai, Names.Campaign, Names.Combat, Names.Dungeon,
            Names.Encounter, Names.Loot, Names.Town
        });

        private readonly Dictionary<string, Pcg32RandomSource> sources =
            new Dictionary<string, Pcg32RandomSource>(StringComparer.Ordinal);

        public ulong MasterSeed { get; }

        public RandomStreams(ulong masterSeed)
        {
            MasterSeed = masterSeed;
            for (int index = 0; index < All.Count; index++)
                sources[All[index]] = new Pcg32RandomSource(masterSeed, SequenceOf(All[index]));
        }

        /// <summary>
        /// 스트림 하나. 모르는 이름은 <b>거부한다.</b>
        ///
        /// 없으면 만들어 주는 편이 편하지만, 오타 하나가 조용히 새 스트림을 만들고
        /// 그 뽑기는 아무 스트림에도 속하지 않게 된다. 부팅 때 터지는 편이 낫다 (D05 와 같은 태도).
        /// </summary>
        public IRandomSource Get(string name)
        {
            Pcg32RandomSource source;
            if (!sources.TryGetValue(name, out source))
                throw new ArgumentException("알 수 없는 난수 스트림이다: " + name, nameof(name));
            return source;
        }

        /// <summary>S07 — 모든 스트림의 상태. 하나라도 빠지면 이어했을 때 결과가 달라진다.</summary>
        public IReadOnlyDictionary<string, RandomSnapshot> Capture()
        {
            Dictionary<string, RandomSnapshot> captured =
                new Dictionary<string, RandomSnapshot>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, Pcg32RandomSource> pair in sources)
                captured[pair.Key] = pair.Value.Capture();
            return captured;
        }

        /// <summary>
        /// S07 — 저장에서 되돌린다.
        ///
        /// 저장에 없는 스트림은 <b>그대로 둔다.</b> 마스터 시드로 이미 초기화되어 있으므로
        /// 새로 생긴 스트림이 있는 옛 저장도 읽을 수 있다 (S03 마이그레이션과 같은 태도).
        /// </summary>
        public void Restore(IReadOnlyDictionary<string, RandomSnapshot> captured)
        {
            if (captured == null) throw new ArgumentNullException(nameof(captured));

            foreach (KeyValuePair<string, RandomSnapshot> pair in captured)
            {
                Pcg32RandomSource source;
                if (!sources.TryGetValue(pair.Key, out source)) continue;   // 사라진 스트림은 무시
                source.Restore(pair.Value);
            }
        }

        /// <summary>
        /// 스트림 이름 → PCG sequence.
        ///
        /// FNV-1a 를 직접 쓴다. <c>string.GetHashCode()</c> 를 쓰면 안 된다 —
        /// .NET Core 에서는 프로세스마다 값이 달라지도록 무작위화되어 있어서,
        /// <b>같은 시드로 두 번 돌려도 결과가 달라진다.</b> A04 가 조용히 깨지는 자리다.
        /// </summary>
        private static ulong SequenceOf(string name)
        {
            const ulong Offset = 14695981039346656037UL;
            const ulong Prime = 1099511628211UL;

            ulong hash = Offset;
            for (int index = 0; index < name.Length; index++)
                hash = unchecked((hash ^ name[index]) * Prime);
            return hash;
        }
    }
}
