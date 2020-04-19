using GraphQL.Types;
using Landscape.Base.Entity;
using System;
using System.Collections.Generic;

namespace LandscapeRegistry.GraphQL
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
        public IEnumerable<Guid> CIIDs { get; private set; }
        public static CreateCIsReturn Build(IEnumerable<Guid> ciids)
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
            Field("ciids", x => x.CIIDs, type: typeof(ListGraphType<GuidGraphType>));
        }
    }

    //public class MutatePredicatesReturn
    //{
    //    public IEnumerable<Predicate> MutatedPredicates { get; private set; }
    //    public static MutatePredicatesReturn Build(IEnumerable<Predicate> mutatedPredicates)
    //    {
    //        return new MutatePredicatesReturn()
    //        {
    //            MutatedPredicates = mutatedPredicates
    //        };
    //    }
    //}
    //public class MutatePredicatesReturnType : ObjectGraphType<MutatePredicatesReturn>
    //{
    //    public MutatePredicatesReturnType()
    //    {
    //        Field(x => x.MutatedPredicates, type: typeof(ListGraphType<PredicateType>));
    //    }
    //}
}
