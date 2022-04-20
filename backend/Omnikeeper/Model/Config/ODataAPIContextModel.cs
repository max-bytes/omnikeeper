using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Config
{
    public class ODataAPIContextModel : IODataAPIContextModel
    {
        private readonly ILogger<ODataAPIContextModel> logger;

        public ODataAPIContextModel(ILogger<ODataAPIContextModel> logger)
        {
            this.logger = logger;
        }

        private ODataAPIContext? Deserialize(string id, JsonDocument configJO)
        {
            try
            {
                var config = ODataAPIContext.ConfigSerializer.Deserialize(configJO);
                return new ODataAPIContext(id, config);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize OData API context \"{id}\"");
                return null;
            }
        }

        public async Task<IEnumerable<ODataAPIContext>> GetContexts(IModelContext trans)
        {
            var ret = new List<ODataAPIContext>();

            using var command = new NpgsqlCommand(@"
                SELECT id, config FROM config.odataapi_context
            ", trans.DBConnection, trans.DBTransaction);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    var configJO = s.GetFieldValue<JsonDocument>(1);
                    var context = Deserialize(id, configJO);
                    if (context != null) // TODO: we actually need a fallback config to show, so users can at least attempt to fix any serialization issues
                        ret.Add(context);
                }
            }
            return ret;
        }

        public async Task<ODataAPIContext> GetContextByID(string id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM config.odataapi_context WHERE id = @id LIMIT 1
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("id", id);
            using var s = await command.ExecuteReaderAsync();
            if (!await s.ReadAsync())
                throw new Exception($"Could not find context with ID {id}");

            var configJO = s.GetFieldValue<JsonDocument>(0);
            var d = Deserialize(id, configJO);
            if (d == null)
                throw new Exception($"Could not deserialized context with ID {id}");
            return d;
        }

        public async Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, IModelContext trans)
        {
            var configJO = ODataAPIContext.ConfigSerializer.SerializeToJsonDocument(config);
            using var command = new NpgsqlCommand(@"INSERT INTO config.odataapi_context (id, config) VALUES (@id, @config) ON CONFLICT (id) DO UPDATE SET config = EXCLUDED.config", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            await command.ExecuteNonQueryAsync();
            var d = Deserialize(id, configJO);
            if (d == null)
                throw new Exception($"Could not deserialized context with ID {id}");
            return d;
        }

        public async Task<ODataAPIContext> Delete(string id, IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"DELETE FROM config.odataapi_context WHERE id = @id RETURNING config", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("id", id);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var config = reader.GetFieldValue<JsonDocument>(0);

            var d = Deserialize(id, config);
            if (d == null)
                throw new Exception($"Could not deserialized context with ID {id}");
            return d;
        }
    }
}
