using System;
using System.Collections.Generic;

namespace Abyss.Portfolio.Graph
{
    /// <summary>
    /// 제출용으로 독립 추출한 샘플입니다.
    /// Abyss DungeonGenerator의 DistancesFrom/ChooseBossRoom에서 BFS와 동률 선택만 발췌했습니다.
    /// Room/Hallway 대신 인접 목록을 받고, 입력 검증과 거리 탐색 공통화를 추가했습니다.
    /// 던전 생성, 콘텐츠 배치, 게임 도메인 모델은 포함하지 않습니다.
    /// </summary>
    public static class GraphSearch
    {
        /// <summary>
        /// 가중치 없는 그래프에서 시작점에서 각 방까지의 최단 거리를 반환합니다.
        /// 도달 불가능한 방은 결과에 없습니다. 각 간선은 이동 1회입니다.
        /// BFS 시간 O(V+E), 보조 공간 O(V). 무방향 복도는 양방향 간선을 입력합니다.
        /// </summary>
        public static IReadOnlyDictionary<string, int> DistancesFrom(
            IReadOnlyDictionary<string, IReadOnlyList<string>> adjacency, string originId)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));
            if (originId == null) throw new ArgumentNullException(nameof(originId));
            if (!adjacency.ContainsKey(originId))
                throw new ArgumentException("시작 방이 그래프에 없습니다.", nameof(originId));

            Dictionary<string, int> distance = new Dictionary<string, int>(StringComparer.Ordinal);
            Queue<string> queue = new Queue<string>();
            distance[originId] = 0;
            queue.Enqueue(originId);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                IReadOnlyList<string> neighbors = adjacency[current];
                if (neighbors == null)
                    throw new ArgumentException("인접 목록이 null입니다: " + current, nameof(adjacency));

                for (int index = 0; index < neighbors.Count; index++)
                {
                    string next = neighbors[index];
                    if (next == null || !adjacency.ContainsKey(next))
                        throw new ArgumentException("간선의 도착 방이 없습니다: " + next, nameof(adjacency));

                    // 큐에 넣기 전에 방문 처리하여 순환 그래프에서도 한 번만 탐색합니다.
                    if (distance.ContainsKey(next)) continue;
                    distance[next] = distance[current] + 1;
                    queue.Enqueue(next);
                }
            }
            return distance;
        }

        /// <summary>
        /// G03: 최단 거리가 가장 큰 방을 선택합니다. 동률이면 Ordinal ID가 앞선 방입니다.
        /// 도달 가능한 방만 후보이며, 시작 방만 있으면 시작 방을 반환합니다.
        /// 원본의 정렬 기반 동률 규칙을 유지하므로 전체 시간은 O(V log V+E)입니다.
        /// </summary>
        public static string ChooseFarthestRoom(
            IReadOnlyDictionary<string, IReadOnlyList<string>> adjacency, string entranceId)
        {
            IReadOnlyDictionary<string, int> distance = DistancesFrom(adjacency, entranceId);
            string best = entranceId;
            int bestDistance = -1;
            List<string> ordered = new List<string>(distance.Keys);
            ordered.Sort(StringComparer.Ordinal);

            for (int index = 0; index < ordered.Count; index++)
            {
                if (distance[ordered[index]] > bestDistance)
                {
                    bestDistance = distance[ordered[index]];
                    best = ordered[index];
                }
            }
            return best;
        }
    }
}
