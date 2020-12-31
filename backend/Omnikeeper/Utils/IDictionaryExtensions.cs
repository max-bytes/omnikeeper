using System;
using System.Collections.Generic;

namespace Omnikeeper.Utils
{
    public static class IDictionaryExtensions
    { // TODO: move to Omnikeeper.Base.Utils.DictionaryExtensions
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary,
                TKey key,
                Func<TValue> defaultValueProvider) where TKey : notnull
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValueProvider();
        }
    }
}
