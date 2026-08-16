using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Dungeon;

namespace Xeon.RootFinder
{
    public class JpsSearchOptions
    {
        /// <summary>
        /// 斜め移動を許可するか
        /// </summary>
        public bool AllowDiagonal { get; set; } = true;

        /// <summary>
        /// 斜め移動時の角抜けを禁止するか
        /// </summary>
        public bool PreventCornerCutting { get; set; } = true;

        /// <summary>
        /// 追加の移動条件。
        /// true を返した場合のみ移動可能。
        /// </summary>
        public Func<FloorData, Vector2Int, Vector2Int, bool> MovementRule { get; set; }
    }

    /// <summary>
    /// Jump Point Search による経路探索クラス
    /// </summary>
    public class Jps
    {
        private readonly FloorData floorData;
        private readonly IUnitContainer unitContainer;
        private readonly Vector2Int size;

        private static readonly Vector2Int[] StraightDirections =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
        };

        private static readonly Vector2Int[] DiagonalDirections =
        {
            Vector2Int.up + Vector2Int.right,
            Vector2Int.up + Vector2Int.left,
            Vector2Int.down + Vector2Int.right,
            Vector2Int.down + Vector2Int.left,
        };

        private sealed class JpsNode
        {
            public Vector2Int Position;
            public float G;
            public float F;
            public JpsNode Parent;
        }

        public Jps(FloorData floorData, IUnitContainer unitContainer = null)
        {
            this.floorData = floorData;
            this.unitContainer = unitContainer;
            size = floorData.Size;
        }

        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, JpsSearchOptions options = null)
        {
            options ??= new JpsSearchOptions();

            if (!IsInside(start) || !IsInside(goal)) return new List<Vector2Int>();
            if (!IsWalkable(start)) return new List<Vector2Int>();
            if (!IsWalkable(goal)) return new List<Vector2Int>();

            var open = new List<JpsNode>();
            var nodeMap = new Dictionary<Vector2Int, JpsNode>();
            var closed = new HashSet<Vector2Int>();

            var startNode = new JpsNode
            {
                Position = start,
                G = 0f,
                F = Heuristic(start, goal),
                Parent = null,
            };
            open.Add(startNode);
            nodeMap[start] = startNode;

            while (open.Count > 0)
            {
                var current = open.OrderBy(node => node.F).First();
                open.Remove(current);

                if (current.Position == goal)
                    return BuildPath(current);

                closed.Add(current.Position);

                foreach (var direction in PruneDirections(current, options))
                {
                    var jumpPoint = Jump(current.Position, direction, goal, options);
                    if (!jumpPoint.HasValue) continue;
                    if (closed.Contains(jumpPoint.Value)) continue;

                    var distance = OctileDistance(current.Position, jumpPoint.Value);
                    var nextG = current.G + distance;

                    if (!nodeMap.TryGetValue(jumpPoint.Value, out var jumpNode))
                    {
                        jumpNode = new JpsNode { Position = jumpPoint.Value };
                        nodeMap[jumpPoint.Value] = jumpNode;
                    }
                    else if (nextG >= jumpNode.G)
                    {
                        continue;
                    }

                    jumpNode.Parent = current;
                    jumpNode.G = nextG;
                    jumpNode.F = nextG + Heuristic(jumpPoint.Value, goal);

                    if (!open.Contains(jumpNode))
                        open.Add(jumpNode);
                }
            }

            return new List<Vector2Int>();
        }

        private List<Vector2Int> BuildPath(JpsNode node)
        {
            var points = new List<Vector2Int>();
            var current = node;
            while (current != null)
            {
                points.Add(current.Position);
                current = current.Parent;
            }
            points.Reverse();
            return ExpandPath(points);
        }

        private List<Vector2Int> ExpandPath(List<Vector2Int> jumpPoints)
        {
            if (jumpPoints.Count <= 1) return jumpPoints;

            var result = new List<Vector2Int> { jumpPoints[0] };
            for (var i = 1; i < jumpPoints.Count; i++)
            {
                var from = jumpPoints[i - 1];
                var to = jumpPoints[i];
                var direction = NormalizeDirection(to - from);
                var current = from;

                while (current != to)
                {
                    current += direction;
                    result.Add(current);
                }
            }
            return result;
        }

        private IEnumerable<Vector2Int> PruneDirections(JpsNode node, JpsSearchOptions options)
        {
            var directions = new List<Vector2Int>();
            if (node.Parent == null)
            {
                directions.AddRange(StraightDirections);
                if (options.AllowDiagonal) directions.AddRange(DiagonalDirections);
                return directions;
            }

            var direction = NormalizeDirection(node.Position - node.Parent.Position);
            var dx = direction.x;
            var dy = direction.y;
            var x = node.Position.x;
            var y = node.Position.y;

            if (dx != 0 && dy != 0)
            {
                directions.Add(new Vector2Int(dx, dy));
                directions.Add(new Vector2Int(dx, 0));
                directions.Add(new Vector2Int(0, dy));

                if (IsBlocked(new Vector2Int(x - dx, y), node.Position, options) && IsMovable(node.Position, new Vector2Int(x - dx, y + dy), options))
                    directions.Add(new Vector2Int(-dx, dy));
                if (IsBlocked(new Vector2Int(x, y - dy), node.Position, options) && IsMovable(node.Position, new Vector2Int(x + dx, y - dy), options))
                    directions.Add(new Vector2Int(dx, -dy));
            }
            else if (dx != 0)
            {
                directions.Add(new Vector2Int(dx, 0));

                if (IsBlocked(new Vector2Int(x, y + 1), node.Position, options) && IsMovable(node.Position, new Vector2Int(x + dx, y + 1), options))
                    directions.Add(new Vector2Int(dx, 1));
                if (IsBlocked(new Vector2Int(x, y - 1), node.Position, options) && IsMovable(node.Position, new Vector2Int(x + dx, y - 1), options))
                    directions.Add(new Vector2Int(dx, -1));
            }
            else
            {
                directions.Add(new Vector2Int(0, dy));

                if (IsBlocked(new Vector2Int(x + 1, y), node.Position, options) && IsMovable(node.Position, new Vector2Int(x + 1, y + dy), options))
                    directions.Add(new Vector2Int(1, dy));
                if (IsBlocked(new Vector2Int(x - 1, y), node.Position, options) && IsMovable(node.Position, new Vector2Int(x - 1, y + dy), options))
                    directions.Add(new Vector2Int(-1, dy));
            }

            return directions.Where(offset => options.AllowDiagonal || offset.x == 0 || offset.y == 0);
        }

        private Vector2Int? Jump(Vector2Int current, Vector2Int direction, Vector2Int goal, JpsSearchOptions options)
        {
            var next = current + direction;
            if (!IsMovable(current, next, options)) return null;

            if (next == goal) return next;
            if (HasForcedNeighbor(next, direction, options)) return next;

            if (direction.x != 0 && direction.y != 0)
            {
                if (Jump(next, new Vector2Int(direction.x, 0), goal, options).HasValue) return next;
                if (Jump(next, new Vector2Int(0, direction.y), goal, options).HasValue) return next;
            }

            return Jump(next, direction, goal, options);
        }

        private bool HasForcedNeighbor(Vector2Int position, Vector2Int direction, JpsSearchOptions options)
        {
            var x = position.x;
            var y = position.y;
            var dx = direction.x;
            var dy = direction.y;

            if (dx != 0 && dy != 0)
            {
                if (IsBlocked(new Vector2Int(x - dx, y), position, options) && IsMovable(position, new Vector2Int(x - dx, y + dy), options))
                    return true;
                if (IsBlocked(new Vector2Int(x, y - dy), position, options) && IsMovable(position, new Vector2Int(x + dx, y - dy), options))
                    return true;
            }
            else if (dx != 0)
            {
                if (IsBlocked(new Vector2Int(x, y + 1), position, options) && IsMovable(position, new Vector2Int(x + dx, y + 1), options))
                    return true;
                if (IsBlocked(new Vector2Int(x, y - 1), position, options) && IsMovable(position, new Vector2Int(x + dx, y - 1), options))
                    return true;
            }
            else
            {
                if (IsBlocked(new Vector2Int(x + 1, y), position, options) && IsMovable(position, new Vector2Int(x + 1, y + dy), options))
                    return true;
                if (IsBlocked(new Vector2Int(x - 1, y), position, options) && IsMovable(position, new Vector2Int(x - 1, y + dy), options))
                    return true;
            }

            return false;
        }

        private bool IsBlocked(Vector2Int target, Vector2Int from, JpsSearchOptions options)
            => !IsMovable(from, target, options);

        private bool IsMovable(Vector2Int from, Vector2Int to, JpsSearchOptions options)
        {
            if (!IsInside(to) || !IsWalkable(to)) return false;

            var delta = to - from;
            var isDiagonal = delta.x != 0 && delta.y != 0;
            if (isDiagonal)
            {
                if (!options.AllowDiagonal) return false;

                if (options.PreventCornerCutting)
                {
                    var sideA = new Vector2Int(from.x + delta.x, from.y);
                    var sideB = new Vector2Int(from.x, from.y + delta.y);
                    if (!IsWalkable(sideA) || !IsWalkable(sideB))
                        return false;
                }
            }

            return options.MovementRule == null || options.MovementRule(floorData, from, to);
        }

        private bool IsWalkable(Vector2Int position)
        {
            if (!IsInside(position)) return false;
            if (floorData.Map[position.x, position.y].IsWall) return false;
            return unitContainer == null || !unitContainer.ExistsUnit(position);
        }

        private bool IsInside(Vector2Int position)
            => position.x >= 0 && position.y >= 0 && position.x < size.x && position.y < size.y;

        private static Vector2Int NormalizeDirection(Vector2Int offset)
            => new Vector2Int(Math.Sign(offset.x), Math.Sign(offset.y));

        private static float Heuristic(Vector2Int from, Vector2Int to)
            => OctileDistance(from, to);

        private static float OctileDistance(Vector2Int from, Vector2Int to)
        {
            var dx = Mathf.Abs(from.x - to.x);
            var dy = Mathf.Abs(from.y - to.y);
            var diagonal = Mathf.Min(dx, dy);
            var straight = Mathf.Max(dx, dy) - diagonal;
            return diagonal * 1.41421356f + straight;
        }
    }
}
