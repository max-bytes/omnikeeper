using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface ICalculateUnprocessedChangesetsService
    {
        Task<(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, IReadOnlyDictionary<string, Guid> latestSeenChangesets)> CalculateUnprocessedChangesets(
            IReadOnlyDictionary<string, Guid>? processedChangesets, ISet<string> layerIDs, TimeThreshold timeThreshold, IModelContext trans);
    }
}
