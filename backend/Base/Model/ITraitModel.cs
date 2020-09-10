using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITraitModel
    {
        Task<TraitSet> GetTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold);
        Task<TraitSet> SetTraitSet(TraitSet traitSet, NpgsqlTransaction trans);
    }
}
