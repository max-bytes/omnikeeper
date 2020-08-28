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
    public class ODataAPIContextModel : IODataAPIContextModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ILogger<ODataAPIContextModel> logger;

        public ODataAPIContextModel(ILogger<ODataAPIContextModel> logger, NpgsqlConnection connection)
        {
            conn = connection;
            this.logger = logger;
        }

        private ODataAPIContext Deserialize(string id, JObject configJO)
        {
            try
            {
                var config = ODataAPIContext.DeserializeConfig(configJO);
                return new ODataAPIContext { ID = id, CConfig = config };
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize OData API context \"{id}\"");
                return null;
            }
        }

        public async Task<IEnumerable<ODataAPIContext>> GetContexts(NpgsqlTransaction trans)
        {
            var ret = new List<ODataAPIContext>();

            using var command = new NpgsqlCommand(@"
                SELECT id, config FROM odataapicontext
            ", conn, trans);
            using (var s = await command.ExecuteReaderAsync())
            {
                while (await s.ReadAsync())
                {
                    var id = s.GetString(0);
                    var configJO = s.GetFieldValue<JObject>(1);
                    var context = Deserialize(id, configJO);
                    if (context != null)
                        ret.Add(context);
                }
            }
            return ret;
        }

        public async Task<ODataAPIContext> GetContextByID(string id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM odataapicontext WHERE id = @id LIMIT 1
            ", conn, trans);
            command.Parameters.AddWithValue("id", id);
            using var s = await command.ExecuteReaderAsync();
            if (!await s.ReadAsync())
                return null;

            var configJO = s.GetFieldValue<JObject>(0);
            return Deserialize(id, configJO);
        }

        public async Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, NpgsqlTransaction trans)
        {
            var configJO = ODataAPIContext.SerializeConfigToJObject(config);
            using var command = new NpgsqlCommand(@"INSERT INTO odataapicontext (id, config) VALUES (@id, @config) ON CONFLICT (id) DO UPDATE SET config = EXCLUDED.config", conn, trans);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            await command.ExecuteNonQueryAsync();
            return Deserialize(id, configJO);
        }

        public async Task<ODataAPIContext> Delete(string id, NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"DELETE FROM odataapicontext WHERE id = @id RETURNING config", conn, trans);
            command.Parameters.AddWithValue("id", id);

            using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var config = reader.GetFieldValue<JObject>(0);

            return Deserialize(id, config);
        }
    }
}
