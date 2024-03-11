using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.Dungeon;

namespace Xeon.Utility
{
    public class TerrainGenerator
    {
        private static readonly Vector2Int[] Offsets = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.one,
            -Vector2Int.one,
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1)
        };

        private List<TileType> tileTypes = new();
        private Vector2Int size;
        private TileType[,] map;
        private int executeCount = 0;

        public static TileType[,] Generate(Vector2Int size, float weatheringRate = 0.1f, params TileType[] types)
        {
            if (types.Length <= 1)
                throw new Exception("地形のタイプは複数指定してください。");
            var generator = new TerrainGenerator(size, weatheringRate, types);
            return generator.map;
        }

        private TerrainGenerator(Vector2Int size, float weatheringRate, params TileType[] types)
        {
            this.size = size;
            tileTypes = types.ToList();
            map = new TileType[size.x, size.y];
            executeCount = Mathf.CeilToInt(Mathf.Min(size.x, size.y) * weatheringRate);
            Generate();
        }

        private void Generate()
        {
            for (var x = 0; x < size.x; x++)
                for (var y = 0; y < size.y; y++)
                    map[x, y] = tileTypes.Random();
            foreach (var _ in Enumerable.Repeat<int>(0, executeCount))
                ExecuteCellAutomaton();
        }

        private void ExecuteCellAutomaton()
        {
            var tmp = new TileType[size.x, size.y];
            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    tmp[x, y] = GetNextTile(x, y);
                }
            }
            map = tmp;
        }

        private bool TryGetTile(int x, int y, out TileType tileType)
        {
            tileType = tileTypes.First();
            if (x < 0 || y < 0) return false;
            if (x >= size.x || y >= size.y) return false;
            tileType = map[x, y];
            return true;
        }

        private TileType[] GetArroundTile(int x, int y)
        {
            var result = new List<TileType>();
            foreach (var offset in Offsets)
            {
                if (TryGetTile(x + offset.x, y + offset.y, out var tile))
                    result.Add(tile);
            }
            return result.ToArray();
        }

        private TileType GetNextTile(int x, int y)
        {
            var result = new Dictionary<TileType, int>();
            foreach (var tile in GetArroundTile(x, y))
            {
                if (!result.ContainsKey(tile))
                    result.Add(tile, 0);
                result[tile]++;
            }
            foreach (var type in tileTypes)
                if (result.TryGetValue(type, out var count) && count >= 4) return type;

            return map[x, y];
        }
    }
}
