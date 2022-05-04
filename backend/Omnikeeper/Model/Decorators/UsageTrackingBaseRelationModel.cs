using Autofac;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class UsageTrackingBaseRelationModel : IBaseRelationModel
    {
        private readonly IBaseRelationModel model;
        private readonly ScopedLifetimeAccessor scopedLifetimeAccessor;

        private void TrackRelationPredicateUsage(IEnumerable<string> predicateIDs, IEnumerable<string> layerIDs)
        {
            var usageTracker = scopedLifetimeAccessor.GetLifetimeScope()?.Resolve<IScopedUsageTracker>();
            if (usageTracker != null)
                foreach (var layerID in layerIDs)
                    foreach(var predicateID in predicateIDs)
                        usageTracker.TrackUseRelationPredicate(predicateID, layerID);
        }

        private IEnumerable<string> RelationSelection2PredicateIDs(IRelationSelection rl)
        {
            var usedPredicateIDs = rl switch
            {
                RelationSelectionAll _ => new string[] { "*" },
                RelationSelectionNone _ => Array.Empty<string>(),
                RelationSelectionWithPredicate p => p.PredicateIDs,
                RelationSelectionFrom _ => new string[] { "*" },
                RelationSelectionTo _ => new string[] { "*" },
                RelationSelectionSpecific s => s.Specifics.Select(t => t.predicateID).Distinct(),
                _ => throw new NotImplementedException("")
            };
            return usedPredicateIDs;
        }


        public UsageTrackingBaseRelationModel(IBaseRelationModel model, ScopedLifetimeAccessor scopedLifetimeAccessor)
        {
            this.model = model;
            this.scopedLifetimeAccessor = scopedLifetimeAccessor;
        }

        public async Task<IEnumerable<Relation>[]> GetRelations(IRelationSelection rl, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            
            TrackRelationPredicateUsage(RelationSelection2PredicateIDs(rl), layerIDs);
            return await model.GetRelations(rl, layerIDs, trans, atTime, generatedDataHandling);
        }

        public async Task<(bool changed, Guid changesetID)> BulkUpdate(IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid? existingRelationID, Guid newRelationID, bool mask)> inserts, IList<(Guid fromCIID, Guid toCIID, string predicateID, Guid existingRelationID, Guid newRelationID, bool mask)> removes, string layerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans)
        {
            TrackRelationPredicateUsage(inserts.Select(i => i.predicateID).Concat(removes.Select(r => r.predicateID)).Distinct(), new string[] { layerID });
            return await model.BulkUpdate(inserts, removes, layerID, dataOrigin, changesetProxy, trans);
        }

        public async Task<IEnumerable<Relation>> GetRelationsOfChangeset(Guid changesetID, bool getRemoved, IModelContext trans)
        {
            return await model.GetRelationsOfChangeset(changesetID, getRemoved, trans);
        }

        public async Task<ISet<string>> GetPredicateIDs(IRelationSelection rs, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IGeneratedDataHandling generatedDataHandling)
        {
            TrackRelationPredicateUsage(RelationSelection2PredicateIDs(rs), layerIDs);
            return await model.GetPredicateIDs(rs, layerIDs, trans, atTime, generatedDataHandling);
        }
    }
}
