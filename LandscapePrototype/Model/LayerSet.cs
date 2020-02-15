using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class LayerSet : IEnumerable<long>
    {
        public long[] LayerIDs { get; private set; }

        public LayerSet(long[] layerIDs)
        {
            LayerIDs = layerIDs;
        }

        public IEnumerator<long> GetEnumerator() => ((IEnumerable<long>)LayerIDs).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => LayerIDs.GetEnumerator();

        public static async Task CreateLayerSetTempTable(LayerSet layers, string tablename, NpgsqlConnection conn)
        {
            using var c1 = new NpgsqlCommand(@$"
                CREATE TEMPORARY TABLE IF NOT EXISTS {tablename}
               (
                    id bigint,
                    ""order"" int
               )", conn);
            await c1.ExecuteNonQueryAsync();
            await new NpgsqlCommand(@$"TRUNCATE TABLE {tablename}", conn).ExecuteNonQueryAsync();

            var order = 0;
            foreach (var layerID in layers)
            {
                using var c2 = new NpgsqlCommand(@$"INSERT INTO {tablename} (id, ""order"") VALUES (@id, @order)", conn);
                c2.Parameters.AddWithValue("id", layerID);
                c2.Parameters.AddWithValue("order", order++);
                await c2.ExecuteScalarAsync();
            }
        }
    }
}
