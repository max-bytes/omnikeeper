using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITraitsProvider
    {
        Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold);
        Task<ITrait?> GetActiveTrait(string traitID, IModelContext trans, TimeThreshold timeThreshold);
    }
}
