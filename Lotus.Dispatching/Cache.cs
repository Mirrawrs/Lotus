using System;
using System.Collections.Generic;

namespace Lotus.Dispatching
{
    internal static class Cache
    {
        public static TValue Get<TKey, TValue>(TKey key, Func<TKey, TValue> valueFactory)
        {
            var tupleKey = (key, valueFactory);
            var dictionary = Internal<TKey, TValue>.Dictionary;
            return dictionary.TryGetValue(tupleKey, out var value) ? value : dictionary[tupleKey] = valueFactory(key);
        }

        private static class Internal<TKey, TValue>
        {
            public static IDictionary<(TKey, Func<TKey, TValue>), TValue> Dictionary { get; } =
                new Dictionary<(TKey, Func<TKey, TValue>), TValue>();
        }
    }
}