using GraphQL.Types;
using GraphQL.Utilities;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;

namespace Omnikeeper.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType()
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);
            Field(x => x.Origin, type: typeof(DataOriginGQL));
        }
    }

    public class RelationStateType : EnumerationGraphType<RelationState>
    {
    }

    public class CompactRelatedCIType : ObjectGraphType<CompactRelatedCI>
    {
        public CompactRelatedCIType()
        {
            Field(x => x.RelationID);
            Field("ci", x => x.CI, type: typeof(CompactCIType));
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.IsForwardRelation);
            Field(x => x.ChangesetID);
            Field(x => x.Origin, type: typeof(DataOriginGQL));
            Field(x => x.LayerID);
            Field(x => x.LayerStackIDs);
            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source.LayerStackIDs;
                return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
            });
        }
    }

}
