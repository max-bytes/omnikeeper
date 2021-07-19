﻿using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL
{
    public class PredicateConstraintsType : ObjectGraphType<PredicateConstraints>
    {
        public PredicateConstraintsType()
        {
            Field(x => x.PreferredTraitsFrom);
            Field(x => x.PreferredTraitsTo);
        }
    }

    public class PredicateType : ObjectGraphType<Predicate>
    {
        public PredicateType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.Constraints, type: typeof(PredicateConstraintsType));
            Field(x => x.WordingTo);
        }
    }

    //public class DirectedPredicateType : ObjectGraphType<DirectedPredicate>
    //{
    //    public DirectedPredicateType()
    //    {
    //        Field(x => x.PredicateID);
    //        Field(x => x.Wording);
    //        Field(x => x.Forward);
    //    }
    //}
}
