using UnityEngine;

namespace Xeon.RootFinder
{
    internal enum NodeState
    {
        None,
        Open,
        Close
    }

    internal class AStarNode
    {
        internal Vector2Int Position { get; set; } = new Vector2Int(0, 0);
        internal float Cost { get; set; } = 0;
        internal float Score { get; set; } = 0;
        internal int EstimatedCost { get; set; } = 0;
        internal NodeState State { get; set; } = NodeState.None;
        internal AStarNode Parent { get; set; } = null;
        internal AStarNode() { }
        internal AStarNode(int x, int y) => Position = new Vector2Int(x, y);

        internal void CalculateEstimatedCost(Vector2Int endPoint)
        {
            var x = Mathf.Abs(Position.x - endPoint.x);
            var y = Mathf.Abs(Position.y - endPoint.y);
            EstimatedCost = Mathf.Max(x, y);
            Score = Cost + EstimatedCost;
        }
    }
}
