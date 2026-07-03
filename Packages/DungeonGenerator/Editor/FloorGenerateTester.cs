using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Xeon.Dungeon;

public class FloorGenerateTester : EditorWindow
{
    [MenuItem("Debug/DungeonGenerator/フロア生成テスト")]
    public static void Open() => GetWindow<FloorGenerateTester>("フロア生成テスト");

    private Vector2Int size = Vector2Int.one * 20;
    private int maxRoomCount = 3;
    private float deletePathPercent = 0f;
    private float weatheringRate = 0.1f;
    private FloorData floorData;
    private List<GenerationStep> steps;
    private int stepIndex;

    private float cellSize = 10f;
    private Vector2 origin = new Vector2(10, 150);

    private void OnGUI()
    {
        size = EditorGUILayout.Vector2IntField("フロアサイズ", size);
        using (new EditorGUILayout.HorizontalScope())
        {
            maxRoomCount = EditorGUILayout.IntSlider("最大部屋数", maxRoomCount, 2, 999);
            deletePathPercent = EditorGUILayout.Slider("通路削除率", deletePathPercent, 0f, 1f);
            weatheringRate = EditorGUILayout.Slider("地形風化率", weatheringRate, 0f, 1f);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("生成"))
            {
                floorData = DungeonGenerator.GenerateFloor(size.x, size.y, maxRoomCount, deletePathPercent, weatheringRate);
                steps = null;
            }
            if (GUILayout.Button("ステップ生成"))
            {
                steps = DungeonGenerator.GenerateFloorSteps(size.x, size.y, maxRoomCount, deletePathPercent, weatheringRate).ToList();
                stepIndex = 0;
                floorData = steps.Last().FloorData;
            }
        }

        if (steps != null)
            DrawStepMode();
        else if (floorData != null && floorData.Map != null)
            DrawTiles(floorData.Size, (x, y) => GetTileColor(floorData.Map[x, y].Type));
    }

    /// <summary>
    /// 生成プロセスのステップ表示
    /// </summary>
    private void DrawStepMode()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(stepIndex <= 0))
            {
                if (GUILayout.Button("|<", GUILayout.Width(30))) stepIndex = 0;
                if (GUILayout.Button("前へ")) stepIndex--;
            }
            using (new EditorGUI.DisabledScope(stepIndex >= steps.Count - 1))
            {
                if (GUILayout.Button("次へ")) stepIndex++;
                if (GUILayout.Button(">|", GUILayout.Width(30))) stepIndex = steps.Count - 1;
            }
        }
        stepIndex = EditorGUILayout.IntSlider("ステップ", stepIndex, 0, steps.Count - 1);
        var step = steps[stepIndex];
        EditorGUILayout.LabelField($"[{stepIndex + 1}/{steps.Count}] {step.Label}", EditorStyles.boldLabel);
        DrawViewSetting();

        if (step.Map != null)
        {
            DrawTiles(step.Size, (x, y) => GetTileColor(step.Map[x, y]));
            // 削除・復元された通路を強調表示する
            foreach (var position in step.HighlightTiles)
                DrawCell(position.x, position.y, new Color(1f, 1f, 0f, 0.4f));
        }
        else
        {
            DrawGeometricStep(step);
        }
    }

    /// <summary>
    /// マップ適用前のステップを矩形・座標リストから描画する
    /// </summary>
    private void DrawGeometricStep(GenerationStep step)
    {
        DrawTiles(step.Size, (_, _) => new Color(0.15f, 0.15f, 0.15f));
        foreach (var room in step.Rooms)
            DrawRect(room, new Color(0.1f, 0.6f, 0.1f));
        foreach (var path in step.PathTiles)
        {
            foreach (var position in path)
                DrawCell(position.x, position.y, Color.gray);
        }
        foreach (var position in step.HighlightTiles)
            DrawCell(position.x, position.y, Color.yellow);
        foreach (var area in step.Areas)
            DrawRectOutline(area, new Color(1f, 0.5f, 0f, 0.8f));
        foreach (var rect in step.HighlightRects)
            DrawRectOutline(rect, Color.yellow);
    }

    private void DrawViewSetting()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            cellSize = EditorGUILayout.Slider("セルサイズ", cellSize, 1f, 50f);
            origin = EditorGUILayout.Vector2Field("プレビューの左上の座標", origin);
        }
    }

    private void DrawTiles(Vector2Int mapSize, System.Func<int, int, Color> colorSelector)
    {
        for (var x = 0; x < mapSize.x; x++)
        {
            for (var y = 0; y < mapSize.y; y++)
                DrawCell(x, y, colorSelector(x, y));
        }
    }

    private void DrawCell(int x, int y, Color color)
    {
        var rect = new Rect(new Vector2(x, y) * cellSize + origin, new Vector2(cellSize, cellSize));
        EditorGUI.DrawRect(rect, color);
    }

    private void DrawRect(RectInt area, Color color)
    {
        var rect = new Rect(new Vector2(area.x, area.y) * cellSize + origin, new Vector2(area.width, area.height) * cellSize);
        EditorGUI.DrawRect(rect, color);
    }

    private void DrawRectOutline(RectInt area, Color color)
    {
        var rect = new Rect(new Vector2(area.x, area.y) * cellSize + origin, new Vector2(area.width, area.height) * cellSize);
        var thickness = Mathf.Max(1f, cellSize * 0.15f);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private Color GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Wall => Color.gray,
            TileType.Water => Color.blue,
            TileType.Hole => Color.black,
            TileType.Path => new Color(0.7f, 0.7f, 0.5f),
            _ => Color.green,
        };
    }
}
