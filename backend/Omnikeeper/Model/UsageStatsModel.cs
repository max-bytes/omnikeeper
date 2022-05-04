using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class UsageStatsModel : IUsageStatsModel
    {
        public async Task<IEnumerable<UsageStatElement>> GetElements(System.DateTimeOffset from, System.DateTimeOffset to, IModelContext trans)
        {
            var query = @"SELECT element_type, element_name, username, layer_id, timestamp FROM public.usage_stats where timestamp >= @from AND timestamp <= @to";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("from", from);
            command.Parameters.AddWithValue("to", to);
            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            var ret = new List<UsageStatElement>();
            while (dr.Read())
            {
                var type = dr.GetString(0);
                var name = dr.GetString(1);
                var username = dr.GetString(2);
                var layerID = dr.GetString(3);
                var timestamp = dr.GetDateTime(4);

                var e = new UsageStatElement(type, name, username, layerID, timestamp);
                ret.Add(e);
            }
            return ret;
        }
    }
}
