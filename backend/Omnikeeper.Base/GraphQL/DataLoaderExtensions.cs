using GraphQL.DataLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.GraphQL
{
    public static class DataLoaderExtensions
    {
        //public static IDataLoaderResult<T?[]> ToResultOfList<T>(this IEnumerable<IDataLoaderResult<T?>> data)
        //{
        //    return new SimpleDataLoader<T?[]>(token =>
        //    {
        //        var tasks = data.Select(x => x.GetResultAsync(token));
        //        return Task.WhenAll(tasks);
        //    });
        //}
        public static IDataLoaderResult<IEnumerable<T?>> ToResultOfList<T>(this IEnumerable<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<IEnumerable<T?>>(async token =>
            {
                var list = new List<T?>(data.Count());
                foreach (var d in data)
                {
                    list.Add(await d.GetResultAsync(token));
                }
                return list;
            });
        }

        //public static IDataLoaderResult<T[]> ToResultOfListNonNull<T>(this IEnumerable<IDataLoaderResult<T>> data)
        //{
        //    return new SimpleDataLoader<T[]>(token =>
        //    {
        //        var tasks = data.Select(x => x.GetResultAsync(token));
        //        return Task.WhenAll(tasks);
        //    });
        //}
        public static IDataLoaderResult<IEnumerable<T>> ToResultOfListNonNull<T>(this IEnumerable<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<IEnumerable<T>>(async token =>
            {
                var list = new List<T>(data.Count());
                foreach (var d in data)
                {
                    list.Add(await d.GetResultAsync(token));
                }
                return list;
            });
        }

        public static IDataLoaderResult<T> ResolveNestedResults<T>(this IDataLoaderResult<IDataLoaderResult<T>> data)
        {
            return new SimpleDataLoader<T>(async token =>
            {
                var a = await data.GetResultAsync(token).ContinueWith(async t => await t.Result.GetResultAsync(token)).Unwrap();
                return a;
            });

            //return new SimpleDataLoader<T>(async token =>
            //{
            //    var a = await data.GetResultAsync(token);
            //    if (a == null) throw new Exception("???"); // ???
            //    var b = await a.GetResultAsync(token);
            //    if (b == null) throw new Exception("???"); // ??? 
            //    return b;
            //});
        }
    }
}
