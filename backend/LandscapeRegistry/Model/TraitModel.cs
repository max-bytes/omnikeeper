using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitModel : ITraitModel
    {
        private readonly ILogger<TraitModel> logger;
        private readonly NpgsqlConnection conn;

        public TraitModel(ILogger<TraitModel> logger, NpgsqlConnection connection)
        {
            this.logger = logger;
            conn = connection;
        }

        public async Task<TraitSet> GetTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            using var command = new NpgsqlCommand(@"
                SELECT config FROM traits WHERE timestamp <= @timestamp 
                ORDER BY timestamp DESC LIMIT 1
            ", conn, trans);
            command.Parameters.AddWithValue("timestamp", timeThreshold.Time);
            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return TraitSet.Build();

            var configJO = dr.GetFieldValue<JObject>(0);
            try
            {
                var traits = TraitsProvider.TraitSetSerializer.Deserialize(configJO);

                return traits;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Could not deserialize traits");
                return TraitSet.Build();
            }
        }

        public async Task<TraitSet> SetTraitSet(TraitSet traitSet, NpgsqlTransaction trans)
        {
            var traitsJO = TraitsProvider.TraitSetSerializer.SerializeToJObject(traitSet);
            using var command = new NpgsqlCommand(@"INSERT INTO traits (config, timestamp) VALUES (@config, @timestamp)", conn, trans);
            command.Parameters.Add(new NpgsqlParameter("config", NpgsqlDbType.Json) { Value = traitsJO });
            command.Parameters.AddWithValue("timestamp", TimeThreshold.BuildLatest().Time);
            await command.ExecuteNonQueryAsync();
            return traitSet;
        }
    }
}
