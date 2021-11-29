﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
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

        protected virtual ISet<string>? GetDependentLayerIDs(JObject config, ILogger logger) => null;

        public async Task<bool> CanSkipRun(JObject config, ILogger logger, IModelContextBuilder modelContextBuilder)
        {
            if (lastRun == null)
            {
                return false;
            } else
            {
                var dependentLayerIDs = GetDependentLayerIDs(config, logger);
                if (dependentLayerIDs == null)
                {
                    return false;
                } else
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

        public virtual void SetLastRun(DateTimeOffset lr)
        {
            lastRun = lr;
        }

        private DateTimeOffset? lastRun = null;
    }
}
