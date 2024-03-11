using System;
using UnityEngine;

namespace Xeon.Dungeon
{
    public enum TileType
    {
        Room,
        Path,
        Wall,
        Water,
        Hole,
    }

    [Serializable]
    public class TileData
    {
        [SerializeField]
        private Vector2Int position;
        [SerializeField]
        private TileType type = TileType.Wall;
        [SerializeField]
        private int id = -1;
        [SerializeField]
        private bool isDeleted = false;
        /// <summary>
        /// 座標
        /// </summary>
        public Vector2Int Position { get => position; set => position = value; }
        /// <summary>
        /// 部屋か？
        /// </summary>
        public TileType Type { get => type; set => type = value; }

        /// <summary>
        /// 削除済みのタイルか？
        /// </summary>
        public bool IsDeleted { get => isDeleted; set => isDeleted = value; }

        /// <summary>
        /// 部屋か通路のID
        /// </summary>
        public int Id { get => id; set => id = value; }

        public bool IsWall => Type == TileType.Wall;
        public bool IsRoom => Type == TileType.Room;
    }
}
