using System.Collections.Generic;
using UnityEngine;

namespace Xeon.Dungeon
{
    /// <summary>
    /// ダンジョン生成プロセスの1ステップ分の状態
    /// マップ適用前のステップは矩形・座標リスト、適用後のステップはタイルのスナップショットを持つ
    /// </summary>
    public class GenerationStep
    {
        /// <summary>
        /// ステップの説明
        /// </summary>
        public string Label { get; }
        /// <summary>
        /// フロアのサイズ
        /// </summary>
        public Vector2Int Size { get; }
        /// <summary>
        /// 分割されたエリアの矩形
        /// </summary>
        public List<RectInt> Areas { get; set; } = new();
        /// <summary>
        /// 部屋の矩形
        /// </summary>
        public List<RectInt> Rooms { get; set; } = new();
        /// <summary>
        /// 作成済みの通路のタイル座標
        /// </summary>
        public List<List<Vector2Int>> PathTiles { get; set; } = new();
        /// <summary>
        /// このステップで変化したタイル座標
        /// </summary>
        public List<Vector2Int> HighlightTiles { get; set; } = new();
        /// <summary>
        /// このステップで追加された矩形(分割された新エリアや新しい部屋)
        /// </summary>
        public List<RectInt> HighlightRects { get; set; } = new();
        /// <summary>
        /// マップ適用後のタイルスナップショット(適用前のステップではnull)
        /// </summary>
        public TileType[,] Map { get; set; }
        /// <summary>
        /// 生成されたフロアデータ(最終ステップでのみ設定される)
        /// </summary>
        public FloorData FloorData { get; set; }

        public GenerationStep(string label, Vector2Int size)
        {
            Label = label;
            Size = size;
        }
    }
}
