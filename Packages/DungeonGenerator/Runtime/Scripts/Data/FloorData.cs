using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xeon.RootFinder;
using Xeon.Utility;

namespace Xeon.Dungeon
{
    [Serializable]
    public class FloorData
    {
        [SerializeField]
        private Vector2Int stairPosition;
        [SerializeField]
        private Vector2Int spawnPoint;
        [SerializeField]
        private List<Room> rooms;
        [SerializeField]
        private List<Path> paths;
        [SerializeField]
        private Encyclopedia<Vector2Int, TileType> terrainData = new();

        public TileData[,] Map { get; private set; }
        public Vector2Int StairPosition { get => stairPosition; set => stairPosition = value; }
        public Vector2Int SpawnPoint { get => spawnPoint; set => spawnPoint = value; }
        public Vector2Int Size => new Vector2Int(Map.GetLength(0), Map.GetLength(1));
        internal List<Path> DeletedPaths { get; private set; }

        internal List<Room> Rooms => rooms;
        internal List<Path> Paths => paths;

        public bool IsSpawnPoint(Vector2Int index) => SpawnPoint == index;
        public bool IsSpawnPoint(int x, int y) => IsSpawnPoint(new Vector2Int(x, y));
        public bool IsStair(Vector2Int index) => StairPosition == index;
        public bool IsStair(int x, int y) => IsStair(new Vector2Int(x, y));

        internal FloorData() { }

        internal FloorData(int width, int height, List<Room> roomList, List<Path> pathList, float weatheringRate = 0.2f, params TileType[] wallTypes)
            => Initialize(new Vector2Int(width, height), roomList, pathList, weatheringRate, wallTypes);

        private void Initialize(Vector2Int size, List<Room> roomList, List<Path> pathList, float weatheringRate = 0.2f, params TileType[] wallTypes)
        {
            Map = new TileData[size.x, size.y];
            terrainData.Clear();
            if (weatheringRate > 0f)
            {
                if (wallTypes.Length <= 0)
                    wallTypes = new TileType[] { TileType.Wall, TileType.Water, TileType.Hole };
                var terrain = TerrainGenerator.Generate(size, weatheringRate, wallTypes);
                for (var x = 0; x < size.x; x++)
                {
                    for (var y = 0; y < size.y; y++)
                    {
                        Map[x, y] = new TileData() { Position = new Vector2Int(x, y), Type = terrain[x, y] };
                        terrainData[new Vector2Int(x, y)] = terrain[x, y];
                    }
                }
            }
            else
            {
                for (var x = 0; x < size.x; x++)
                {
                    for (var y = 0; y < size.y; y++)
                    {
                        Map[x, y] = new TileData() { Position = new Vector2Int(x, y), Type = TileType.Wall };
                        terrainData[new Vector2Int(x, y)] = TileType.Wall;
                    }
                }
            }
            rooms = roomList;
            paths = pathList;

            ApplyMap();

            var roomTiles = Map.ToArray().Where(tile => tile.IsRoom).ToArray();
            stairPosition = roomTiles.Random().Position;
            spawnPoint = roomTiles.Random().Position;
        }

        public void DeletePath(float deletePercent)
        {
            foreach (var _ in DeletePathSteps(deletePercent)) { }
        }

        /// <summary>
        /// 通路の削除・復元を1件ずつ実行する
        /// 各要素を列挙した時点でマップに反映済みの状態になる
        /// </summary>
        /// <param name="deletePercent">通路の削除率</param>
        /// <returns>実行した操作の説明と、操作対象の通路のタイル座標</returns>
        internal IEnumerable<(string Label, List<Vector2Int> ChangedTiles)> DeletePathSteps(float deletePercent)
        {
            var deletedPath = new List<Path>();
            DeletedPaths = deletedPath;
            foreach (var room in rooms.Where(room => room.ConnectedRooms.Count > 1))
            {
                foreach (var next in room.ConnectedRooms.ToList())
                {
                    var nextRoom = rooms.First(other => other.Id == next);
                    if (room.ConnectedRooms.Count <= 1 || nextRoom.ConnectedRooms.Count <= 1) continue;
                    if (UnityEngine.Random.Range(0f, 1f) > deletePercent) continue;
                    var target = paths.FirstOrDefault(path
                        => (path.FromRoomId == room.Id && path.ToRoomId == next)
                        || (path.FromRoomId == next && path.ToRoomId == room.Id));
                    if (target == null) continue;
                    room.RemovePath(next);
                    nextRoom.RemovePath(room.Id);
                    paths.Remove(target);
                    deletedPath.Add(target);
                    ApplyMap();
                    yield return ($"通路削除 部屋{room.Id} -> 部屋{next}", target.PathPositionList.ToList());
                }
            }
            var retryCount = 0;
            while (true)
            {
                var closedRooms = BackTracking.FindIsolatedRoom(rooms, paths);
                if (closedRooms == null) break;
                foreach (var room in closedRooms)
                {
                    var target = deletedPath.FirstOrDefault(path => path.FromRoomId == room.Id || path.ToRoomId == room.Id);
                    if (target == null) continue;
                    var from = rooms.First(other => other.Id == target.FromRoomId);
                    var to = rooms.First(other => other.Id == target.ToRoomId);
                    from.AddPath(to.Id, target, target.From);
                    to.AddPath(from.Id, target, target.To);
                    paths.Add(target);
                    deletedPath.Remove(target);
                    ApplyMap();
                    yield return ($"通路復元 部屋{target.FromRoomId} -> 部屋{target.ToRoomId}", target.PathPositionList.ToList());
                    break;
                }
                retryCount++;
                if (retryCount >= 100)
                {
                    Debug.LogError("通路の復元回数が上限を超えました");
                    break;
                }
            }
            ApplyMap();
        }

        private void ApplyMap()
        {
            // 削除した通路の適用
            if (DeletedPaths != null)
            {
                foreach (var path in DeletedPaths)
                {
                    foreach (var position in path.PathPositionList)
                    {
                        var tile = Map[position.x, position.y];
                        if (tile.Type == TileType.Room) continue;
                        tile.Type = terrainData[position];
                        tile.IsDeleted = true;
                    }
                }
            }

            // 通路の適用
            foreach (var path in paths)
            {
                foreach (var position in path.PathPositionList)
                {
                    Map[position.x, position.y].Position = position;
                    Map[position.x, position.y].Type = TileType.Path;
                    Map[position.x, position.y].Id = path.Id;
                    Map[position.x, position.y].IsDeleted = false;
                }
            }

            // 部屋の適用
            foreach (var room in rooms)
            {
                var x = room.x;
                var y = room.y;
                var roomWidth = room.width;
                var roomHeight = room.height;
                for (var row = y; row < y + roomHeight; row++)
                {
                    for (var column = x; column < x + roomWidth; column++)
                    {
                        Map[column, row].Position = new Vector2Int(column, row);
                        Map[column, row].Type = TileType.Room;
                        Map[column, row].Id = room.Id;
                    }
                }
            }
        }

        public TileData GetTile(int x, int y)
        {
            if (x < 0 || x >= Size.x || y < 0 || y >= Size.y) return null;
            return Map[x, y];
        }
    }
}
