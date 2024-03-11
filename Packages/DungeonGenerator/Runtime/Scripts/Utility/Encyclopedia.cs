using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.Utility
{
    /// <summary>
    /// シリアライザブルなDictionaryクラス
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class Encyclopedia<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new();
        [SerializeField]
        private List<TValue> values = new();
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            var e = GetEnumerator();
            while (e.MoveNext())
            {
                keys.Add(e.Current.Key);
                values.Add(e.Current.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            Clear();
            var count = Mathf.Min(keys.Count, values.Count);
            for (var i = 0; i < count; i++)
                this[keys[i]] = values[i];
        }
    }
}
