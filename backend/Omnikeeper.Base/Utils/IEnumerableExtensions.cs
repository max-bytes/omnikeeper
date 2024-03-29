﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Base.Utils
{
    public static class IEnumerableExtensions
    {
        // fast check for empty, both when generic IEnumerable has Count property and when not
        public static bool IsEmpty<T>(this IEnumerable<T> list)
        {
            if (list is ICollection<T> c) return c.Count == 0;
            return !list.Any();
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : class
        {
            return o.Where(x => x != null)!;
        }

        public static HashSet<T> ToHashSet<T>(
            this IEnumerable<T> source,
            IEqualityComparer<T>? comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        ///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="predicate">The expression to test the items against.</param>
        ///<returns>The index of the first matching item, or -1 if no items match.</returns>
        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (predicate == null) throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return retVal;
                retVal++;
            }
            return -1;
        }
        ///<summary>Finds the index of the first occurrence of an item in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="item">The item to find.</param>
        ///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> items, T item) { return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i)); }


        public static IEnumerable<TSource> Concat<TSource>(this IEnumerable<TSource> first, TSource second)
        {
            return first.Concat(new List<TSource>() { second });
        }

        [return: MaybeNull]
        public static T GetRandom<T>(this IEnumerable<T> enumerable, Random random)
        {
            var count = enumerable.Count();
            if (count == 0) return default;
            int index = random.Next(0, count);
            return enumerable.ElementAt(index);
        }

        public static bool IsSubsetOf<T>(this IEnumerable<T> coll1, IEnumerable<T> coll2)
        {
            bool isSubset = !coll1.Except(coll2).Any();
            return isSubset;
        }

        public static bool NullRespectingSequenceEqual<T>(this IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (first == null && second == null)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            return first.SequenceEqual(second);
        }
    }
}
