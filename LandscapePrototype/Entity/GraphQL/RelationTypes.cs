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
        public RelationType(LayerModel layerModel)
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.LayerID);
            Field(x => x.LayerStackIDs);
            Field(x => x.Predicate);
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);

            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as LandscapeUserContext;
                var layerstackIDs = context.Source.LayerStackIDs;
                return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
            });
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
            Field(x => x.IsForward);
        }
    }

}
