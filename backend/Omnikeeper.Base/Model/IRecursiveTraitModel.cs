using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Npgsql;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IRecursiveTraitModel
    {
        Task<RecursiveTraitSet> GetRecursiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold);
        Task<RecursiveTraitSet> SetRecursiveTraitSet(RecursiveTraitSet traitSet, NpgsqlTransaction trans);
    }
}
