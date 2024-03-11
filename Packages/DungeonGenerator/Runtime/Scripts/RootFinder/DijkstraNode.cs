using System.Collections.Generic;
using UnityEngine;
using Xeon.Dungeon;

namespace Xeon.RootFinder
{
    internal enum NodeStatus
    {
        None,
        Open,
        Close
    }
    public class DijkstraNode
    {
        private const int PathNodeIdOffset = 1000;
        public int Id
        {
            get
            {
                if (IsPathNode)
                    return Path.Id + PathNodeIdOffset;
                return Room.Id;
            }
        }
        internal NodeStatus Status { get; set; } = NodeStatus.None;
        internal Room Room { get; set; } = null;
        internal Path Path { get; set; } = null;
        public int Score { get; set; } = int.MaxValue;
        public DijkstraNode Parent { get; set; }
        public Dictionary<int, int> ConnectedCosts { get; set; } = new();
        public bool IsPathNode => Path != null;
        public Vector3 Position() => IsPathNode ? new Vector3(Path.Center.x, Path.Center.y) : new Vector3(Room.Center.x, Room.Center.y);
    }

}
