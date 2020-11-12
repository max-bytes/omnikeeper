﻿using Npgsql;
using Omnikeeper.Base.Entity;
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

        Task<Changeset> CreateChangeset(long userID, IModelContext trans, DateTimeOffset? timestamp = null);
        Task<Changeset?> GetChangeset(Guid id, IModelContext trans);
        Task<IEnumerable<Changeset>> GetChangesetsInTimespan(DateTimeOffset from, DateTimeOffset to, LayerSet layers, IChangesetSelection cs, IModelContext trans, int? limit = null);

        Task<int> ArchiveUnusedChangesetsOlderThan(DateTimeOffset threshold, IModelContext trans);
    }
}
