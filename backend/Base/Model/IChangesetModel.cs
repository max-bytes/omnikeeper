using Landscape.Base.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace Landscape.Base.Model
{
    public interface IChangesetModel
    {
        Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans);
        Task<Changeset> GetChangeset(long id, NpgsqlTransaction trans);
        Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IncludeRelationDirections ird, Guid ciid, NpgsqlTransaction trans, int? limit = null);
        Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IncludeRelationDirections ird, NpgsqlTransaction trans, int? limit = null);    }
}
