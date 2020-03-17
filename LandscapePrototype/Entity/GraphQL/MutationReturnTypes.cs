using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class MutateReturn
    {
        public IEnumerable<CIAttribute> InsertedAttributes { get; private set; }
        public IEnumerable<CIAttribute> RemovedAttributes { get; private set; }
        public IEnumerable<Relation> InsertedRelations { get; private set; }
        
        public IEnumerable<CI> AffectedCIs { get; private set; }
        public static MutateReturn Build(IEnumerable<CIAttribute> insertedAttributes,
            IEnumerable<CIAttribute> removedAttributes, IEnumerable<Relation> insertedRelations, IEnumerable<CI> affectedCIs)
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
            Field(x => x.AffectedCIs, type: typeof(ListGraphType<CIType>));
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
