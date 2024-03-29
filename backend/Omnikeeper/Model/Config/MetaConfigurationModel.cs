﻿using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity.Config;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Text.Json;
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

        public async Task<MetaConfiguration> GetConfigOrDefault(IModelContext trans)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM config.general WHERE key = 'meta' LIMIT 1",
            trans.DBConnection, trans.DBTransaction);

            using var _ = await trans.WaitAsync();
            using var s = await command.ExecuteReaderAsync();

            if (await s.ReadAsync())
            {
                var config = s.GetFieldValue<JsonDocument>(0);
                try
                {
                    return MetaConfiguration.SystemTextJSONSerializer.Deserialize(config);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Could not deserialize meta configuration");
                }
            }

            return new MetaConfiguration(
                new string[] { "__okconfig" },
                "__okconfig",
                new string[] { "__okissues" },
                "__okissues"
            );
        }

        public async Task<MetaConfiguration> SetConfig(MetaConfiguration config, IModelContext trans)
        {
            var configJD = MetaConfiguration.SystemTextJSONSerializer.SerializeToJsonDocument(config);
            using var command = new NpgsqlCommand(@"
                INSERT INTO config.general (key, config) VALUES ('meta', @config) ON CONFLICT (key) DO UPDATE SET config = EXCLUDED.config
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = configJD }); 
            using var _ = await trans.WaitAsync();
            await command.ExecuteScalarAsync();

            return config;
        }
    }

    public static class MetaConfigurationModelExtensions
    {
        public static async Task<bool> IsLayerPartOfMetaConfiguration(this IMetaConfigurationModel model, string layerID, IModelContext trans)
        {
            var metaConfiguration = await model.GetConfigOrDefault(trans);

            if (metaConfiguration.ConfigWriteLayer == layerID)
                return true;
            foreach (var l in metaConfiguration.ConfigLayerset)
                if (l == layerID)
                    return true;
            return false;
        }
    }
}
