using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Config
{
    public class OIAContextModel : IOIAContextModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ILogger<OIAContextModel> logger;

        public OIAContextModel(ILogger<OIAContextModel> logger, NpgsqlConnection connection)
        {
            conn = connection;
            this.logger = logger;
        }

        public async Task<IEnumerable<OIAContext>> GetContexts(bool useFallbackConfig, NpgsqlTransaction trans)
        {
            var ret = new List<OIAContext>();

            using var command = new NpgsqlCommand(@"
                SELECT id, name, config FROM config.onlineinboundadapter_context
            ", conn, trans);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetInt64(0);
                    var name = s.GetString(1);
                    var configJO = s.GetFieldValue<JObject>(2);
                    IOnlineInboundAdapter.IConfig config = null;
                    try
                    {
                        config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configJO);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                        if (useFallbackConfig)
                            config = new OIAFallbackConfig(configJO.ToString(Formatting.None));
                    }
                    if (config != null)
                        ret.Add(OIAContext.Build(name, id, config));
                }
            }
            return ret;
        }

        public async Task<OIAContext> GetContextByName(string name, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT id, config FROM config.onlineinboundadapter_context WHERE name = @name LIMIT 1
            ", conn, trans);
            command.Parameters.AddWithValue("name", name);
            using var s = await command.ExecuteReaderAsync();
            if (!await s.ReadAsync())
                return null;

            var id = s.GetInt64(0);
            var configJO = s.GetFieldValue<JObject>(1);
            try
            {
                var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configJO);
                return OIAContext.Build(name, id, config);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                return null;
            }
        }

        public async Task<OIAContext> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = IOnlineInboundAdapter.IConfig.Serializer.SerializeToJObject(config);
            using var command = new NpgsqlCommand(@"INSERT INTO config.onlineinboundadapter_context (name, config) VALUES (@name, @config) RETURNING id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            var id = (long)await command.ExecuteScalarAsync();
            return OIAContext.Build(name, id, config);
        }

        public async Task<OIAContext> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = IOnlineInboundAdapter.IConfig.Serializer.SerializeToJObject(config);
            using var command = new NpgsqlCommand(@"UPDATE config.onlineinboundadapter_context SET name = @name, config = @config WHERE id = @id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();
            return OIAContext.Build(name, id, config);
        }

        public async Task<OIAContext> Delete(long id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"DELETE FROM config.onlineinboundadapter_context WHERE id = @id RETURNING name, config", conn, trans);
            command.Parameters.AddWithValue("id", id);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var name = reader.GetString(0);
            var configJO = reader.GetFieldValue<JObject>(1);
            try
            {
                var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configJO);
                return OIAContext.Build(name, id, config);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                return null;
            }
        }
    }
}
