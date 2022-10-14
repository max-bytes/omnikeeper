using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Base.Model.TraitBased;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.GraphQL
{
    public interface IDataLoaderService
    {
        IDataLoaderResult<UserInDatabase> SetupAndLoadUser(long userID, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<Changeset>> SetupAndLoadChangesets(ISet<Guid> ids, IModelContext trans);
        IDataLoaderResult<IEnumerable<EffectiveTrait>> SetupAndLoadEffectiveTraits(MergedCI ci, ITraitSelection traitSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, EffectiveTrait>> SetupAndLoadEffectiveTraits(ICIIDSelection ciids, ITrait trait, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> SetupAndLoadMergedAttributes(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<MergedCI>> SetupAndLoadMergedCIs(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, string>> SetupAndLoadCINames(ICIIDSelection ciidSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, Changeset>> SetupAndLoadLatestRelevantChangesetPerCI(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, IPredicateSelection predicateSelection, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, Changeset>> SetupAndLoadLatestRelevantChangesetPerTraitEntity(ICIIDSelection ciidSelection, bool includingRemovedTraitEntities, bool filterOutNonTraitEntityCIs, TraitEntityModel traitEntityModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<string, LayerData>> SetupAndLoadAllLayers(TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<MergedRelation>> SetupAndLoadRelation(IRelationSelection rs, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
    }
}
