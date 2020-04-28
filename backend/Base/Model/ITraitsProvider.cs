using Landscape.Base.Entity;
using Npgsql;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITraitsProvider
    {
        public Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans);
    }
}
