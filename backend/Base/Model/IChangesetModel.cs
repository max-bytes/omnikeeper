using Landscape.Base.Entity;
using Npgsql;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IChangesetModel
    {
        Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans);
    }
}
