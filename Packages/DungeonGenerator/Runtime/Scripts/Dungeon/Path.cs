using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.Dungeon
{
    internal enum Direction
    {
        Up = 0,
        Down,
        Left,
        Right,
        MAX
    }
    /// <summary>
    /// 通路を表すクラス
    /// </summary>
    [Serializable]
    internal class Path
    {
        [SerializeField]
        private int id;
        [SerializeField]
        private int toRoomId;
        [SerializeField]
        private int fromRoomId;
        [SerializeField]
        private Vector2Int to;
        [SerializeField]
        private Vector2Int from;
        [SerializeField]
        private List<Vector2Int> pathPositionList = new List<Vector2Int>();
        [SerializeField]
        private Direction direction;

        public int Id
        {
            get => id;
            set => id = value;
        }

        public int ToRoomId
        {
            get => toRoomId;
            set => toRoomId = value;
        }

        public int FromRoomId
        {
            get => fromRoomId;
            set => fromRoomId = value;
        }

        public Vector2Int To
        {
            get => to;
            set => to = value;
        }
        public Vector2Int From
        {
            get => from;
            set => from = value;
        }

        public List<Vector2Int> PathPositionList => pathPositionList;
        public Direction Dir
        {
            get => direction;
            set => direction = value;
        }

        public Vector2Int Center => new Vector2Int((To.x + From.x) / 2, (To.y + From.y) / 2);

        public Path() { }

        public void SetIds(int fromRoomId, int toRoomId)
        {
            this.fromRoomId = fromRoomId;
            this.toRoomId = toRoomId;
        }

        /// <summary>
        /// 境界の座標を取得する
        /// </summary>
        /// <param name="fromArea"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private int GetBorderPosition(Area fromArea)
        {
            switch (Dir)
            {
                case Direction.Up:
                    return fromArea.y;
                case Direction.Down:
                    return fromArea.y + fromArea.height;
                case Direction.Left:
                    return fromArea.x;
                case Direction.Right:
                    return fromArea.x + fromArea.width;
                default:
                    throw new Exception($"{Dir}は未定義です");
            }
        }

        /// <summary>
        /// 座標リストを作成する
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="fromArea"></param>
        public void CreatePositionList(Vector2Int from, Vector2Int to, Area fromArea)
        {
            this.from = from;
            this.to = to;

            var borderPosition = GetBorderPosition(fromArea);
            var fromPosition = Vector2Int.zero;
            var toPosition = Vector2Int.zero;

            fromPosition.x = Mathf.Min(From.x, To.x);
            fromPosition.y = Mathf.Min(From.y, To.y);

            toPosition.x = Mathf.Max(From.x, To.x);
            toPosition.y = Mathf.Max(From.y, To.y);

            pathPositionList = new();
            if (Dir == Direction.Up || Dir == Direction.Down)
                CreatePositionListVertical(borderPosition);
            else
                CreatePositionListHorizontal(borderPosition);
        }

        /// <summary>
        /// 垂直方向の座標リストを作成する
        /// </summary>
        /// <param name="borderPosition"></param>
        private void CreatePositionListVertical(int borderPosition)
        {
            var x = From.x;
            if (From.y < To.y)
            {
                for (int y = From.y; y <= To.y; y++)
                {
                    if (y == borderPosition) CreatePositionListHorizontal(ref x, y);
                    PathPositionList.Add(new Vector2Int(x, y));
                }
                return;
            }

            for (var y = From.y; y >= To.y; y--)
            {
                if (y == borderPosition) CreatePositionListHorizontal(ref x, y);
                PathPositionList.Add(new Vector2Int(x, y));
            }
        }

        /// <summary>
        /// 垂直方向の座標リストを作成する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CreatePositionListVertical(int x, ref int y)
        {
            if (From.y < To.y)
            {
                for (y = From.y; y < To.y; y++) PathPositionList.Add(new Vector2Int(x, y));
                return;
            }
            for (y = From.y; y > To.y; y--) PathPositionList.Add(new Vector2Int(x, y));
        }

        /// <summary>
        /// 水平方向の座標リストを作成する
        /// </summary>
        /// <param name="borderPosition"></param>
        private void CreatePositionListHorizontal(int borderPosition)
        {
            var y = From.y;
            if (From.x < To.x)
            {
                for (var x = From.x; x <= To.x; x++)
                {
                    if (x == borderPosition) CreatePositionListVertical(x, ref y);
                    PathPositionList.Add(new Vector2Int(x, y));
                }
                return;
            }

            for (var x = From.x; x >= To.x; x--)
            {
                if (x == borderPosition) CreatePositionListVertical(x, ref y);
                PathPositionList.Add(new Vector2Int(x, y));
            }
        }

        /// <summary>
        /// 水平方向の座標リストを作成する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void CreatePositionListHorizontal(ref int x, int y)
        {
            if (From.x < To.x)
            {
                for (x = From.x; x < To.x; x++) PathPositionList.Add(new Vector2Int(x, y));
                return;
            }
            for (x = From.x; x > To.x; x--) PathPositionList.Add(new Vector2Int(x, y));
        }
    }
}
