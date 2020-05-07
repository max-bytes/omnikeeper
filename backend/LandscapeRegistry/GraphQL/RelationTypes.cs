using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Model.Decorators;

namespace LandscapeRegistry.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(ILayerModel layerModel)
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
                var userContext = context.UserContext as RegistryUserContext;
                var layerstackIDs = context.Source.LayerStackIDs;
                return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
            });
        }
    }

    public class RelationStateType : EnumerationGraphType<RelationState>
    {
    }

    public class CompactRelatedCIType : ObjectGraphType<CompactRelatedCI>
    {
        public CompactRelatedCIType(ILayerModel layerModel)
        {
            Field("ci", x => x.CI, type: typeof(CompactCIType));
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.PredicateWording);
            Field(x => x.ChangesetID);
            Field(x => x.LayerID);
            Field(x => x.LayerStackIDs);
            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var userContext = context.UserContext as RegistryUserContext;
                var layerstackIDs = context.Source.LayerStackIDs;
                return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
            });
        }
    }

}
