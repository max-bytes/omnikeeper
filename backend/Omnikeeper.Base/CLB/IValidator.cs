using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IValidator
    {
        string Name { get; }

        Task<bool> Run(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, JsonDocument config, 
            IModelContextBuilder modelContextBuilder, TimeThreshold timeThreshold, ILogger logger, IIssueAccumulator issueAccumulator);

        Task<ISet<string>> GetDependentLayerIDs(JsonDocument config, ILogger logger, IModelContextBuilder modelContextBuilder);
    }
}
