using GraphQL.Types;
using LandscapePrototype.Entity.AttributeValues;
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(CIModel ciModel, LayerModel layerModel)
        {
            Field(x => x.ActivationTime);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.LayerID);
            Field(x => x.Predicate);
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);
        }
    }

    public class RelationStateType : EnumerationGraphType<RelationState>
    {
    }

    public class RelatedCIType : ObjectGraphType<RelatedCI>
    {
        public RelatedCIType()
        {
            Field(x => x.Relation, type: typeof(RelationType));
            Field("ci", x => x.CI, type: typeof(CIType));
        }
    }

}
