using System.Collections.Generic;
using UnityEngine;

namespace Xeon.Dungeon
{
    /// <summary>
    /// エリアを表すクラス
    /// </summary>
    internal class Area
    {
        private struct AdjacentData
        {
            public Area area;
            public bool isHorizontal;
        }

        private const int RoomSizeMin = 5;
        private const int AreaSizeMin = RoomSizeMin + 4;

        public static int MaxRoomNum { get; set; } = 3;
        public static int Count { get; set; } = 0;
        public int x { get; private set; }
        public int y { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }
        private Area[] child = new Area[2];
        public Room Room { get; private set; }
        public int Id { get; private set; }

        /// <summary>
        /// 隣接エリアデータ
        /// </summary>
        private List<AdjacentData> adjacent;

        public Area(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            Id = Count;
            Count++;
        }

        /// <summary>
        /// エリアを分割する
        /// </summary>
        public void Split()
        {
            if (width < AreaSizeMin && height < AreaSizeMin) return;
            if (Count > MaxRoomNum) return;

            var horizontal = Random.Range(0, 2) == 1 && height >= AreaSizeMin * 2;
            if (horizontal)
            {
                if (width < AreaSizeMin * 2) return;
                var dividePoint = Random.Range(AreaSizeMin, width - AreaSizeMin);
                child[0] = new Area(x, y, dividePoint, height);
                child[1] = new Area(x + dividePoint, y, width - dividePoint, height);
            }
            else
            {
                if (height < AreaSizeMin * 2) return;
                var dividePoint = Random.Range(AreaSizeMin, height - AreaSizeMin);
                child[0] = new Area(x, y, width, dividePoint);
                child[1] = new Area(x, y + dividePoint, width, height - dividePoint);
            }
            child[0].Split();
            child[1].Split();
        }

        /// <summary>
        /// 再帰的にすべてのステータスを表示する
        /// </summary>
        public void RecursivePrintStatus()
        {
            if (child[0] == null && child[1] == null) return;
            child[0]?.RecursivePrintStatus();
            child[1]?.RecursivePrintStatus();
        }

        /// <summary>
        /// 再帰的に最下層にあるすべてのエリアを取得する
        /// </summary>
        /// <param name="result"></param>
        public void RecursiveGetArea(ref List<Area> result)
        {
            if (child[0] == null && child[1] == null)
            {
                result.Add(this);
                return;
            }
            child[0]?.RecursiveGetArea(ref result);
            child[1]?.RecursiveGetArea(ref result);
        }

        /// <summary>
        /// 再帰的に最下層にあるすべての部屋を取得する
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public List<Room> RecursiveGetRoom(ref List<Room> result)
        {
            if (Room != null)
            {
                result.Add(Room);
            }
            else
            {
                child[0]?.RecursiveGetRoom(ref result);
                child[1]?.RecursiveGetRoom(ref result);
            }
            return result;
        }

        /// <summary>
        /// 再帰的に部屋を作成する
        /// </summary>
        public void RecursiveCrateRoom()
        {
            if (child[0] == null && child[1] == null)
            {
                var width = Mathf.Max(RoomSizeMin, Random.Range(RoomSizeMin, this.width - 4));
                var height = Mathf.Max(RoomSizeMin, Random.Range(RoomSizeMin, this.height - 4));
                var x = Random.Range(2, this.width - width - 2) + this.x;
                var y = Random.Range(2, this.height - height - 2) + this.y;
                Room = new Room(Id, x, y, width, height);
                return;
            }
            child[0]?.RecursiveCrateRoom();
            child[1]?.RecursiveCrateRoom();
        }

        /// <summary>
        /// 再帰的に通路を作成する
        /// </summary>
        /// <param name="pathList"></param>
        /// <param name="pathIndex"></param>
        public void RecursiveCreatePath(ref List<Path> pathList, ref int pathIndex)
        {
            if (child[0] != null || child[1] != null)
            {
                child[0]?.RecursiveCreatePath(ref pathList, ref pathIndex);
                child[1]?.RecursiveCreatePath(ref pathList, ref pathIndex);
                return;
            }

            for (var index = 0; index < adjacent.Count; index++)
            {
                var toId = adjacent[index].area.Id;
                if (Room.CheckPathBeing(toId))
                {
                    if (!adjacent[index].area.Room.CheckPathBeing(Id))
                        Debug.Log($"エラー 片方の部屋にしか道が登録されていません！ fromArea:{Id} toArea:{toId}");
                    else
                        continue;
                }
                var fromRoom = Room;
                var toRoom = adjacent[index].area.Room;
                var path = adjacent[index].isHorizontal
                    ? CreateHorizontalPath(pathIndex, index, toId, fromRoom, toRoom)
                    : CreateVerticalPath(pathIndex, index, toId, fromRoom, toRoom);
                pathIndex++;

                fromRoom.AddPath(toId, path, path.From);
                toRoom.AddPath(Id, path, path.To);
                pathList.Add(path);
            }
        }

        /// <summary>
        /// 水平方向の通路作成
        /// </summary>
        /// <param name="pathIndex"></param>
        /// <param name="index"></param>
        /// <param name="toId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private Path CreateHorizontalPath(int pathIndex, int index, int toId, Room from, Room to)
        {
            var path = new Path() { Id = pathIndex };
            var fromPosition = Vector2Int.zero;
            var toPosition = Vector2Int.zero;

            if (x > adjacent[index].area.x)
            {
                fromPosition.x = from.x;
                toPosition.x = to.x + to.width;
                path.Dir = Direction.Left;
            }
            else
            {
                fromPosition.x = from.x + from.width;
                toPosition.x = to.x;
                path.Dir = Direction.Right;
            }

            fromPosition.y = Random.Range(from.y, from.y + from.height);
            toPosition.y = Random.Range(to.y, to.y + to.height);
            path.SetIds(Id, toId);
            path.CreatePositionList(fromPosition, toPosition, this);
            return path;
        }

        /// <summary>
        /// 垂直方向の通路を作成する
        /// </summary>
        /// <param name="pathIndex"></param>
        /// <param name="index"></param>
        /// <param name="toId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private Path CreateVerticalPath(int pathIndex, int index, int toId, Room from, Room to)
        {
            var path = new Path() { Id = pathIndex };
            var fromPosition = Vector2Int.zero;
            var toPosition = Vector2Int.zero;

            if (y > adjacent[index].area.y)
            {
                fromPosition.y = from.y;
                toPosition.y = to.y + to.height;
                path.Dir = Direction.Up;
            }
            else
            {
                fromPosition.y = from.y + from.height;
                toPosition.y = to.y;
                path.Dir = Direction.Down;
            }

            fromPosition.x = Random.Range(from.x, from.x + from.width);
            toPosition.x = Random.Range(to.x, to.x + to.width);
            path.SetIds(Id, toId);
            path.CreatePositionList(fromPosition, toPosition, this);
            return path;
        }

        /// <summary>
        /// 隣接するエリアのリストを作成する
        /// </summary>
        /// <param name="list"></param>
        public void CreateAdjacentList(List<Area> list)
        {
            adjacent = new List<AdjacentData>();
            for (var index = 0; index < list.Count; index++)
            {
                if (list[index] == this) continue;
                var data = new AdjacentData();
                if (list[index].x + list[index].width == x || x + width == list[index].x)
                {
                    if ((y >= list[index].y && y <= list[index].y + list[index].height) || (list[index].y >= y && list[index].y <= y + height))
                    {
                        data.area = list[index];
                        data.isHorizontal = true;
                        adjacent.Add(data);
                    }
                }
                else if (list[index].y + list[index].height == y || y + height == list[index].y)
                {
                    if ((x >= list[index].x && x <= list[index].x + list[index].width) || (list[index].x >= x && list[index].x <= x + width))
                    {
                        data.area = list[index];
                        data.isHorizontal = false;
                        adjacent.Add(data);
                    }
                }
            }
        }
    }
}
