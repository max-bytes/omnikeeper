using GraphQL.Types;
using Landscape.Base.Entity;
using System.Collections.Generic;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class MutateReturn
    {
        public IEnumerable<CIAttribute> InsertedAttributes { get; private set; }
        public IEnumerable<CIAttribute> RemovedAttributes { get; private set; }
        public IEnumerable<Relation> InsertedRelations { get; private set; }

        public IEnumerable<MergedCI> AffectedCIs { get; private set; }
        public static MutateReturn Build(IEnumerable<CIAttribute> insertedAttributes,
            IEnumerable<CIAttribute> removedAttributes, IEnumerable<Relation> insertedRelations, IEnumerable<MergedCI> affectedCIs)
        {
            return new MutateReturn()
            {
                InsertedAttributes = insertedAttributes,
                RemovedAttributes = removedAttributes,
                InsertedRelations = insertedRelations,
                AffectedCIs = affectedCIs
            };
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
        public IEnumerable<string> CIIDs { get; private set; }
        public static CreateCIsReturn Build(IEnumerable<string> ciids)
        {
            return new CreateCIsReturn()
            {
                CIIDs = ciids
            };
        }
    }
    public class CreateCIsReturnType : ObjectGraphType<CreateCIsReturn>
    {
        public CreateCIsReturnType()
        {
            Field(x => x.CIIDs, type: typeof(ListGraphType<StringGraphType>));
        }
    }
}
