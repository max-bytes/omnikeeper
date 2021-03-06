using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.GraphQL
{
    public interface IDataLoaderService
    {
        IDataLoaderResult<IEnumerable<Changeset>> SetupAndLoadChangesets(ISet<Guid> ids, IChangesetModel changesetModel, IModelContext trans);
        IDataLoaderResult<IEnumerable<EffectiveTrait>> SetupAndLoadEffectiveTraits(MergedCI ci, ITraitSelection traitSelection, IEffectiveTraitModel traitModel, ITraitsProvider traitsProvider, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, IDictionary<string, MergedCIAttribute>>> SetupAndLoadMergedAttributes(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, IAttributeModel attributeModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<MergedCI>> SetupAndLoadMergedCIs(ICIIDSelection ciidSelection, IAttributeSelection attributeSelection, ICIModel ciModel, IAttributeModel attributeModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<Guid, string>> SetupAndLoadCINames(ICIIDSelection ciidSelection, IAttributeModel attributeModel, ICIIDModel ciidModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IDictionary<string, LayerData>> SetupAndLoadAllLayers(ILayerDataModel layerDataModel, TimeThreshold timeThreshold, IModelContext trans);
        IDataLoaderResult<IEnumerable<MergedRelation>> SetupAndLoadRelation(IRelationSelection rs, IRelationModel relationModel, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
    }
}
