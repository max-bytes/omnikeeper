using GraphQL.Types;
using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL.Types
{
    public class MutateReturn
    {
        public IEnumerable<MergedCI> AffectedCIs { get; private set; }
        public MutateReturn(IEnumerable<MergedCI> affectedCIs)
        {
            AffectedCIs = affectedCIs;
        }
    }
    public class MutateReturnType : ObjectGraphType<MutateReturn>
    {
        public MutateReturnType()
        {
            Field(x => x.AffectedCIs, nullable: true, type: typeof(ListGraphType<MergedCIType>));
        }
    }

    public class CreateCIsReturn
    {
        public IEnumerable<Guid> CIIDs { get; private set; }
        public CreateCIsReturn(IEnumerable<Guid> ciids)
        {
            CIIDs = ciids;
        }
    }
    public class CreateCIsReturnType : ObjectGraphType<CreateCIsReturn>
    {
        public CreateCIsReturnType()
        {
            Field("ciids", x => x.CIIDs, type: typeof(ListGraphType<GuidGraphType>));
        }
    }

    public class InsertChangesetDataReturn
    {
        public Guid ChangesetDataCIID { get; private set; }
        public InsertChangesetDataReturn(Guid changesetDataCIID)
        {
            ChangesetDataCIID = changesetDataCIID;
        }
    }
    public class InsertChangesetDataReturnType : ObjectGraphType<InsertChangesetDataReturn>
    {
        public InsertChangesetDataReturnType()
        {
            Field("changesetDataCIID", x => x.ChangesetDataCIID);
        }
    }

    public record class BulkReplaceTraitEntityReturn(Changeset? Changeset, bool Success, bool IsNoOp);
    public class BulkReplaceTraitEntityReturnType : ObjectGraphType<BulkReplaceTraitEntityReturn>
    {
        public BulkReplaceTraitEntityReturnType()
        {
            Field("changeset", x => x.Changeset, nullable: true, type: typeof(ChangesetType));
            Field("success", x => x.Success);
            Field("isNoOp", x => x.IsNoOp);
        }
    }
}
