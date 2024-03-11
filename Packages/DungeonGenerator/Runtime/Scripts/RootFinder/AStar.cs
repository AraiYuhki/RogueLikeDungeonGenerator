using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Dungeon;

namespace Xeon.RootFinder
{
    public interface IUnitContainer
    {
        bool ExistsUnit(Vector2Int position);
    }

    /// <summary>
    /// A*経路探索クラス
    /// </summary>
    public class AStar
    {
        private FloorData floorData;
        private Vector2Int size;

        private IUnitContainer unitContainer;

        private static readonly Vector2Int[] OffsetList = new Vector2Int[]{
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up + Vector2Int.right,
            Vector2Int.down + Vector2Int.right,
            Vector2Int.up + Vector2Int.left,
            Vector2Int.down + Vector2Int.left
            };

        public AStar()
        {
        }

        public AStar(FloorData floorData)
        {
            this.floorData = floorData;
            size = floorData.Size;
        }

        public AStar(FloorData floorData, IUnitContainer container)
        {
            this.floorData = floorData;
            this.unitContainer = container;
            size = floorData.Size;
        }

        public void Setup(FloorData floorData, IUnitContainer unitContainer)
        {
            this.floorData = floorData;
            this.unitContainer = unitContainer;
            size = floorData.Size;
        }

        public List<Vector2Int> FindRoot(Vector2Int startPoint, Vector2Int endPoint)
        {
            var nodes = new AStarNode[size.x, size.y];
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var node = new AStarNode(x, y);
                    if (unitContainer != null && unitContainer.ExistsUnit(node.Position))
                        node.Cost += 100;
                    nodes[x, y] = node;
                }
            }
            var result = new List<Vector2Int>();
            var openedNode = new List<AStarNode>();
            FindRoot(endPoint, nodes[startPoint.x, startPoint.y], nodes, ref result, ref openedNode);
            return result;
        }

        private bool FindRoot(Vector2Int endPoint, AStarNode current, AStarNode[,] nodes, ref List<Vector2Int> result, ref List<AStarNode> openedNode)
        {
            current.State = NodeState.Close;
            openedNode.Remove(current);
            var goal = OpenAround(endPoint, nodes, current, ref openedNode);
            if (goal != null)
            {
                result = CreateRoot(goal);
                return true;
            }
            while (openedNode.Count > 0)
            {
                var next = openedNode.OrderBy(node => node.Score).First();
                if (FindRoot(endPoint, next, nodes, ref result, ref openedNode))
                    return true;
            }
            return false;
        }

        private List<Vector2Int> CreateRoot(AStarNode current)
        {
            var tmp = current;
            var result = new List<Vector2Int>() { current.Position };
            while (tmp != null)
            {
                result.Add(tmp.Position);
                tmp = tmp.Parent;
            }
            result.Reverse();
            return result;
        }

        private AStarNode OpenAround(Vector2Int endPoint, AStarNode[,] nodes, AStarNode node, ref List<AStarNode> openedNode)
        {
            var position = node.Position;
            node.State = NodeState.Close;
            foreach (var offset in OffsetList)
            {
                var targetPosition = position + offset;
                if (targetPosition.x < 0 || targetPosition.x >= size.x || targetPosition.y < 0 || targetPosition.y >= size.y)
                    continue;
                var targetNode = nodes[targetPosition.x, targetPosition.y];
                if (floorData.Map[targetPosition.x, targetPosition.y].IsWall)
                    continue;
                if (targetNode.State != NodeState.None)
                    continue;
                openedNode.Add(targetNode);
                targetNode.State = NodeState.Open;
                targetNode.Parent = node;
                if (offset.x != 0 && offset.y != 0)
                    targetNode.Cost = node.Cost + 1.5f;
                else
                    targetNode.Cost = node.Cost + 1f;
                if (unitContainer != null && unitContainer.ExistsUnit(targetPosition))
                    targetNode.Cost += 100f;
                targetNode.CalculateEstimatedCost(endPoint);
                if (targetNode.Position.x == endPoint.x && targetNode.Position.y == endPoint.y)
                {
                    return targetNode;
                }
            }
            return null;
        }
    }
}
