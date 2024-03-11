using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.Dungeon;
using Xeon.RootFinder;

public class RootfinderTester : EditorWindow
{
    private enum ViewMode
    {
        DijkstraGraph,
        TileMap,
    }

    private FloorData floorData;
    private Vector2Int size = new Vector2Int(20, 20);
    private int roomCount = 4;
    private float deletePercent = 30f;

    private int startId = 0;
    private int endId = 0;

    private ViewMode viewMode;
    private bool visibleDeleted = false;

    private Vector3 origin = new Vector2(0, 250f);
    private List<Vector2Int> root = new List<Vector2Int>();

    private Vector2Int startPoint;
    private Vector2Int endPoint;

    private Dijkstra dijkstra;

    [MenuItem("Debug/DungeonGenerator/経路探索テスト")]
    public static void Open() => GetWindow<RootfinderTester>("経路探索テスト");

    public void OnGUI()
    {
        if (GUILayout.Button("読み込み"))
            LoadFile();

        size = EditorGUILayout.Vector2IntField("マップサイズ", size);
        using (new EditorGUILayout.HorizontalScope())
        {
            size.x = Mathf.Max(size.x, 10);
            size.y = Mathf.Max(size.y, 10);
            roomCount = EditorGUILayout.IntField("部屋数", roomCount);
            deletePercent = EditorGUILayout.Slider("通路削除確立", deletePercent, 0f, 1f);
        }

        if (GUILayout.Button("生成")) Generate();

        if (floorData == null || dijkstra == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            var noeIds = dijkstra.Nodes.Keys.ToArray();
            startId = EditorGUILayout.IntPopup("開始地点", startId, noeIds.Select(id => id.ToString()).ToArray(), noeIds);
            endId = EditorGUILayout.IntPopup("終了地点", endId, noeIds.Select(id => id.ToString()).ToArray(), noeIds);
            if (GUILayout.Button("経路探索"))
            {
                var root = dijkstra.GetRoot(startId, endId);
                Debug.Log(string.Join("->", root));
            }
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            startPoint = EditorGUILayout.Vector2IntField("開始地点", startPoint);
            endPoint = EditorGUILayout.Vector2IntField("終了地点", endPoint);;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("チェックポイントリスト作成"))
                CreateCheckPointList(startPoint, endPoint);
            if (GUILayout.Button("ルートを破棄")) root.Clear();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            viewMode = (ViewMode)EditorGUILayout.EnumPopup("表示モード", viewMode);
            visibleDeleted = EditorGUILayout.Toggle("削除した通路を表示", visibleDeleted);
        }

        try
        {
            switch (viewMode)
            {
                case ViewMode.DijkstraGraph:
                    DrawGraph();
                    break;
                case ViewMode.TileMap:
                    DrawFloorPreview();
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            floorData = null;
        }
    }

    private void LoadFile()
    {
        var filePath = EditorUtility.OpenFilePanelWithFilters("フロア情報選択", FloorUtil.SavePath, new string[] { "フロア情報", "flr" });
        if (string.IsNullOrEmpty(filePath)) return;
        floorData = FloorUtil.Deserialize(filePath);
        dijkstra = new Dijkstra(floorData);
        root?.Clear();
    }

    private void Generate()
    {
        floorData = DungeonGenerator.GenerateFloor(size.x, size.y, roomCount, deletePercent, 0f);
        dijkstra = new Dijkstra(floorData);
        root?.Clear();
    }
    private void DrawGraph()
    {
        Handles.color = Color.cyan;
        var style = new GUIStyle();
        style.normal.textColor = Color.cyan;
        foreach (var node in dijkstra.Nodes.Values)
        {
            var position = node.Position() * 10f + origin;
            Handles.DrawSolidDisc(position, Vector3.forward, 5f);
            Handles.Label(position - Vector3.up * 15f, node.Id.ToString(), style);
        }

        Handles.color = Color.white;

        var pathList = new List<(int, int)>();
        foreach (var node in dijkstra.Nodes.Values)
        {
            foreach (var pair in node.ConnectedCosts)
            {
                // 同じ線は一度だけ描画する
                if (pathList.Contains((node.Id, pair.Key)) || pathList.Contains((pair.Key, node.Id))) continue;
                var toNode = dijkstra.Nodes[pair.Key];
                var cost = pair.Value;
                pathList.Add((node.Id, toNode.Id));

                Handles.DrawLine(node.Position() * 10f + origin, toNode.Position() * 10f + origin);
                Handles.Label(((node.Position() + toNode.Position()) * 0.5f) * 10f + origin, cost.ToString());
            }
        }
    }

    private void DrawFloorPreview()
    {
        if (floorData == null) return;
        var rect = new Rect();
        rect.height = rect.width = 9;
        var origin = new Vector2(this.origin.x, this.origin.y);
        for (var x = 0; x < floorData.Size.x; x++)
        {
            for (var y = 0; y < floorData.Size.y; y++)
            {
                rect.position = new Vector2(x * 10, y * 10) + origin;
                var color = NodeColor(floorData.Map[x, y]);
                if (startPoint.x == x && startPoint.y == y)
                    color = Color.cyan;
                else if (endPoint.x == x && endPoint.y == y)
                    color = Color.yellow;
                else if (root.Any(point => point.x == x && point.y == y))
                    color = Color.red;
                EditorGUI.DrawRect(rect, color);
            }
        }
    }

    private void CreateCheckPointList(Vector2Int startPosition, Vector2Int endPosition)
    {
        var rootFinder = new Dijkstra(floorData);
        root = rootFinder.GetCheckpoints(startPosition, endPosition);
        root = rootFinder.GetRoot(startPosition, endPosition, root);
    }

    private Color NodeColor(TileData tile)
    {
        if (floorData.StairPosition == tile.Position)
            return Color.magenta;
        else if (floorData.SpawnPoint == tile.Position)
            return Color.green;
        if (visibleDeleted && tile.IsDeleted)
            return Color.red;
        switch (tile.Type)
        {
            case TileType.Wall:
                return Color.gray;
            case TileType.Water:
                return Color.cyan;
            case TileType.Hole:
                return Color.black;
            default:
                return Color.white;
        }
    }
}
