using GraphQL.Types;
using Landscape.Base.Entity;
using LandscapeRegistry.Model.Cached;

namespace LandscapeRegistry.Entity.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(CachedLayerModel layerModel)
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.LayerID);
            Field(x => x.LayerStackIDs);
            Field(x => x.Predicate, type: typeof(PredicateType));
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
            Field("ciid", x => x.CIID);
            Field(x => x.IsForward);
        }
    }

}
