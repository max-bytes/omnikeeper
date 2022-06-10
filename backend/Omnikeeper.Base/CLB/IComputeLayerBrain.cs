using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(string targetLayerID, IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, 
            IChangesetProxy changesetProxy, IModelContextBuilder modelContextBuilder, ILogger logger, IIssueAccumulator issueAccumulator);

        ISet<string>? GetDependentLayerIDs(string targetLayerID, JsonDocument config, ILogger logger);
    }
}
