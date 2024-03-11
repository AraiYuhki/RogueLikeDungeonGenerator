using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Utility;

namespace Xeon.Dungeon
{
    /// <summary>
    /// 部屋を表すクラス
    /// </summary>
    [Serializable]
    internal class Room
    {
        [SerializeField]
        private int id;
        [SerializeField]
        private RectInt rect;
        [SerializeField]
        private Encyclopedia<int, Path> connected = new();
        [SerializeField]
        private Encyclopedia<int, Vector2Int> connectedPoints = new();
        public int x => rect.x;
        public int y => rect.y;
        public int width => rect.width;
        public int height => rect.height;
        public int Id => id;

        /// <summary>
        /// 接続先番号
        /// </summary>
        public Encyclopedia<int, Path> Connected => connected;
        /// <summary>
        /// (接続先の部屋ID, 接続元の部屋から通路への入り口の座標)
        /// </summary>
        public Encyclopedia<int, Vector2Int> ConnectedPoint => connectedPoints;
        public List<int> ConnectedRooms => Connected.Keys.ToList();
        public Vector2Int Center => new Vector2Int((int)rect.center.x, (int)rect.center.y);

        public Room()
        {
        }

        public Room(int id, int x, int y, int width, int height)
        {
            this.id = id;
            rect = new RectInt(x, y, width, height);
        }

        /// <summary>
        /// 部屋に接続されている通路を追加する
        /// </summary>
        /// <param name="toRoomID">接続先の部屋ID</param>
        /// <param name="path">通路インスタンス　</param>
        /// <param name="connectedPoint">接続元の部屋から通路に入る座標</param>
        public void AddPath(int toRoomID, Path path, Vector2Int connectedPoint)
        {
            if (connected.ContainsKey(toRoomID)) return;
            connected.Add(toRoomID, path);
            connectedPoints.Add(toRoomID, connectedPoint);
        }

        public void RemovePath(int toRoomId)
        {
            if (!connected.ContainsKey(toRoomId)) return;
            connected.Remove(toRoomId);
            connectedPoints.Remove(toRoomId);
        }

        public bool CheckPathBeing(int toRoomId) => ConnectedRooms.Contains(toRoomId);
    }
}
