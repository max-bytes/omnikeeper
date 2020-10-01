using Landscape.Base.Entity;
using Landscape.Base.Utils;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Landscape.Base.Model.IRelationModel;

namespace Landscape.Base.Model
{
    public interface IChangesetModel
    {
        public interface IChangesetSelection
        {

        }

        public class ChangesetSelectionMultipleCIs : IChangesetSelection
        {
            public Guid[] CIIDs { get; }
            private ChangesetSelectionMultipleCIs(IEnumerable<Guid> ciids)
            {
                CIIDs = ciids.ToArray();
            }

            public bool Contains(Guid ciid) => CIIDs.Contains(ciid);

            public static ChangesetSelectionMultipleCIs Build(IEnumerable<Guid> ciids)
            {
                if (ciids.IsEmpty()) throw new Exception("Empty ChangesetSelectionMultipleCIs not allowed");
                return new ChangesetSelectionMultipleCIs(ciids);
            }
            public static ChangesetSelectionMultipleCIs Build(params Guid[] ciids)
            {
                if (ciids.IsEmpty()) throw new Exception("Empty ChangesetSelectionMultipleCIs not allowed");
                return new ChangesetSelectionMultipleCIs(ciids);
            }
        }

        public class ChangesetSelectionAllCIs : IChangesetSelection
        {

        }

        Task<Changeset> CreateChangeset(long userID, NpgsqlTransaction trans, DateTimeOffset? timestamp = null);
        Task<Changeset> GetChangeset(Guid id, NpgsqlTransaction trans);
        Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IChangesetSelection cs, NpgsqlTransaction trans, int? limit = null);

        Task<int> ArchiveUnusedChangesetsOlderThan(DateTimeOffset threshold, NpgsqlTransaction trans);
    }
}
