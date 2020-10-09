using Omnikeeper.Base.Entity;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IChangesetProxy
    {
        Task<Changeset> GetChangeset(NpgsqlTransaction trans);
        DateTimeOffset Timestamp { get; }
    }

}
