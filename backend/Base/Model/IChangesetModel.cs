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
        public interface IChangesetSelection
        {

        }

        public class ChangesetSelectionSingleCI : IChangesetSelection
        {
            public readonly Guid ciid;

            public ChangesetSelectionSingleCI(Guid ciid)
            {
                this.ciid = ciid;
            }
        }

        public class ChangesetSelectionAllCIs : IChangesetSelection
        {

        }

        Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans, DateTimeOffset? timestamp = null);
        Task<Changeset> GetChangeset(Guid id, NpgsqlTransaction trans);
        Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IChangesetSelection cs, NpgsqlTransaction trans, int? limit = null);   }
}
