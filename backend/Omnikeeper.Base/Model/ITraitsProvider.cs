using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITraitsProvider
    {
        Task<TraitSet> GetActiveTraitSet(IModelContext trans, TimeThreshold timeThreshold);
        Task<Trait?> GetActiveTrait(string traitName, IModelContext trans, TimeThreshold timeThreshold);
    }
}
