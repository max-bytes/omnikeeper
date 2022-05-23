using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public class CLBLastRunCache
    {
        private class CLBLastRunEntry
        {
            public DateTimeOffset LastRun { get; set; }
        }

        public async Task UpdateCache(string clConfigID, string layerID, DateTimeOffset latestChange, IModelContext trans)
        {
            var prefixedKey = $"CLBLastRun_{clConfigID}{layerID}";
            using var command = new NpgsqlCommand(@"
                INSERT INTO config.general (key, config) VALUES (@key, @config) ON CONFLICT (key) DO UPDATE SET config = EXCLUDED.config
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("key", prefixedKey);
            var d = new CLBLastRunEntry() { LastRun = latestChange };
            var json = JsonSerializer.SerializeToDocument(d);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = json });
            await command.ExecuteScalarAsync();
        }

        public async Task DeleteFromCache(string clConfigID, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"DELETE FROM config.general WHERE key ~ @keyRegex", trans.DBConnection, trans.DBTransaction);
            var keyRegex = $"^CLBLastRun_{clConfigID}.*";
            command.Parameters.AddWithValue("keyRegex", keyRegex);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<DateTimeOffset?> TryGetValue(string clConfigID, string layerID, IModelContext trans)
        {
            var prefixedKey = $"CLBLastRun_{clConfigID}{layerID}";
            using var command = new NpgsqlCommand("SELECT config FROM config.general WHERE key = @key LIMIT 1", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("key", prefixedKey);
            using var s = await command.ExecuteReaderAsync();

            if (await s.ReadAsync())
            {
                try
                {
                    var json = s.GetFieldValue<JsonDocument>(0);
                    var d = json.Deserialize<CLBLastRunEntry>();
                    return d?.LastRun;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
