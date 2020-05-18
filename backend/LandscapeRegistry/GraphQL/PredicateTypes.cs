using GraphQL.Types;
using Landscape.Base.Entity;

namespace LandscapeRegistry.GraphQL
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
            Field(x => x.State, type: typeof(AnchorStateType));
        }
    }
}
