using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IRecursiveTraitModel
    {
        Task<RecursiveTraitSet> GetRecursiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold);
        Task<RecursiveTraitSet> SetRecursiveTraitSet(RecursiveTraitSet traitSet, NpgsqlTransaction trans);
    }
}
