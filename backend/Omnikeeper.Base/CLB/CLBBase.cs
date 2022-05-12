using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public abstract class CLBBase : IComputeLayerBrain
    {
        private readonly ILatestLayerChangeModel latestLayerChangeModel;

        protected CLBBase(ILatestLayerChangeModel latestLayerChangeModel)
        {
            this.latestLayerChangeModel = latestLayerChangeModel;
        }

        public string Name => GetType().Name!;

        public async Task<bool> Run(Layer targetLayer, JsonDocument config, IChangesetProxy changesetProxy, IModelContextBuilder modelContextBuilder, ILogger logger)
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

        public abstract Task<bool> Run(Layer targetLayer, JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger);

        protected virtual ISet<string>? GetDependentLayerIDs(JsonDocument config, ILogger logger) => null;

        public async Task<bool> CanSkipRun(DateTimeOffset? lastRun, JsonDocument config, ILogger logger, IModelContextBuilder modelContextBuilder)
        {
            if (lastRun == null)
            {
                return false;
            }
            else
            {
                // TODO: check if config has changed; if yes -> cannot skip run

                var dependentLayerIDs = GetDependentLayerIDs(config, logger);
                if (dependentLayerIDs == null)
                {
                    return false;
                }
                else
                {
                    using var trans = modelContextBuilder.BuildImmediate();
                    foreach (var dependentLayerID in dependentLayerIDs)
                    {
                        var latestChangeInLayer = await latestLayerChangeModel.GetLatestChangeInLayer(dependentLayerID, trans);
                        if (latestChangeInLayer == null)
                            return false;
                        if (latestChangeInLayer > lastRun)
                            return false;
                    }
                    return true; // <- all dependent layer have not changed since last run, we can skip
                }
            }
        }
    }
}
