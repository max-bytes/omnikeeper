﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class MetaConfigurationModel : IMetaConfigurationModel
    {
        private readonly ILogger<MetaConfigurationModel> logger;

        public MetaConfigurationModel(ILogger<MetaConfigurationModel> logger)
        {
            this.logger = logger;
        }

        public async Task<MetaConfiguration> GetConfig(IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM config.general WHERE key = 'meta' LIMIT 1
            ", trans.DBConnection, trans.DBTransaction);
            using var s = await command.ExecuteReaderAsync();

            if (!await s.ReadAsync())
                throw new Exception("Could not find meta config");

            var configJO = s.GetFieldValue<JObject>(0);
            try
            {
                return MetaConfiguration.Serializer.Deserialize(configJO);
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize meta configuration");
                throw new Exception("Could not find meta config", e);
            }
        }

        public async Task<MetaConfiguration> GetConfigOrDefault(IModelContext trans)
        {
            try
            {
                var fromDB = await GetConfig(trans);
                return fromDB;
            }
            catch (Exception)
            {
                return new MetaConfiguration(
                    new string[] { "__okconfig" },
                    "__okconfig"
                );
            }
        }

        public async Task<bool> IsLayerPartOfMetaConfiguration(string layerID, IModelContext trans)
        {
            var metaConfiguration = await GetConfigOrDefault(trans);

            if (metaConfiguration.ConfigWriteLayer == layerID)
                return true;
            foreach (var l in metaConfiguration.ConfigLayerset)
                if (l == layerID)
                    return true;
            return false;
        }

        public async Task<MetaConfiguration> SetConfig(MetaConfiguration config, IModelContext trans)
        {
            var configJO = MetaConfiguration.Serializer.SerializeToJObject(config);
            using var command = new NpgsqlCommand(@"
                INSERT INTO config.general (key, config) VALUES ('meta', @config) ON CONFLICT (key) DO UPDATE SET config = EXCLUDED.config
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJO });
            await command.ExecuteScalarAsync();

            return config;
        }
    }
}
