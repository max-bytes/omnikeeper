using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IChangesetModel
    {
        public interface IChangesetSelection
        {

        }

        public class ChangesetSelectionSpecificCIs : IChangesetSelection
        {
            public Guid[] CIIDs { get; }
            private ChangesetSelectionSpecificCIs(IEnumerable<Guid> ciids)
            {
                CIIDs = ciids.ToArray();
            }

            public bool Contains(Guid ciid) => CIIDs.Contains(ciid);

            public static ChangesetSelectionSpecificCIs Build(IEnumerable<Guid> ciids)
            {
                if (ciids.IsEmpty()) throw new Exception("Empty ChangesetSelectionMultipleCIs not allowed");
                return new ChangesetSelectionSpecificCIs(ciids);
            }
            public static ChangesetSelectionSpecificCIs Build(params Guid[] ciids)
            {
                if (ciids.IsEmpty()) throw new Exception("Empty ChangesetSelectionMultipleCIs not allowed");
                return new ChangesetSelectionSpecificCIs(ciids);
            }
        }

        public class ChangesetSelectionAllCIs : IChangesetSelection
        {

        }

        Task<Changeset> CreateChangeset(long userID, string layerID, DataOriginV1 dataOrigin, IModelContext trans, DateTimeOffset? timestamp = null);
        Task<Changeset?> GetChangeset(Guid id, IModelContext trans);
        Task<IReadOnlyList<Changeset>> GetChangesets(ISet<Guid> ids, IModelContext trans);
        Task<IReadOnlySet<Guid>> GetCIIDsAffectedByChangeset(Guid changesetID, IModelContext trans);
        Task<IReadOnlyList<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IChangesetSelection cs, IModelContext trans, int? limit = null);
        Task<Changeset?> GetLatestChangesetForLayer(string layerID, IModelContext trans);

        [Obsolete("Archiving full-changesets-only is not necessary anymore, consider writing a simpler method that just removes outdated attributes/relations")]
        Task<int> ArchiveUnusedChangesetsOlderThan(DateTimeOffset threshold, IModelContext trans);
        Task<int> DeleteEmptyChangesets(IModelContext trans);
        Task<long> GetNumberOfChangesets(IModelContext trans);
        Task<long> GetNumberOfChangesets(string layerID, IModelContext trans);
        Task<IReadOnlyList<Changeset>> GetChangesetsAfter(Guid afterChangesetID, string[] layerIDs, IModelContext trans);
    }
}
