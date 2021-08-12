using System;
using System.Collections.Generic;

namespace mosh.Collections
{
    public static class CollectionExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueProducer)
        {
            if (!dictionary.TryGetValue(key, out TValue? value))
                value = valueProducer();
            return value;
        }

        public static TValue GetValueOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out TValue? gotValue))
                return gotValue;
            else
            {
                dictionary[key] = value;
                return value;
            }
        }

        public static TValue GetValueOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueProducer)
        {
            if (!dictionary.TryGetValue(key, out TValue? value))
            {
                value = valueProducer();
                dictionary[key] = value;
            }
            return value;
        }
    }
}
