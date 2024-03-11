using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Xeon.Dungeon;
using Xeon.RootFinder;
using Xeon.Utility;

public class AStarTester : EditorWindow
{
    private class UnitContainer : IUnitContainer
    {
        private ReorderableList listView;
        private Vector2 scrollPosition = Vector2.zero;
        public List<Vector2Int> units = new();

        public UnitContainer()
        {
            listView = new ReorderableList(units, typeof(Vector2Int));
            listView.drawElementCallback = (rect, index, isActive, isFocus) =>
            {
                units[index] = EditorGUI.Vector2IntField(rect, "座標", units[index]);
            };
            listView.elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 2f;
        }

        public bool ExistsUnit(Vector2Int position)
        {
            return units.Contains(position);
        }
        public void Draw()
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                listView.DoLayoutList();
                scrollPosition = scrollView.scrollPosition;
            }
        }
    }
    private FloorData floorData;
    private Vector2Int size = new Vector2Int(20, 20);
    private int roomCount = 4;
    private float deletePercent = 0.3f;

    private List<Vector2Int> root = new();
    private HashSet<(int x, int y)> rootTiles = new();
    private Vector2Int startPoint;
    private Vector2Int endPoint;
    private float cellSize = 20f;

    private UnitContainer unitContainer;

    private AStar astar;

    [MenuItem("Debug/DungeonGenerator/A*テスター")]
    public static void Open() => GetWindow<AStarTester>("A*テスト");

    public void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope())
            {
                DrawGenerateSetting();
                if (floorData == null || astar == null) return;
                DrawPathFinderSetting();
                cellSize = EditorGUILayout.Slider("セルサイズ", cellSize, 10f, 100f);
            }
            if (unitContainer == null)
                unitContainer = new UnitContainer();
            unitContainer.Draw();
        }
        DrawFloorPreview();
    }

    private void DrawGenerateSetting()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            size = EditorGUILayout.Vector2IntField("フロアサイズ", size);
            using (new EditorGUILayout.HorizontalScope())
            {
                roomCount = EditorGUILayout.IntField("部屋数", roomCount);
                deletePercent = EditorGUILayout.Slider("通路削除率", deletePercent, 0f, 1f);
            }
            if (GUILayout.Button("生成")) Generate();
        }
    }

    private void DrawPathFinderSetting()
    {
        startPoint = EditorGUILayout.Vector2IntField("開始地点", startPoint);
        endPoint = EditorGUILayout.Vector2IntField("到達地点", endPoint);
        if (GUILayout.Button("経路探索"))
        {
            astar.Setup(floorData, unitContainer);
            root = astar.FindRoot(startPoint, endPoint);
            rootTiles = root.Select(tile => (tile.x, tile.y)).ToHashSet();
        }
    }

    private void Generate()
    {
        floorData = DungeonGenerator.GenerateFloor(size.x, size.y, roomCount, deletePercent, 0f);
        astar = new AStar(floorData, unitContainer);
        if (root == null) root = new();
        root.Clear();
    }

    private void DrawFloorPreview()
    {
        if (floorData == null) return;
        var rect = new Rect();
        rect.height = rect.width = cellSize - 1f;
        var origin = new Vector2(0, 210f);
        for (var x = 0; x < floorData.Size.x; x++)
        {
            for (var y = 0; y < floorData.Size.y; y++)
            {
                rect.position = new Vector2(x * cellSize, y * cellSize) + origin;
                var tile = floorData.Map[x, y];
                var color = TileColor(tile);
                EditorGUI.DrawRect(rect, color);
                if (color == Color.cyan)
                {
                    EditorGUI.LabelField(rect, rootTiles.IndexOf(pos => pos.x == tile.Position.x && pos.y == tile.Position.y).ToString());
                }
            }
        }
    }

    private Color TileColor(TileData tile)
    {
        if (startPoint.x == tile.Position.x && startPoint.y == tile.Position.y)
            return Color.blue;
        else if (endPoint.x == tile.Position.x && endPoint.y == tile.Position.y)
            return Color.gray;
        if (unitContainer.ExistsUnit(tile.Position))
            return Color.red;

        if (floorData.StairPosition == tile.Position)
            return Color.magenta;
        else if (floorData.SpawnPoint == tile.Position)
            return Color.green;

        if (tile.Type == TileType.Wall)
            return Color.black;
        if (rootTiles.Contains((tile.Position.x, tile.Position.y)))
            return Color.cyan;
        return Color.white;
    }
}
