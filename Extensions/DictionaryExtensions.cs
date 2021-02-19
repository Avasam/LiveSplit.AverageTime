using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveSplit.AverageTime.Extensions
{
    public static class DictionaryExtensions
    {
        public static IDictionary<K, V> Where<K, V>(this IDictionary<K, V> dict, Func<KeyValuePair<K, V>, bool> action) {
            return Enumerable.Where(dict, action).ToDictionary(k => k.Key, v => dict[v.Key]);
        }
    }
}
