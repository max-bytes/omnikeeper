using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class RecursiveTraitModel : IRecursiveTraitModel
    {
        private readonly ILogger<RecursiveTraitModel> logger;

        public RecursiveTraitModel(ILogger<RecursiveTraitModel> logger)
        {
            this.logger = logger;
        }

        public async Task<RecursiveTraitSet> GetRecursiveTraitSet(IModelContext trans, TimeThreshold timeThreshold)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM traits WHERE timestamp <= @timestamp 
                ORDER BY timestamp DESC LIMIT 1
            ", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("timestamp", timeThreshold.Time);
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return RecursiveTraitSet.Build();

            var configJO = dr.GetFieldValue<JObject>(0);
            try
            {
                var traits = TraitsProvider.TraitSetSerializer.Deserialize(configJO);

                return traits;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize traits");
                return RecursiveTraitSet.Build();
            }
        }

        public async Task<RecursiveTraitSet> SetRecursiveTraitSet(RecursiveTraitSet traitSet, IModelContext trans)
        {
            var traitsJO = TraitsProvider.TraitSetSerializer.SerializeToJObject(traitSet);
            using var command = new NpgsqlCommand(@"INSERT INTO traits (config, timestamp) VALUES (@config, @timestamp)", trans.DBConnection, trans.DBTransaction);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = traitsJO });
            command.Parameters.AddWithValue("timestamp", TimeThreshold.BuildLatest().Time);
            await command.ExecuteNonQueryAsync();
            return traitSet;
        }
    }
}
