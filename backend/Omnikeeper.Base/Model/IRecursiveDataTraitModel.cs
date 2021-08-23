using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRecursiveDataTraitModel
    {
        Task<IEnumerable<RecursiveTrait>> GetRecursiveTraits(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);
        Task<RecursiveTrait> GetRecursiveTrait(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, RecursiveTrait)> TryToGetRecursiveTrait(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);

        Task<(RecursiveTrait recursiveTrait, bool changed)> InsertOrUpdate(string id, IEnumerable<TraitAttribute> requiredAttributes, IEnumerable<TraitAttribute>? optionalAttributes, IEnumerable<TraitRelation>? requiredRelations, IEnumerable<string>? requiredTraits, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
