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

        internal FloorData(int width, int height, List<Room> roomList, List<Path> pathList, float weateringRate = 0.2f, params TileType[] wallTypes)
            => Initialize(new Vector2Int(width, height), roomList, pathList, weateringRate, wallTypes);

        private void Initialize(Vector2Int size, List<Room> roomList, List<Path> pathList, float weateringRate = 0.2f, params TileType[] wallTypes)
        {
            Map = new TileData[size.x, size.y];
            terrainData.Clear();
            if (weateringRate > 0f)
            {
                if (wallTypes.Length <= 0)
                    wallTypes = new TileType[] { TileType.Wall, TileType.Water, TileType.Hole };
                var terrain = TerrainGenerator.Generate(size, weateringRate, wallTypes);
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
            var deletedPath = new List<Path>();
            foreach (var room in rooms.Where(room => room.ConnectedRooms.Count > 1))
            {
                foreach (var next in room.ConnectedRooms.ToList())
                {
                    var nextRoom = rooms.First(room => room.Id == next);
                    if (room.ConnectedRooms.Count <= 1 || nextRoom.ConnectedRooms.Count <= 1) continue;
                    var random = UnityEngine.Random.Range(0f, 1f);
                    if (random > deletePercent) continue;
                    room.RemovePath(next);
                    nextRoom.RemovePath(room.Id);
                    var target = paths.FirstOrDefault(path
                        => (path.FromRoomId == room.Id && path.ToRoomId == next)
                        || (path.FromRoomId == next && path.ToRoomId == room.Id));
                    paths.Remove(target);
                    deletedPath.Add(target);
                    Debug.Log($"delete path {room.Id} -> {next}");
                }
            }
            var closedRooms = new List<Room>();
            var retryCount = 0;
            while (true)
            {
                closedRooms = BackTracking.FindIsolatedRoom(rooms, paths);
                if (closedRooms == null) break;
                foreach (var room in closedRooms)
                {
                    var target = deletedPath.FirstOrDefault(path => path.FromRoomId == room.Id || path.ToRoomId == room.Id);
                    if (target != null)
                    {
                        var from = rooms.First(room => room.Id == target.FromRoomId);
                        var to = rooms.First(room => room.Id == target.ToRoomId);
                        from.AddPath(to.Id, target, target.From);
                        to.AddPath(from.Id, target, target.To);
                        paths.Add(target);
                        deletedPath.Remove(target);
                        Debug.Log($"restore path {target.FromRoomId} -> {target.ToRoomId}");
                        break;
                    }
                }
                retryCount++;
                if (retryCount >= 100)
                {
                    Debug.LogError("Over retry counts");
                    break;
                }
            }
            DeletedPaths = deletedPath;
            ApplyMap();
        }

        private void ApplyMap()
        {
            // 削除した通路の適用
            if (DeletedPaths != null)
            {
                foreach (var path in DeletedPaths.Where(path => path != null))
                {
                    Debug.Log($"deleted path {path.FromRoomId} -> {path.ToRoomId}");
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
