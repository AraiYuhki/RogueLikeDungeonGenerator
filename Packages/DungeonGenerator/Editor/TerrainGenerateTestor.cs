using UnityEditor;
using UnityEngine;
using Xeon.Dungeon;
using Xeon.Utility;

public class TerrainGenerateTestor : EditorWindow
{
    [MenuItem("Debug/DungeonGenerator/地形生成テスト")]
    public static void Open() => GetWindow<TerrainGenerateTestor>("地形生成テスト");

    private Vector2Int size = Vector2Int.one * 10;
    private float weatheringRate = 0.1f;
    private TileType[,] map = null;

    private Vector2 offset = new Vector2(0, 100f);

    private void OnGUI()
    {
        size = EditorGUILayout.Vector2IntField("マップサイズ", size);
        weatheringRate = EditorGUILayout.Slider(weatheringRate, 0f, 10f);
        if (GUILayout.Button("実行"))
        {
            map = TerrainGenerator.Generate(size, weatheringRate, TileType.Wall, TileType.Hole, TileType.Water);
        }

        if (map == null) return;
        var rect = new Rect(offset, Vector2.one * 10f);
        for (var x = 0; x < map.GetLength(0); x++)
        {
            for (var y = 0; y < map.GetLength(1); y++)
            {
                rect.x = offset.x + x * 10;
                rect.y = offset.y + y * 10;
                EditorGUI.DrawRect(rect, GetColor(map[x, y]));
            }
        }
    }

    private Color GetColor(TileType type)
    {
        switch(type)
        {
            case TileType.Wall:
                return Color.gray;
            case TileType.Hole:
                return Color.red;
            case TileType.Water:
                return Color.cyan;
            default:
                return Color.white;
        }
    }

}
