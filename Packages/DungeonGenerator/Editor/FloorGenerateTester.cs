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

    private float cellSize = 10f;
    private Vector2 origin = new Vector2(10, 125);

    private void OnGUI()
    {
        size = EditorGUILayout.Vector2IntField("フロアサイズ", size);
        using (new EditorGUILayout.HorizontalScope())
        {
            maxRoomCount = EditorGUILayout.IntSlider("最大部屋数", maxRoomCount, 2, 999);
            deletePathPercent = EditorGUILayout.Slider("通路削除率", deletePathPercent, 0f, 1f);
            weatheringRate = EditorGUILayout.Slider("地形風化率", weatheringRate, 0f, 1f);
        }

        if (GUILayout.Button("生成"))
        {
            floorData = DungeonGenerator.GenerateFloor(size.x, size.y, maxRoomCount, deletePathPercent, weatheringRate);
            Debug.Log("マップ生成");
        }
        if (floorData == null || floorData.Map == null) return;

        using (new EditorGUILayout.HorizontalScope())
        {
            cellSize = EditorGUILayout.Slider("セルサイズ", cellSize, 1f, 50f);
            origin = EditorGUILayout.Vector2Field("プレビューの左上の座標", origin);
        }

        var rect = new Rect();
        rect.size = new Vector2(cellSize, cellSize);
        for (var x = 0; x < floorData.Size.x; x++)
        {
            for (var y = 0; y < floorData.Size.y; y++)
            {
                rect.position = new Vector2(x, y) * cellSize + origin;
                EditorGUI.DrawRect(rect, GetColor(floorData.Map[x, y]));
            }
        }
    }

    private Color GetColor(TileData data)
    {
        return data.Type switch
        {
            TileType.Wall => Color.gray,
            TileType.Water => Color.blue,
            TileType.Hole => Color.black,
            _ => Color.green,
        };
    }
}
