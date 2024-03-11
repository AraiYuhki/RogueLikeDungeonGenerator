using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Xeon.Utility
{
    public static class Extensions
    {
        private static Random random = new Random();
        public static List<T> ToList<T>(this T[,] self)
        {
            var xLength = self.GetLength(0);
            var yLength = self.GetLength(1);
            var result = new List<T>();
            for (var y = 0; y < yLength; y++)
            {
                for (var x = 0; x < xLength; x++)
                {
                    result.Add(self[x, y]);
                }
            }
            return result;
        }
        public static T[] ToArray<T>(this T[,] self)
        {
            return self == null ? null : self.ToList().ToArray();
        }

        public static T[,] To2DArray<T>(this T[] self, Vector2Int size)
            => self.To2DArray(size.x, size.y);

        public static T[,] To2DArray<T>(this T[] self, int width, int height)
        {
            if (self == null) return null;
            var newData = new T[width, height];
            var index = 0;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (index >= self.Length)
                    {
                        newData[x, y] = default;
                        continue;
                    }
                    newData[x, y] = self[index];
                    index++;
                }
            }
            return newData;
        }

        public static T Random<T>(this IEnumerable<T> self)
        {
            var value = random.Next(self.Count());
            try
            {
                return self.ElementAt(value);
            }
            catch (Exception e)
            {
                Debug.LogError($"{value} : {self.Count()} : {e.Message}");
                throw e;
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> values, Func<T, bool> action)
        {
            foreach ((var value, var index) in values.Select((value, index) => (value, index)))
            {
                if (action(value))
                    return index;
            }
            return -1;
        }
    }
}
