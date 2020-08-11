using GraphQL.Types;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Model.Decorators;

namespace LandscapeRegistry.GraphQL
{
    // not needed (yet)
    //public class MergedRelationType : ObjectGraphType<MergedRelation>
    //{
    //    public MergedRelationType(ILayerModel layerModel)
    //    {
    //        Field(x => x.LayerID);
    //        Field(x => x.LayerStackIDs);
    //        Field(x => x.Relation, type: typeof(RelationType));

    //        FieldAsync<ListGraphType<LayerType>>("layerStack",
    //        resolve: async (context) =>
    //        {
    //            var userContext = context.UserContext as RegistryUserContext;
    //            var layerstackIDs = context.Source.LayerStackIDs;
    //            return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
    //        });
    //    }
    //}

    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType()
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.Predicate, type: typeof(PredicateType));
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);
        }
    }

    public class RelationStateType : EnumerationGraphType<RelationState>
    {
    }

    public class CompactRelatedCIType : ObjectGraphType<CompactRelatedCI>
    {
        public CompactRelatedCIType(ILayerModel layerModel)
        {
            Field(x => x.RelationID);
            Field("ci", x => x.CI, type: typeof(CompactCIType));
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.PredicateWording);
            Field(x => x.IsForwardRelation);
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
