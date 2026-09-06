using System;
using System.Collections.Generic;
using Abyss.Portfolio.Graph;

namespace Abyss.Portfolio.Tests
{
    /// <summary>제출용 독립 추출 과정에서 작성한 BFS 테스트입니다. G03은 원 프로젝트의 보스방 선택 규칙입니다.</summary>
    internal static class GraphSearchTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("G03_cycle_uses_shortest_paths", () => G03_Cycle(runner));
            runner.Run("G03_disconnected_rooms_are_not_candidates", () => G03_Disconnected(runner));
            runner.Run("G03_nonexistent_start_is_rejected", () => G03_MissingStart(runner));
            runner.Run("G03_tie_uses_ordinal_id_regardless_of_input_order", () => G03_DeterministicTie(runner));
            runner.Run("G03_isolated_start_selects_itself", () => G03_IsolatedStart(runner));
            runner.Run("G03_dangling_edge_is_rejected", () => G03_DanglingEdge(runner));
        }

        private static void G03_Cycle(TestRunner runner)
        {
            // A-B-C-A 순환과 C-D 끝방. 재방문으로 무한 루프에 빠지거나 긴 경로를 고르면 실패합니다.
            Dictionary<string, IReadOnlyList<string>> graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "A", new[] { "B", "C" } },
                { "B", new[] { "A", "C" } },
                { "C", new[] { "B", "A", "D" } },
                { "D", new[] { "C" } }
            };
            IReadOnlyDictionary<string, int> distance = GraphSearch.DistancesFrom(graph, "A");
            runner.Equal(4, distance.Count, "각 방은 한 번만 기록되어야 합니다.");
            runner.Equal(0, distance["A"], "시작 방 거리");
            runner.Equal(1, distance["B"], "첫 이웃 거리");
            runner.Equal(1, distance["C"], "순환을 따라 2로 덮어쓰면 안 됩니다.");
            runner.Equal(2, distance["D"], "끝방의 최단 거리");
            runner.Equal("D", GraphSearch.ChooseFarthestRoom(graph, "A"), "가장 깊은 방을 선택합니다.");
        }

        private static void G03_Disconnected(TestRunner runner)
        {
            Dictionary<string, IReadOnlyList<string>> graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "Entrance", new[] { "Reachable" } },
                { "Reachable", new[] { "Entrance" } },
                { "Island-1", new[] { "Island-2" } },
                { "Island-2", new[] { "Island-1", "Island-3" } },
                { "Island-3", new[] { "Island-2" } }
            };
            IReadOnlyDictionary<string, int> distance = GraphSearch.DistancesFrom(graph, "Entrance");
            runner.Equal(2, distance.Count, "입구에서 도달 가능한 방만 기록합니다.");
            runner.True(!distance.ContainsKey("Island-1"), "분리된 섬에는 거리를 부여하지 않습니다.");
            runner.Equal("Reachable", GraphSearch.ChooseFarthestRoom(graph, "Entrance"), "도달할 수 없는 섬은 보스 후보가 아닙니다.");
        }

        private static void G03_MissingStart(TestRunner runner)
        {
            Dictionary<string, IReadOnlyList<string>> graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "A", Array.Empty<string>() }
            };
            runner.Throws<ArgumentException>(() => GraphSearch.DistancesFrom(graph, "missing"), "존재하지 않는 시작점은 거부합니다.");
            runner.Throws<ArgumentException>(() => GraphSearch.ChooseFarthestRoom(graph, "missing"), "선택 API도 동일한 입력 계약을 따릅니다.");
        }

        private static void G03_DeterministicTie(TestRunner runner)
        {
            Dictionary<string, IReadOnlyList<string>> first = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "Entrance", new[] { "Room-B", "Room-A" } },
                { "Room-B", new[] { "Entrance" } },
                { "Room-A", new[] { "Entrance" } }
            };
            Dictionary<string, IReadOnlyList<string>> reordered = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "Room-A", new[] { "Entrance" } },
                { "Room-B", new[] { "Entrance" } },
                { "Entrance", new[] { "Room-A", "Room-B" } }
            };
            runner.Equal("Room-A", GraphSearch.ChooseFarthestRoom(first, "Entrance"), "B가 먼저 탐색되어도 A가 선택됩니다.");
            runner.Equal("Room-A", GraphSearch.ChooseFarthestRoom(reordered, "Entrance"), "딕셔너리·간선 입력 순서를 바꿔도 결과가 같습니다.");
        }

        private static void G03_IsolatedStart(TestRunner runner)
        {
            Dictionary<string, IReadOnlyList<string>> graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "Entrance", Array.Empty<string>() }
            };
            runner.Equal(0, GraphSearch.DistancesFrom(graph, "Entrance")["Entrance"], "고립된 시작점 거리는 0입니다.");
            runner.Equal("Entrance", GraphSearch.ChooseFarthestRoom(graph, "Entrance"), "후보가 시작점뿐이면 시작점을 반환합니다.");
        }

        private static void G03_DanglingEdge(TestRunner runner)
        {
            Dictionary<string, IReadOnlyList<string>> graph = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                { "Entrance", new[] { "missing" } }
            };
            runner.Throws<ArgumentException>(() => GraphSearch.DistancesFrom(graph, "Entrance"), "없는 방을 참조한 간선은 거부합니다.");
        }
    }
}
