using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.Dungeon
{
    /// <summary>
    /// エリア分割全体で共有する状態
    /// </summary>
    internal class AreaContext
    {
        public int Count { get; set; } = 0;
        public int MaxRoomNum { get; set; } = 3;
        /// <summary>
        /// 実行された分割の記録(分割されたエリア, 分割後の1つ目, 2つ目)
        /// </summary>
        public List<(RectInt parent, RectInt first, RectInt second)> SplitEvents { get; } = new();
    }

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

        public int x { get; private set; }
        public int y { get; private set; }
        public int width { get; private set; }
        public int height { get; private set; }
        public RectInt Rect => new RectInt(x, y, width, height);
        private Area[] child = new Area[2];
        private AreaContext context;
        public Room Room { get; private set; }
        public int Id { get; private set; }

        /// <summary>
        /// 隣接エリアデータ
        /// </summary>
        private List<AdjacentData> adjacent;

        public Area(int x, int y, int width, int height, AreaContext context)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.context = context;
            Id = context.Count;
            context.Count++;
        }

        /// <summary>
        /// エリアを分割する
        /// </summary>
        public void Split()
        {
            if (width < AreaSizeMin && height < AreaSizeMin) return;
            if (context.Count > context.MaxRoomNum) return;

            var horizontal = Random.Range(0, 2) == 1 && height >= AreaSizeMin * 2;
            if (horizontal)
            {
                if (width < AreaSizeMin * 2) return;
                var dividePoint = Random.Range(AreaSizeMin, width - AreaSizeMin);
                child[0] = new Area(x, y, dividePoint, height, context);
                child[1] = new Area(x + dividePoint, y, width - dividePoint, height, context);
            }
            else
            {
                if (height < AreaSizeMin * 2) return;
                var dividePoint = Random.Range(AreaSizeMin, height - AreaSizeMin);
                child[0] = new Area(x, y, width, dividePoint, context);
                child[1] = new Area(x, y + dividePoint, width, height - dividePoint, context);
            }
            context.SplitEvents.Add((Rect, child[0].Rect, child[1].Rect));
            child[0].Split();
            child[1].Split();
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
        public void RecursiveCreateRoom()
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
            child[0]?.RecursiveCreateRoom();
            child[1]?.RecursiveCreateRoom();
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

            foreach (var data in adjacent)
            {
                var toArea = data.area;
                if (Room.CheckPathBeing(toArea.Id))
                {
                    if (!toArea.Room.CheckPathBeing(Id))
                        Debug.LogError($"エラー 片方の部屋にしか道が登録されていません! fromArea:{Id} toArea:{toArea.Id}");
                    else
                        continue;
                }
                var path = data.isHorizontal
                    ? CreateHorizontalPath(pathIndex, toArea)
                    : CreateVerticalPath(pathIndex, toArea);
                pathIndex++;

                Room.AddPath(toArea.Id, path, path.From);
                toArea.Room.AddPath(Id, path, path.To);
                pathList.Add(path);
            }
        }

        /// <summary>
        /// 水平方向の通路作成
        /// </summary>
        /// <param name="pathIndex"></param>
        /// <param name="toArea"></param>
        /// <returns></returns>
        private Path CreateHorizontalPath(int pathIndex, Area toArea)
        {
            var path = new Path() { Id = pathIndex };
            var from = Room;
            var to = toArea.Room;
            var fromPosition = Vector2Int.zero;
            var toPosition = Vector2Int.zero;
            int border;

            if (x > toArea.x)
            {
                fromPosition.x = from.x;
                toPosition.x = to.x + to.width;
                path.Dir = Direction.Left;
                border = x;
            }
            else
            {
                fromPosition.x = from.x + from.width;
                toPosition.x = to.x;
                path.Dir = Direction.Right;
                border = x + width;
            }

            fromPosition.y = PickDoorPosition(from.y, from.height, GetUsedDoorPositions(from, fromPosition.x, isHorizontal: true));
            toPosition.y = PickDoorPosition(to.y, to.height, GetUsedDoorPositions(to, toPosition.x, isHorizontal: true));
            path.SetIds(Id, toArea.Id);
            path.CreatePositionList(fromPosition, toPosition, PickBendPosition(border));
            return path;
        }

        /// <summary>
        /// 垂直方向の通路を作成する
        /// </summary>
        /// <param name="pathIndex"></param>
        /// <param name="toArea"></param>
        /// <returns></returns>
        private Path CreateVerticalPath(int pathIndex, Area toArea)
        {
            var path = new Path() { Id = pathIndex };
            var from = Room;
            var to = toArea.Room;
            var fromPosition = Vector2Int.zero;
            var toPosition = Vector2Int.zero;
            int border;

            if (y > toArea.y)
            {
                fromPosition.y = from.y;
                toPosition.y = to.y + to.height;
                path.Dir = Direction.Up;
                border = y;
            }
            else
            {
                fromPosition.y = from.y + from.height;
                toPosition.y = to.y;
                path.Dir = Direction.Down;
                border = y + height;
            }

            fromPosition.x = PickDoorPosition(from.x, from.width, GetUsedDoorPositions(from, fromPosition.y, isHorizontal: false));
            toPosition.x = PickDoorPosition(to.x, to.width, GetUsedDoorPositions(to, toPosition.y, isHorizontal: false));
            path.SetIds(Id, toArea.Id);
            path.CreatePositionList(fromPosition, toPosition, PickBendPosition(border));
            return path;
        }

        /// <summary>
        /// 通路の折れ曲がり位置を選ぶ
        /// 部屋はエリア境界から2タイル以上離れているため、境界±1の範囲なら他の部屋と干渉しない
        /// </summary>
        /// <param name="border">エリア境界の座標</param>
        private static int PickBendPosition(int border) => Random.Range(border - 1, border + 2);

        /// <summary>
        /// 指定した部屋の縁で既に使われている出入口の座標を取得する
        /// </summary>
        /// <param name="room">対象の部屋</param>
        /// <param name="edge">出入口がある縁の座標(水平通路ならX座標、垂直通路ならY座標)</param>
        /// <param name="isHorizontal">水平方向の通路か</param>
        private static IEnumerable<int> GetUsedDoorPositions(Room room, int edge, bool isHorizontal)
        {
            return isHorizontal
                ? room.ConnectedPoint.Values.Where(point => point.x == edge).Select(point => point.y)
                : room.ConnectedPoint.Values.Where(point => point.y == edge).Select(point => point.x);
        }

        /// <summary>
        /// 使用済みの座標を避けて出入口の座標を選ぶ(空きがなければランダム)
        /// </summary>
        /// <param name="min">部屋の縁の開始座標</param>
        /// <param name="length">部屋の縁の長さ</param>
        /// <param name="used">使用済みの座標</param>
        private static int PickDoorPosition(int min, int length, IEnumerable<int> used)
        {
            var candidates = Enumerable.Range(min, length).Except(used).ToList();
            if (candidates.Count <= 0)
                return Random.Range(min, min + length);
            return candidates[Random.Range(0, candidates.Count)];
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
