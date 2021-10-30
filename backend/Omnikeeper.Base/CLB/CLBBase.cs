using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public abstract class CLBBase : IComputeLayerBrain
    {
        public string Name => GetType().Name!;

        public async Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            try
            {
                using var trans = modelContextBuilder.BuildDeferred();

                var result = await Run(targetLayer, config, changesetProxy, trans, logger);

                if (result)
                {
                    trans.Commit();
                }

                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"Running CLB {Name} failed");
                return false;
            }
        }

        public abstract Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger);

    }
}
