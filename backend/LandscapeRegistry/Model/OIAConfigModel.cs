using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Landscape.Base.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class OIAConfigModel : IOIAConfigModel
    {
        //public IDictionary<string, string> oiaConfigs = new Dictionary<string, string>()
        //{
        //    // keycloak adapter
        //    { "Internal Keycloak", "{\"$type\":\"OnlineInboundAdapterKeycloak.OnlineInboundAdapter+ConfigInternal, OnlineInboundAdapterKeycloak\",\"mapperScope\":\"internal_keycloak\",\"preferredIDMapUpdateRate\":\"00:00:05\",\"BuilderName\":\"Keycloak Internal\"}" },

        //        //JsonConvert.SerializeObject(
        //        //new OnlineInboundAdapterKeycloak.OnlineInboundAdapter.ConfigInternal(new TimeSpan(0, 0, 5), "internal_keycloak")
        //        //, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects })},

        //    // omnikeeper adapter
        //    { "Omnikeeper", "{\"$type\":\"OnlineInboundAdapterOmnikeeper.OnlineInboundAdapter+Config, OnlineInboundAdapterOmnikeeper\",\"apiURL\":\"https://localhost:44378/\",\"authURL\":\"https://host.docker.internal:8443\",\"realm\":\"landscape\",\"clientID\":\"landscape-registry-api\",\"clientSecret\":\"d822a6c6-aca4-45bc-aa7e-5d59bc9011cf\",\"mapperScope\":\"omnikeeper\",\"remoteLayerNames\":[\"CMDB\"],\"preferredIDMapUpdateRate\":\"00:00:05\",\"BuilderName\":\"Omnikeeper\"}" }


        //        //JsonConvert.SerializeObject(new OnlineInboundAdapterOmnikeeper.OnlineInboundAdapter.Config(
        //        //"https://localhost:44378/", 
        //        //"https://host.docker.internal:8443",
        //        //"landscape",
        //        //"landscape-registry-api",
        //        //"d822a6c6-aca4-45bc-aa7e-5d59bc9011cf",
        //        //new string[] { "CMDB" }, 
        //        //new TimeSpan(0, 0, 5), 
        //        //"omnikeeper")
        //        //, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects })
        //    //}
        //};


        private readonly NpgsqlConnection conn;
        private readonly ILogger<OIAConfigModel> logger;
        private readonly JsonSerializer serializer;

        public OIAConfigModel(ILogger<OIAConfigModel> logger, NpgsqlConnection connection)
        {
            conn = connection;
            this.logger = logger;

            serializer = new JsonSerializer
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
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
                        var config = serializer.Deserialize<IOnlineInboundAdapter.IConfig>(new JTokenReader(configJO));
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
                var config = serializer.Deserialize<IOnlineInboundAdapter.IConfig>(new JTokenReader(configJO));
                return OIAConfig.Build(name, id, config);
            } catch(Exception e)
            {
                logger.LogError(e, $"Could not deserialize OIA config \"{name}\"");
                return null;
            }
        }

        public async Task<OIAConfig> Create(string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = JObject.FromObject(config, serializer);
            using var command = new NpgsqlCommand(@"INSERT INTO onlineinboundadapter_config (name, config) VALUES (@name, @config) RETURNING id", conn, trans);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            var id = (long)await command.ExecuteScalarAsync();
            return OIAConfig.Build(name, id, config);
        }

        public async Task<OIAConfig> Update(long id, string name, IOnlineInboundAdapter.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = JObject.FromObject(config, serializer);
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
                var config = serializer.Deserialize<IOnlineInboundAdapter.IConfig>(new JTokenReader(configJO));
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
