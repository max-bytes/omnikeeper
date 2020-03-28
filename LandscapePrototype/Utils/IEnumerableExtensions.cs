using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Utils
{
    public static class IEnumerableExtensions
    {
        // fast check for empty, both when generic IEnumerable has Count property and when not
        public static bool IsEmpty<T>(this IEnumerable<T> list)
        {
            if (list is ICollection<T>) return ((ICollection<T>)list).Count == 0;
            return !list.Any();
        }
    }
}
