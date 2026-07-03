using System;
using UnityEngine;

namespace Xeon.Dungeon
{
    public abstract class DungeonSetting<T> : ScriptableObject where T : Enum
    {
        [SerializeField]
        private T baseTileType;
        [SerializeField]
        private T pathTileType, roomTileType;
        [SerializeField]
        private TileSetting<T>[] tileSettings = new TileSetting<T>[0];

        public T BaseTileType => baseTileType;
        public T PathTileType => pathTileType;
        public T RoomTileType => roomTileType;

        public int GetCost(T type)
        {
            foreach (var tile in tileSettings)
            {
                if (tile.Type.Equals(type)) return tile.Cost;
            }
            return 0;
        }
    }
}
