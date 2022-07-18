using GraphQL.DataLoader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.GraphQL
{
    public static class DataLoaderExtensions
    {
        public static IDataLoaderResult<IEnumerable<T?>> ToResultOfList<T>(this IEnumerable<IDataLoaderResult<T?>> data)
        {
            return new SimpleDataLoader<IEnumerable<T?>>(async token =>
            {
                var tasks = data.Select(x => x.GetResultAsync(token));
                var list = new List<T?>(tasks.Count());
                foreach (var task in tasks)
                {
                    list.Add(await task.ConfigureAwait(false));
                }
                return list;
            });
        }
        public static IDataLoaderResult<IEnumerable<T>> ToResultOfListNonNull<T>(this IEnumerable<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<IEnumerable<T>>(async token =>
            {
                var tasks = data.Select(x => x.GetResultAsync(token));
                var list = new List<T>(tasks.Count());
                foreach (var task in tasks)
                {
                    list.Add(await task.ConfigureAwait(false));
                }
                return list;
            });
        }

        // TODO: is there a better way to do this?
        public static IDataLoaderResult<T> ResolveNestedResults<T>(this IDataLoaderResult<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<T>(async token =>
            {
                var a = data.GetResultAsync(token);
                var b = await a;
                if (b == null) throw new Exception("???"); // ???
                var c = b.GetResultAsync(token);
                var d = await c;
                if (d == null) throw new Exception("???"); // ??? 
                return d;
            });
        }
    }
}
