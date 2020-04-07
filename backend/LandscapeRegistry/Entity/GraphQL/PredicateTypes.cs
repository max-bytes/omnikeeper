using GraphQL.Types;
using Landscape.Base.Entity;

namespace LandscapeRegistry.Entity.GraphQL
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
