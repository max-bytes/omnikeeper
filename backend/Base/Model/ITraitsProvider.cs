using Landscape.Base.Entity;
using Npgsql;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ITraitsProvider
    {
        public Task<Traits> GetTraits(NpgsqlTransaction trans);
    }
}
