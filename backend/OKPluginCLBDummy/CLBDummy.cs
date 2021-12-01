using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OKPluginCLBDummy
{
    public class CLBDummy : CLBBase
    {
        public CLBDummy(ILatestLayerChangeModel latestLayerChangeModel) : base(latestLayerChangeModel)
        {
        }

        private class Config
        {
            [JsonProperty("source_layerset")]
            public string[] SourceLayerset { get; set; }
        }
        /*
         {
          "source_layerset": [
            "layer_id1", "layer_id2"
          ]
        }
        */

        private Config ParseConfig(JObject config) => config.ToObject<Config>();

        protected override ISet<string> GetDependentLayerIDs(JObject config, ILogger logger)
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

        public override async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger)
        {
            logger.LogDebug("Start dummy CLB");

            logger.LogDebug("End dummy CLB");

            return true;
        }
    }
}
