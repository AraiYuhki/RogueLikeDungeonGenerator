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
        /// 座標リストを作成する
        /// 出発点から折れ曲がり位置まで直進し、折れ曲がり位置で軸を変えて目的地まで進む
        /// </summary>
        /// <param name="from">出発点(出発側の部屋の縁)</param>
        /// <param name="to">目的地(接続先の部屋の縁)</param>
        /// <param name="bendPosition">折れ曲がり位置(垂直通路ならY座標、水平通路ならX座標)</param>
        public void CreatePositionList(Vector2Int from, Vector2Int to, int bendPosition)
        {
            this.from = from;
            this.to = to;
            pathPositionList = new();

            if (Dir == Direction.Up || Dir == Direction.Down)
            {
                AddVerticalSegment(from.x, from.y, bendPosition);
                AddHorizontalSegment(bendPosition, from.x, to.x);
                AddVerticalSegment(to.x, bendPosition, to.y, includeEnd: true);
            }
            else
            {
                AddHorizontalSegment(from.y, from.x, bendPosition);
                AddVerticalSegment(bendPosition, from.y, to.y);
                AddHorizontalSegment(to.y, bendPosition, to.x, includeEnd: true);
            }
        }

        /// <summary>
        /// 水平方向のセグメントを追加する(始点を含み、includeEnd指定時のみ終点を含む)
        /// </summary>
        private void AddHorizontalSegment(int y, int fromX, int toX, bool includeEnd = false)
        {
            var step = toX >= fromX ? 1 : -1;
            for (var x = fromX; x != toX; x += step)
                pathPositionList.Add(new Vector2Int(x, y));
            if (includeEnd)
                pathPositionList.Add(new Vector2Int(toX, y));
        }

        /// <summary>
        /// 垂直方向のセグメントを追加する(始点を含み、includeEnd指定時のみ終点を含む)
        /// </summary>
        private void AddVerticalSegment(int x, int fromY, int toY, bool includeEnd = false)
        {
            var step = toY >= fromY ? 1 : -1;
            for (var y = fromY; y != toY; y += step)
                pathPositionList.Add(new Vector2Int(x, y));
            if (includeEnd)
                pathPositionList.Add(new Vector2Int(x, toY));
        }
    }
}
