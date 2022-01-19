using GraphQL.Types;
using Omnikeeper.Base.Entity;

namespace Omnikeeper.GraphQL.Types
{
    public class StatisticsType : ObjectGraphType<Statistics>
    {
        public StatisticsType()
        {
            Field("cis", x => x.CIs);
            Field(x => x.ActiveAttributes);
            Field(x => x.ActiveRelations);
            Field(x => x.AttributeChanges);
            Field(x => x.RelationChanges);
            Field(x => x.Changesets);
            Field(x => x.Layers);
            Field(x => x.Predicates);
            Field(x => x.Traits);
            Field(x => x.Generators);
        }
    }

}
