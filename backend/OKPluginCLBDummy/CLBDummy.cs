using Microsoft.Extensions.Logging;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OKPluginCLBDummy
{
    public class CLBDummy : CLBBase
    {
        private class Config
        {
            [JsonPropertyName("source_layerset")]
            public string[] SourceLayerset { get; set; }
        }
        /*
         {
          "source_layerset": [
            "layer_id1", "layer_id2"
          ]
        }
        */

        private Config ParseConfig(JsonDocument config) => JsonSerializer.Deserialize<Config>(config);

        public override ISet<string> GetDependentLayerIDs(JsonDocument config, ILogger logger)
        {
            try
            {
                var parsedConfig = ParseConfig(config);
                return parsedConfig.SourceLayerset.ToHashSet();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Cannot parse CLB config");
                return null; // we hit an error parsing the config, cannot extract dependent layers
            }
        }

        public override async Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start dummy CLB");

            logger.LogDebug("End dummy CLB");

            return true;
        }
    }
}
