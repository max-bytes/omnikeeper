using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Threading.Tasks;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Entity.Config;
using DotLiquid.Tags;

namespace Omnikeeper.Model
{
    public class BaseConfigurationModel : IBaseConfigurationModel
    {
        private readonly NpgsqlConnection conn;
        private readonly ILogger<BaseConfigurationModel> logger;

        public BaseConfigurationModel(ILogger<BaseConfigurationModel> logger, NpgsqlConnection connection)
        {
            conn = connection;
            this.logger = logger;
        }

        public async Task<BaseConfigurationV1> GetConfig(NpgsqlTransaction trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM config.general WHERE key = 'base' LIMIT 1
            ", conn, trans);
            using var s = await command.ExecuteReaderAsync();

            if (!await s.ReadAsync())
                return null;

            var configJO = s.GetFieldValue<JObject>(0);
            try
            {
                // NOTE: as soon as BaseConfigurationV2 comes along, we can first try to parse V2 here, then V1, and only then return null
                // we can also migrate from V1 to V2
                return BaseConfigurationV1.Serializer.Deserialize(configJO);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize application configuration");
                return null;
            }
        }

        public async Task<BaseConfigurationV1> GetConfigOrDefault(NpgsqlTransaction trans)
        {
            var fromDB = await GetConfig(trans);
            if (fromDB == null)
            {
                // return default
                return new BaseConfigurationV1
                {
                    ArchiveChangesetThreshold = TimeSpan.FromDays(90),
                    CLBRunnerInterval = "*/15 * * * * *",
                    ArchiveOldDataRunnerInterval = "*/5 * * * * *",
                    MarkedForDeletionRunnerInterval = "* * * * *",
                    ExternalIDManagerRunnerInterval = "*/5 * * * * *"
                };
            }
            return fromDB;
        }

        public async Task<BaseConfigurationV1> SetConfig(BaseConfigurationV1 config, NpgsqlTransaction trans)
        {
            var configJO = BaseConfigurationV1.Serializer.SerializeToJObject(config);
            using var command = new NpgsqlCommand(@"
            INSERT INTO config.general (key, config) VALUES ('base', @config) ON CONFLICT (key) DO UPDATE SET config = EXCLUDED.config
        ", conn, trans);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            await command.ExecuteScalarAsync();

            return config;
        }
    }
}
