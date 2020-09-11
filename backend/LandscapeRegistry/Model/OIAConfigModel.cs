using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class OIAConfigModel : IOIAConfigModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ILogger<OIAConfigModel> logger;

        public OIAConfigModel(ILogger<OIAConfigModel> logger, NpgsqlConnection connection)
        {
            conn = connection;
            this.logger = logger;
        }

        public async Task<IEnumerable<OIAConfig>> GetConfigs(NpgsqlTransaction trans)
        {
            var ret = new List<OIAConfig>();

            using var command = new NpgsqlCommand(@"
                SELECT id, name, config FROM onlineinboundadapter_config
            ", conn, trans);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetInt64(0);
                    var name = s.GetString(1);
                    var configJO = s.GetFieldValue<JObject>(2);
                    try
                    {
                        var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configJO);
                        ret.Add(OIAConfig.Build(name, id, config));
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                        return null;
                    }
                }
            }
            return ret;
        }

        public async Task<OIAConfig> GetConfigByName(string name, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT id, config FROM onlineinboundadapter_config WHERE name = @name LIMIT 1
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
                return OIAConfig.Build(name, id, config);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                return null;
            }
        }

        public async Task<OIAConfig> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = IOnlineInboundAdapter.IConfig.Serializer.SerializeToJObject(config);
            using var command = new NpgsqlCommand(@"INSERT INTO onlineinboundadapter_config (name, config) VALUES (@name, @config) RETURNING id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            var id = (long)await command.ExecuteScalarAsync();
            return OIAConfig.Build(name, id, config);
        }

        public async Task<OIAConfig> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = IOnlineInboundAdapter.IConfig.Serializer.SerializeToJObject(config);
            using var command = new NpgsqlCommand(@"UPDATE onlineinboundadapter_config SET name = @name, config = @config WHERE id = @id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            command.Parameters.AddWithValue("id", id);
            await command.ExecuteNonQueryAsync();
            return OIAConfig.Build(name, id, config);
        }

        public async Task<OIAConfig> Delete(long id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"DELETE FROM onlineinboundadapter_config WHERE id = @id RETURNING name, config", conn, trans);
            command.Parameters.AddWithValue("id", id);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var name = reader.GetString(0);
            var configJO = reader.GetFieldValue<JObject>(1);
            try
            {
                var config = IOnlineInboundAdapter.IConfig.Serializer.Deserialize(configJO);
                return OIAConfig.Build(name, id, config);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                return null;
            }
        }
    }
}
