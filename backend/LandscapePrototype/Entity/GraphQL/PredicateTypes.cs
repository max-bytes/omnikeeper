using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class PredicateType : ObjectGraphType<Predicate>
    {
        public PredicateType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.WordingTo);
        }
    }
}
