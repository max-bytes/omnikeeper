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
        public string Name => GetType().Name!;

        public virtual ISet<string>? GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger) => null;

        public async Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, 
            IChangesetProxy changesetProxy, IModelContextBuilder modelContextBuilder, ILogger logger, IIssueAccumulator issueAccumulator)
        {
            try
            {
                using var trans = modelContextBuilder.BuildDeferred();

                var result = await Run(targetLayerID, unprocessedChangesets, config, changesetProxy, trans, logger, issueAccumulator);

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

        public abstract Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, 
            JsonDocument config, IChangesetProxy changesetProxy, IModelContext trans, ILogger logger, IIssueAccumulator issueAccumulator);

    }
}
