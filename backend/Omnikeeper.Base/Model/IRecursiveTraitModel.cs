using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRecursiveTraitModel
    {
        Task<RecursiveTraitSet> GetRecursiveTraitSet(IModelContext trans, TimeThreshold timeThreshold);
        Task<RecursiveTraitSet> SetRecursiveTraitSet(RecursiveTraitSet traitSet, IModelContext trans);
    }
}
