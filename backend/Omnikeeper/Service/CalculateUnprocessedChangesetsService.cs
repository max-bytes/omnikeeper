using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class CalculateUnprocessedChangesetsService : ICalculateUnprocessedChangesetsService
    {
        private readonly IChangesetModel changesetModel;

        public CalculateUnprocessedChangesetsService(IChangesetModel changesetModel)
        {
            this.changesetModel = changesetModel;
        }

        public async Task<(IReadOnlyDictionary<string, IReadOnlyList<Changeset>?> unprocessedChangesets, IReadOnlyDictionary<string, Guid> latestSeenChangesets)>
            CalculateUnprocessedChangesets(IReadOnlyDictionary<string, Guid>? processedChangesets, ISet<string> layerIDs, TimeThreshold timeThreshold, IModelContext trans)
        {
            var unprocessedChangesets = new Dictionary<string, IReadOnlyList<Changeset>?>(); // null value means all changesets
            var latestSeenChangesets = new Dictionary<string, Guid>();
            foreach (var dependentLayerID in layerIDs)
            {
                if (processedChangesets != null && processedChangesets.TryGetValue(dependentLayerID, out var lastProcessedChangesetID))
                {
                    var up = await changesetModel.GetChangesetsAfter(lastProcessedChangesetID, new string[] { dependentLayerID }, trans, timeThreshold);
                    var latestID = up.FirstOrDefault()?.ID ?? lastProcessedChangesetID;
                    unprocessedChangesets.Add(dependentLayerID, up);
                    latestSeenChangesets.Add(dependentLayerID, latestID);
                }
                else
                { // we have not processed any changesets for this layer
                    var latest = await changesetModel.GetLatestChangeset(AllCIIDsSelection.Instance, AllAttributeSelection.Instance, null, new string[] { dependentLayerID }, trans, timeThreshold);
                    if (latest != null)
                    { // there is at least one changeset for this layer
                        unprocessedChangesets.Add(dependentLayerID, null);
                        latestSeenChangesets.Add(dependentLayerID, latest.ID);
                    }
                    else
                    { // there exists no changeset for this layer at all
                        unprocessedChangesets.Add(dependentLayerID, ImmutableList<Changeset>.Empty);
                    }
                }
            }
            return (unprocessedChangesets, latestSeenChangesets);
        }
    }
}
