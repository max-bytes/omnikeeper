using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRelationModel : IBaseRelationModel
    {
        Task<IEnumerable<MergedRelation>> GetMergedRelations(IRelationSelection rl, LayerSet layerset, IModelContext trans, TimeThreshold atTime);
        Task<MergedRelation?> GetMergedRelation(Guid fromCIID, Guid toCIID, string predicateID, LayerSet layerset, IModelContext trans, TimeThreshold atTime);
    }
}
