using GraphQL.Types;
using Landscape.Base.Entity;
using static Landscape.Base.Model.IPredicateModel;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class PredicateStateType : EnumerationGraphType<PredicateState>
    {
    }
    public class PredicateType : ObjectGraphType<Predicate>
    {
        public PredicateType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.WordingTo);
            Field(x => x.State, type: typeof(PredicateStateType));
        }
    }

    public class PredicateStateFilterType : EnumerationGraphType<PredicateStateFilter>
    {

    }
}
