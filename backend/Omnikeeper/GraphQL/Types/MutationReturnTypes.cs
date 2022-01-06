using GraphQL.Types;
using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GraphQL.Types
{
    public class MutateReturn
    {
        public IEnumerable<CIAttribute> InsertedAttributes { get; private set; }
        public IEnumerable<CIAttribute> RemovedAttributes { get; private set; }
        public IEnumerable<Relation> InsertedRelations { get; private set; }

        public IEnumerable<MergedCI> AffectedCIs { get; private set; }
        public MutateReturn(IEnumerable<CIAttribute> insertedAttributes,
            IEnumerable<CIAttribute> removedAttributes, IEnumerable<Relation> insertedRelations, IEnumerable<MergedCI> affectedCIs)
        {
            InsertedAttributes = insertedAttributes;
            RemovedAttributes = removedAttributes;
            InsertedRelations = insertedRelations;
            AffectedCIs = affectedCIs;
        }
    }
    public class MutateReturnType : ObjectGraphType<MutateReturn>
    {
        public MutateReturnType()
        {
            Field(x => x.InsertedAttributes, type: typeof(ListGraphType<CIAttributeType>));
            Field(x => x.RemovedAttributes, type: typeof(ListGraphType<CIAttributeType>));
            Field(x => x.InsertedRelations, type: typeof(ListGraphType<RelationType>));
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
}
