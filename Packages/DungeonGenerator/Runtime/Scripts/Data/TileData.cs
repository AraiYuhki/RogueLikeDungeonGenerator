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
    public class TileData : TileData<TileType>
    {
        public override bool IsWall => type == TileType.Wall;
        public override bool IsRoom => type == TileType.Room;
    }

    [Serializable]
    public class TileData<T> where T : Enum
    {
        [SerializeField]
        protected Vector2Int position;
        [SerializeField]
        protected T type;
        [SerializeField]
        protected int id = -1;
        [SerializeField]
        protected bool isDeleted = false;
        /// <summary>
        /// 座標
        /// </summary>
        public Vector2Int Position { get => position; set => position = value; }
        /// <summary>
        /// 部屋か？
        /// </summary>
        public T Type { get => type; set => type = value; }

        /// <summary>
        /// 削除済みのタイルか？
        /// </summary>
        public bool IsDeleted { get => isDeleted; set => isDeleted = value; }

        /// <summary>
        /// 部屋か通路のID
        /// </summary>
        public int Id { get => id; set => id = value; }

        public virtual bool IsWall => false;
        public virtual bool IsRoom => false;
    }
}
