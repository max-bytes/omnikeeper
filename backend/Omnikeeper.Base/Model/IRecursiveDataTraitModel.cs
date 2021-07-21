using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRecursiveDataTraitModel
    {
        Task<IEnumerable<RecursiveTrait>> GetRecursiveTraits(IModelContext trans, TimeThreshold timeThreshold);
        Task<RecursiveTrait> GetRecursiveTrait(string id, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, RecursiveTrait)> TryToGetRecursiveTrait(string name, TimeThreshold timeThreshold, IModelContext trans);
    }
}
