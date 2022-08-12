using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class ArchiveOutdatedChangesetDataService : IArchiveOutdatedChangesetDataService
    {
        private readonly ChangesetDataModel changesetDataModel;
        private readonly ILayerModel layerModel;
        private readonly IChangesetModel changesetModel;
        private readonly IBaseAttributeRevisionistModel baseAttributeRevisionistModel;

        public ArchiveOutdatedChangesetDataService(ChangesetDataModel changesetDataModel, ILayerModel layerModel, IChangesetModel changesetModel,
            IBaseAttributeRevisionistModel baseAttributeRevisionistModel)
        {
            this.changesetDataModel = changesetDataModel;
            this.layerModel = layerModel;
            this.changesetModel = changesetModel;
            this.baseAttributeRevisionistModel = baseAttributeRevisionistModel;
        }

        // delete changeset-data when it belongs to an empty changeset
        // we check for changesets that have ONLY changeset-data, but no other changes associated and delete the changeset data
        public async Task<int> Archive(ILogger logger, IModelContext trans)
        {
            var allLayers = await layerModel.GetLayers(trans);
            var changesetData = await changesetDataModel.GetByCIID(AllCIIDsSelection.Instance, new LayerSet(allLayers.Select(l => l.ID)), trans, TimeThreshold.BuildLatest());

            // NOTE: this wont find changesets, whose changeset-data has relations to other CIs that are part of the same changeset
            // find the changesets with those changeset-data CIs and check if the changeset-data CI is the ONLY change
            var changesetDataCIIDsToTruncate = new HashSet<(Guid changesetDataCIID, string layerID)>();
            foreach (var cd in changesetData)
            {
                if (Guid.TryParse(cd.Value.ID, out var changesetID))
                {
                    var ciids = await changesetModel.GetCIIDsAffectedByChangeset(changesetID, trans);
                    if (ciids.Count == 1 && ciids.First() == cd.Key)
                    {
                        var changeset = await changesetModel.GetChangeset(changesetID, trans);
                        if (changeset != null)
                            changesetDataCIIDsToTruncate.Add((cd.Key, changeset.LayerID));
                        else
                            logger.LogError($"Could not find changeset with changeset-ID {changesetID}");
                    }
                }
                else
                {
                    logger.LogError($"Could not parse changeset-ID {cd.Value.ID}");
                }
            }
            int overallDeleted = 0;
            if (!changesetDataCIIDsToTruncate.IsEmpty())
            {
                var groupedByLayer = changesetDataCIIDsToTruncate.GroupBy(t => t.layerID);
                foreach (var g in groupedByLayer)
                {
                    var ciids = g.Select(t => t.changesetDataCIID).ToHashSet();
                    var numDeleted = await baseAttributeRevisionistModel.DeleteAllAttributes(
                        SpecificCIIDsSelection.Build(ciids), g.Key, trans
                    );
                    logger.LogInformation($"Emptied {ciids.Count} changeset-data-CIs in layer {g.Key}");

                    overallDeleted += ciids.Count;
                }
            }
            return overallDeleted;
        }
    }
}
