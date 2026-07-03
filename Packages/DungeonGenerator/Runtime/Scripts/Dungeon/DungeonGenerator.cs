using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.Dungeon
{
    public class DungeonGenerator
    {
        public static FloorData GenerateFloor(int mapWidth = 20, int mapHeight = 20, int maxRoom = 3, float deletePathPercent = 1f, float weatheringRate = 0.1f)
        {
            FloorData result = null;
            foreach (var step in GenerateFloorSteps(mapWidth, mapHeight, maxRoom, deletePathPercent, weatheringRate))
            {
                if (step.FloorData != null)
                    result = step.FloorData;
            }
            return result;
        }

        /// <summary>
        /// 生成プロセスをステップごとに確認しながらフロアを生成する
        /// 最終ステップのFloorDataに生成結果が設定される
        /// </summary>
        public static IEnumerable<GenerationStep> GenerateFloorSteps(int mapWidth = 20, int mapHeight = 20, int maxRoom = 3, float deletePathPercent = 1f, float weatheringRate = 0.1f)
        {
            var size = new Vector2Int(mapWidth, mapHeight);
            var context = new AreaContext { MaxRoomNum = maxRoom };
            var rootArea = new Area(0, 0, mapWidth, mapHeight, context);
            rootArea.Split();

            // 分割の記録を再生して、1回の分割ごとにステップを作る
            var currentAreas = new List<RectInt> { rootArea.Rect };
            yield return new GenerationStep("初期状態", size) { Areas = currentAreas.ToList() };
            var splitCount = 0;
            foreach (var (parent, first, second) in context.SplitEvents)
            {
                var parentIndex = currentAreas.IndexOf(parent);
                currentAreas.RemoveAt(parentIndex);
                currentAreas.Insert(parentIndex, first);
                currentAreas.Insert(parentIndex + 1, second);
                splitCount++;
                yield return new GenerationStep($"エリア分割 {splitCount}回目", size)
                {
                    Areas = currentAreas.ToList(),
                    HighlightRects = new List<RectInt> { first, second },
                };
            }

            var areaList = new List<Area>();
            rootArea.RecursiveGetArea(ref areaList);
            var areaRects = areaList.Select(area => area.Rect).ToList();

            rootArea.RecursiveCreateRoom();
            var roomList = new List<Room>();
            rootArea.RecursiveGetRoom(ref roomList);
            var roomRects = roomList.Select(room => new RectInt(room.x, room.y, room.width, room.height)).ToList();
            for (var index = 0; index < roomList.Count; index++)
            {
                yield return new GenerationStep($"部屋作成 部屋{roomList[index].Id}", size)
                {
                    Areas = areaRects,
                    Rooms = roomRects.Take(index + 1).ToList(),
                    HighlightRects = new List<RectInt> { roomRects[index] },
                };
            }

            foreach (var area in areaList)
                area.CreateAdjacentList(areaList);

            var pathList = new List<Path>();
            var pathIndex = 1;
            rootArea.RecursiveCreatePath(ref pathList, ref pathIndex);
            for (var index = 0; index < pathList.Count; index++)
            {
                yield return new GenerationStep($"通路作成 部屋{pathList[index].FromRoomId} -> 部屋{pathList[index].ToRoomId}", size)
                {
                    Areas = areaRects,
                    Rooms = roomRects,
                    PathTiles = pathList.Take(index + 1).Select(path => path.PathPositionList.ToList()).ToList(),
                    HighlightTiles = pathList[index].PathPositionList.ToList(),
                };
            }

            var data = new FloorData(mapWidth, mapHeight, roomList, pathList, weatheringRate);
            yield return new GenerationStep("地形生成・マップ適用", size) { Map = SnapshotMap(data) };

            foreach (var (label, changedTiles) in data.DeletePathSteps(deletePathPercent))
            {
                yield return new GenerationStep(label, size)
                {
                    Map = SnapshotMap(data),
                    HighlightTiles = changedTiles,
                };
            }

            yield return new GenerationStep("完成", size) { Map = SnapshotMap(data), FloorData = data };
        }

        private static TileType[,] SnapshotMap(FloorData data)
        {
            var snapshot = new TileType[data.Size.x, data.Size.y];
            for (var x = 0; x < data.Size.x; x++)
                for (var y = 0; y < data.Size.y; y++)
                    snapshot[x, y] = data.Map[x, y].Type;
            return snapshot;
        }
    }
}
