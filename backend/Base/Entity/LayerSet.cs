using DotLiquid;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Landscape.Base.Entity
{
    public class LayerSet : IEnumerable<long>
    {
        // can be unsorted
        public long[] LayerIDs { get; private set; }
        public long LayerHash
        {
            get
            {
                unchecked // we expect overflows
                {
                    return LayerIDs.Aggregate(2341L, (hash, item) => hash * 37L + item);
                }
            }
        }

        public LayerSet(params long[] layerIDs)
        {
            LayerIDs = layerIDs;
        }

        public IEnumerator<long> GetEnumerator() => ((IEnumerable<long>)LayerIDs).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => LayerIDs.GetEnumerator();

        public bool IsEmpty => LayerIDs.Length <= 0;

        public static string CreateLayerSetSQLValues(LayerSet layers)
        {
            if (layers.IsEmpty) throw new Exception("Cannot create valid SQL values from an empty layerset");

            var order = 0;
            var items = new List<string>();
            foreach (var layerID in layers)
            {
                items.Add($"({layerID}, {order++})");
            }
            return $"VALUES{string.Join(',', items)}";
        }

        //public static async Task<string> CreateLayerSetTempTable(LayerSet layers, string tablePrefix, NpgsqlConnection conn, NpgsqlTransaction trans)
        //{
        //    //--CREATE TEMPORARY TABLE temp_layerset(id bigint, "order" int);
        //    //--INSERT INTO temp_layerset(id, "order") VALUES(1, 1), (2, 2), (3, 3), (4, 4);
        //    var fullTableName = $"{tablePrefix}_{layers.LayerHash}";

        //    using var cCheck = new NpgsqlCommand(@$"SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_schema like 'pg_temp_%' AND table_name = LOWER('{fullTableName}') )", conn, trans);
        //    var exists = (bool)await cCheck.ExecuteScalarAsync();
        //    if (!exists)
        //    {
        //        using var c1 = new NpgsqlCommand(@$"
        //            CREATE TEMPORARY TABLE {fullTableName}
        //           (
        //                id bigint,
        //                ""order"" int
        //           )", conn, trans);
        //        await c1.ExecuteNonQueryAsync();
        //        //await new NpgsqlCommand(@$"TRUNCATE TABLE {tablePrefix}", conn, trans).ExecuteNonQueryAsync();

        //        var order = 0;
        //        foreach (var layerID in layers)
        //        {
        //            using var c2 = new NpgsqlCommand(@$"INSERT INTO {fullTableName} (id, ""order"") VALUES (@id, @order)", conn, trans);
        //            c2.Parameters.AddWithValue("id", layerID);
        //            c2.Parameters.AddWithValue("order", order++);
        //            await c2.ExecuteScalarAsync();
        //        }
        //    }
        //    return fullTableName;
        //}
    }
}
