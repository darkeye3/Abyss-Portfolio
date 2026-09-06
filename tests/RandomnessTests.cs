using System;
using System.Collections.Generic;
using Abyss.Core;

namespace Abyss.Portfolio.Tests
{
    /// <summary>제출 샘플을 위해 작성한 계약 테스트입니다. A03/A04/S07은 원 프로젝트 규칙 ID입니다.</summary>
    internal static class RandomnessTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("A04_500_extra_loot_draws_preserve_combat_and_dungeon", () => A04_StreamIsolation(runner));
            runner.Run("A04_same_seed_reproduces_all_streams", () => A04_SameSeed(runner));
            runner.Run("S07_capture_restore_resumes_every_stream", () => S07_AllStreamSnapshots(runner));
            runner.Run("S07_single_source_rewinds_and_recreates", () => S07_SingleSourceSnapshot(runner));
            runner.Run("A03_unknown_stream_name_is_rejected", () => A03_UnknownStream(runner));
            runner.Run("A03_ranges_and_invalid_inputs", () => A03_Ranges(runner));
        }

        private static void A04_StreamIsolation(TestRunner runner)
        {
            RandomStreams baseline = new RandomStreams(42UL);
            RandomStreams changed = new RandomStreams(42UL);
            RandomSnapshot originalLoot = changed.Get(RandomStreams.Names.Loot).Capture();

            // 전리품 규칙에 추가된 500번의 난수 소비를 전투·던전 조회 사이에 끼워 넣습니다.
            for (int index = 0; index < 500; index++)
            {
                changed.Get(RandomStreams.Names.Loot).NextInt(0, 1000000);
                runner.Equal(
                    baseline.Get(RandomStreams.Names.Combat).NextInt(0, 1000000),
                    changed.Get(RandomStreams.Names.Combat).NextInt(0, 1000000),
                    "전리품 변경 후에도 전투 수열이 유지되어야 합니다.");
                runner.Equal(
                    baseline.Get(RandomStreams.Names.Dungeon).NextInt(0, 1000000),
                    changed.Get(RandomStreams.Names.Dungeon).NextInt(0, 1000000),
                    "전리품 변경 후에도 던전 수열이 유지되어야 합니다.");
            }
            runner.True(!originalLoot.Equals(changed.Get(RandomStreams.Names.Loot).Capture()),
                "추가 난수가 실제로 소비되어야 비간섭 검사가 의미가 있습니다.");
        }

        private static void A04_SameSeed(TestRunner runner)
        {
            RandomStreams first = new RandomStreams(12345UL);
            RandomStreams second = new RandomStreams(12345UL);
            for (int stream = 0; stream < RandomStreams.All.Count; stream++)
            {
                string name = RandomStreams.All[stream];
                for (int draw = 0; draw < 128; draw++)
                {
                    runner.Equal(first.Get(name).NextInt(-10000, 10000), second.Get(name).NextInt(-10000, 10000),
                        name + " 정수 수열이 같아야 합니다.");
                    runner.Equal(first.Get(name).NextDouble(), second.Get(name).NextDouble(),
                        name + " 실수 수열이 같아야 합니다.");
                }
            }
        }

        private static void S07_AllStreamSnapshots(TestRunner runner)
        {
            RandomStreams running = new RandomStreams(99UL);
            for (int stream = 0; stream < RandomStreams.All.Count; stream++)
            {
                IRandomSource source = running.Get(RandomStreams.All[stream]);
                // 시스템마다 다른 소비 위치에서 저장합니다.
                for (int draw = 0; draw < stream * 7 + 3; draw++) source.NextDouble();
            }
            IReadOnlyDictionary<string, RandomSnapshot> snapshot = running.Capture();
            runner.Equal(RandomStreams.All.Count, snapshot.Count, "모든 시스템 상태가 저장되어야 합니다.");

            // 다른 시드로 만든 인스턴스에도 스냅샷의 전체 상태가 적용되는지 확인합니다.
            RandomStreams resumed = new RandomStreams(700UL);
            resumed.Restore(snapshot);
            for (int stream = 0; stream < RandomStreams.All.Count; stream++)
            {
                string name = RandomStreams.All[stream];
                for (int draw = 0; draw < 64; draw++)
                {
                    runner.Equal(running.Get(name).NextInt(0, 1000000), resumed.Get(name).NextInt(0, 1000000),
                        name + " 정수 수열이 저장 지점 다음부터 이어져야 합니다.");
                    runner.Equal(running.Get(name).NextDouble(), resumed.Get(name).NextDouble(),
                        name + " 실수 수열이 저장 지점 다음부터 이어져야 합니다.");
                }
            }
        }

        private static void S07_SingleSourceSnapshot(TestRunner runner)
        {
            Pcg32RandomSource source = new Pcg32RandomSource(777UL, 19UL);
            for (int draw = 0; draw < 17; draw++) source.NextDouble();
            RandomSnapshot snapshot = source.Capture();
            int[] expected = new int[128];
            for (int draw = 0; draw < expected.Length; draw++) expected[draw] = source.NextInt(-50000, 50000);

            source.Restore(snapshot);
            Pcg32RandomSource recreated = Pcg32RandomSource.FromSnapshot(snapshot);
            for (int draw = 0; draw < expected.Length; draw++)
            {
                runner.Equal(expected[draw], source.NextInt(-50000, 50000), "기존 인스턴스의 복원 결과");
                runner.Equal(expected[draw], recreated.NextInt(-50000, 50000), "새 인스턴스의 복원 결과");
            }
        }

        private static void A03_UnknownStream(TestRunner runner)
        {
            RandomStreams streams = new RandomStreams(1UL);
            RandomSnapshot before = streams.Get(RandomStreams.Names.Combat).Capture();
            runner.Throws<ArgumentException>(() => streams.Get("combatt"), "오타로 새 스트림을 만들면 안 됩니다.");
            runner.Equal(before, streams.Get(RandomStreams.Names.Combat).Capture(), "거부된 조회가 기존 난수를 소비하면 안 됩니다.");
        }

        private static void A03_Ranges(TestRunner runner)
        {
            Pcg32RandomSource source = new Pcg32RandomSource(2024UL);
            for (int draw = 0; draw < 256; draw++)
            {
                int integer = source.NextInt(-10, 3);
                double real = source.NextDouble();
                runner.True(integer >= -10 && integer < 3, "정수 상한은 제외입니다.");
                runner.True(real >= 0d && real < 1d, "실수는 [0, 1) 범위입니다.");
            }
            runner.Equal(3, source.NextInclusive(3, 3), "포함형 범위에는 상한이 들어갑니다.");
            RandomSnapshot before = source.Capture();
            runner.Throws<ArgumentOutOfRangeException>(() => source.NextInt(5, 5), "빈 범위는 거부해야 합니다.");
            runner.Throws<ArgumentOutOfRangeException>(() => source.NextInt(8, 2), "역전된 범위는 거부해야 합니다.");
            runner.Throws<ArgumentException>(() => source.Restore(new RandomSnapshot(10UL, 2UL)),
                "PCG32의 짝수 증분은 거부해야 합니다.");
            runner.Equal(before, source.Capture(), "잘못된 입력은 난수 상태를 바꾸면 안 됩니다.");
        }
    }
}
