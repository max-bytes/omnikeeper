﻿using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Utils
{
    public static class DictionaryExtensions
    {
        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key"></param>
        /// <param name="addF"></param>
        /// <param name="updateF">Allows both in-place and full replacement update</param>
        /// <returns></returns>
        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> addF, Func<TValue, TValue> updateF) where TKey : notnull
        {
            if (dictionary.ContainsKey(key))
            {
                var old = dictionary[key];
                dictionary[key] = updateF(old);
            }
            else
            {
                dictionary.Add(key, addF());
            }

            return dictionary;
        }

        public static bool TryGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue? t) where TValue : struct where TKey : notnull
        {
            if (dictionary.ContainsKey(key))
            {
                t = dictionary[key];
                return true;
            }
            else
            {
                t = null;
                return false;
            }
        }

        public static TValue? GetOrWithClass<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue? or) where TKey : notnull where TValue : class
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            else
            {
                return or;
            }
        }

        public static TValue GetOr<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue or) where TKey : notnull
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            else
            {
                return or;
            }
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : notnull
        {
            return dictionary.TryGetValue(key, out TValue? value) ? value : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue>
            (this IDictionary<TKey, TValue> dictionary,
                TKey key,
                Func<TValue> defaultValueProvider) where TKey : notnull
        {
            return dictionary.TryGetValue(key, out TValue? value) ? value : defaultValueProvider();
        }
    }
}
