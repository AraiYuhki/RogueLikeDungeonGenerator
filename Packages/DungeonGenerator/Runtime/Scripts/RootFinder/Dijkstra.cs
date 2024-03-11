using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Dungeon;

namespace Xeon.RootFinder
{
    /// <summary>
    /// ダイクストラ経路探索クラス
    /// </summary>
    public partial class Dijkstra
    {
        private const int PathNodeIdOffset = 1000;

        private Dictionary<int, DijkstraNode> nodes = new Dictionary<int, DijkstraNode>();
        private List<DijkstraNode> openNodes = new List<DijkstraNode>();
        private FloorData floorData;

        public Dictionary<int, DijkstraNode> Nodes => nodes;
        public Dijkstra(FloorData data)
        {
            floorData = data;
            Initialize();
        }

        private void Initialize()
        {
            nodes = floorData.Rooms.ToDictionary(room => room.Id, room => new DijkstraNode() { Room = room });
            foreach (var path in floorData.Paths)
            {
                var pathNode = new DijkstraNode { Path = path };
                var halfCost = path.PathPositionList.Count / 2;

                pathNode.ConnectedCosts[path.FromRoomId] = path.PathPositionList.Count - halfCost;
                pathNode.ConnectedCosts[path.ToRoomId] = path.PathPositionList.Count - halfCost;
                nodes.Add(pathNode.Id, pathNode);

                nodes[path.FromRoomId].ConnectedCosts[path.ToRoomId] = path.PathPositionList.Count;
                nodes[path.ToRoomId].ConnectedCosts[path.FromRoomId] = path.PathPositionList.Count;
            }
        }

        private void Reset()
        {
            foreach (var node in nodes.Values)
            {
                node.Status = NodeStatus.None;
                node.Parent = null;
                node.Score = int.MaxValue;
            }
        }

        public List<int> GetRoot(int from, int to)
        {
            if (from < 0 || to < 0) return null;
            Reset();
            if (!nodes.ContainsKey(from))
            {
                Debug.LogError($"ID:{from}のノードが登録されていません");
                return null;
            }
            if (!nodes.ContainsKey(to))
            {
                Debug.LogError($"ID:{to}のノードが登録されていません");
                return null;
            }
            nodes[from].Status = NodeStatus.Open;
            nodes[from].Score = 0;
            openNodes.Add(nodes[from]);
            while (openNodes.Any())
            {
                foreach (var node in openNodes.OrderBy(node => node.Score).ToList())
                    OpenConnected(node);
                openNodes = nodes.Values.Where(node => node.Status == NodeStatus.Open).ToList();
            }
            var goal = nodes[to];
            if (goal.Parent == null)
            {
                Debug.LogWarning($"Way to goal is not found {from} -> {to}");
                return null;
            }

            var current = goal;
            var result = new List<int>();
            try
            {
                while (current != null)
                {
                    // 同じタイルを登録しようとしたらループしている可能性がある
                    if (result.Contains(current.Id)) break;
                    result.Add(current.Id);
                    current = current.Parent;
                }
            }
            catch (OutOfMemoryException e)
            {
                Debug.LogException(e);
                return null;
            }
            result.Reverse();
            Debug.Log(string.Join("->", result));
            return result;
        }

        public List<int> GetRoot(TileData start, TileData end)
        {
            var startId = start.IsRoom ? start.Id : start.Id + PathNodeIdOffset;
            var endId = end.IsRoom ? end.Id : end.Id + PathNodeIdOffset;
            return GetRoot(startId, endId);
        }

        /// <summary>
        /// A*を併用した軽量経路探索
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="checkPoints"></param>
        /// <returns></returns>
        public List<Vector2Int> GetRoot(Vector2Int startPoint, Vector2Int endPoint, List<Vector2Int> checkPoints)
        {
            var points = new List<Vector2Int>() { startPoint };
            points.AddRange(checkPoints);
            points.Add(endPoint);
            var root = new List<Vector2Int>();
            var aStar = new AStar(floorData);
            for (var index = 0; index < points.Count - 2; index++)
            {
                var positions = aStar.FindRoot(points[index], points[index + 1]);
                if (positions != null) root.AddRange(positions);
            }
            return root;
        }

        public List<Vector2Int> GetCheckpoints(Vector2Int start, Vector2Int end)
        {
            var startTile = floorData.Map[start.x, start.y];
            var endTile = floorData.Map[end.x, end.y];
            var nodeList = GetRoot(startTile, endTile);
            var checkPoints = new List<Vector2Int>();

            if (nodeList == null) return checkPoints;

            for (var index = 0; index < nodeList.Count; index++)
            {
                var currentNode = nodes[nodeList[index]];
                // 原則通路ノードは最初のノードにしかないはずなので、それ以外のパターンは許容しない
                if (index == 0)
                {
                    // 最初のノードが通路の場合は何もしない
                    if (!currentNode.IsPathNode)
                    {
                        var nextNode = nodes[nodeList[index + 1]];
                        checkPoints.Add(currentNode.Room.ConnectedPoint[nextNode.Id]);
                    }
                    continue;
                }
                var prevNode = nodes[nodeList[index - 1]];
                var prevRoomId = 0;
                // ひとつ前のノードが通路の場合は通路の接続先から部屋のIDを類推し、チェックポイントの座標を探す
                if (prevNode.IsPathNode)
                    prevRoomId = prevNode.Path.ToRoomId == currentNode.Id ? prevNode.Path.FromRoomId : prevNode.Path.ToRoomId;
                else
                    prevRoomId = prevNode.Id;
                checkPoints.Add(currentNode.Room.ConnectedPoint[prevRoomId]);

                // 最後のノードなら目的地を追加
                if (index == nodeList.Count - 1)
                    checkPoints.Add(endTile.Position);
                else
                {
                    var nextNode = nodes[nodeList[index + 1]];
                    var nextRoomId = 0;
                    if (nextNode.IsPathNode)
                        nextRoomId = nextNode.Path.ToRoomId == currentNode.Id ? nextNode.Path.FromRoomId : nextNode.Path.ToRoomId;
                    else
                        nextRoomId = nextNode.Id;
                    checkPoints.Add(currentNode.Room.ConnectedPoint[nextRoomId]);
                }
            }
            return checkPoints;
        }

        private DijkstraNode OpenConnected(DijkstraNode node)
        {
            node.Status = NodeStatus.Close;
            if (nodes.Count <= 0)
            {
                Debug.LogWarning("Nodes is empty");
                return null;
            }
            foreach (var next in node.ConnectedCosts.Keys)
            {
                if (!nodes.ContainsKey(next))
                {
                    Debug.LogWarning($"{next} is not in nodes");
                    continue;
                }
                var nextNode = nodes[next];
                if (nextNode.Status == NodeStatus.Close) continue;
                nextNode.Status = NodeStatus.Open;
                var nextScore = node.Score + node.ConnectedCosts[nextNode.Id];
                if (nextNode.Score > nextScore)
                {
                    Debug.Log($"Update cost {nextNode.Id} {nextNode.Score} -> {nextScore}");
                    nextNode.Parent = node;
                    nextNode.Score = nextScore;
                }
            }
            return null;
        }
    }
}
