using GraphQL.Types;
using Landscape.Base.Entity;
using static Landscape.Base.Model.IPredicateModel;

namespace LandscapeRegistry.GraphQL
{
    public class PredicateType : ObjectGraphType<Predicate>
    {
        public PredicateType()
        {
            Field("id", x => x.ID);
            Field(x => x.WordingFrom);
            Field(x => x.WordingTo);
            Field(x => x.State, type: typeof(AnchorStateType));
        }
    }
}
