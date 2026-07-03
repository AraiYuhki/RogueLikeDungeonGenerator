using System;
using UnityEngine;

namespace Xeon.Dungeon
{
    [Serializable]
    public class TileSetting<T> where T : Enum
    {
        [SerializeField]
        private T type;
        [SerializeField]
        private int cost = 1;

        public T Type => type;
        public int Cost => cost;

#if UNITY_EDITOR
        [Header("Inspector setting")]
        [SerializeField]
        private Color color = Color.white;

        public Color Color => color;
#endif
    }
}
