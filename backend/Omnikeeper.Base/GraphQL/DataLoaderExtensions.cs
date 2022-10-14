using GraphQL.DataLoader;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.GraphQL
{
    public static class DataLoaderExtensions
    {
        public static IDataLoaderResult<T?[]> ToResultOfList<T>(this IEnumerable<IDataLoaderResult<T?>> data)
        {
            return new SimpleDataLoader<T?[]>(token =>
            {
                var tasks = data.Select(x => x.GetResultAsync(token));
                return Task.WhenAll(tasks);
            });
        }
        //public static IDataLoaderResult<IEnumerable<T>> ToResultOfListNonNull<T>(this IEnumerable<IDataLoaderResult<T>> data)
        //{
        //    return new SimpleDataLoader<IEnumerable<T>>(async token =>
        //    {
        //        var tasks = data.Select(x => x.GetResultAsync(token));
        //        var list = new List<T>(tasks.Count());
        //        foreach (var task in tasks)
        //        {
        //            list.Add(await task);
        //        }
        //        return list;
        //    });
        //}

        public static IDataLoaderResult<T[]> ToResultOfListNonNull<T>(this IEnumerable<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<T[]>(token =>
            {
                var tasks = data.Select(x => x.GetResultAsync(token));
                return Task.WhenAll(tasks);
            });
        }

        public static IDataLoaderResult<T> ResolveNestedResults<T>(this IDataLoaderResult<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<T>(token =>
            {
                var a = data.GetResultAsync(token).ContinueWith(t => t.Result.GetResultAsync(token)).Unwrap();
                return a;
            });
        }
    }
}
