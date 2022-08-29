using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Authz;
using Omnikeeper.Model;
using System;

namespace Omnikeeper.GraphQL.Types
{
    public class LayerDataType : ObjectGraphType<LayerData>
    {
        public LayerDataType(CLConfigV1Model clConfigModel, IMetaConfigurationModel metaConfigurationModel, ILayerBasedAuthorizationService lbas)
        {
            Field("description", x => x.Description);
            Field("clConfigID", x => x.CLConfigID);
            FieldAsync<CLConfigType>("clConfig",
            resolve: async (context) =>
            {
                var userContext = context.GetUserContext();
                var clConfigID = context.Source.CLConfigID;

                var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(userContext.Transaction);
                var (clConfig, _) = await clConfigModel.GetSingleByDataID(clConfigID, metaConfiguration.ConfigLayerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                return clConfig;
            });
            Field("onlineInboundAdapterName", x => x.OIAReference);
            Field("id", x => x.LayerID);
            Field("color", x => x.Color);
            Field("generators", x => x.Generators);
            Field("state", x => Enum.Parse<AnchorState>(x.State), type: typeof(AnchorStateType));
            Field<BooleanGraphType>("writable",
            resolve: (context) =>
            {
                var userContext = context.GetUserContext();
                var isWritable = lbas.CanUserWriteToLayer(userContext.User, context.Source!.LayerID);
                return isWritable;
            });
            FieldAsync<BooleanGraphType>("isMetaConfigurationLayer",
            resolve: async (context) =>
            {
                var userContext = context.GetUserContext();

                return await metaConfigurationModel.IsLayerPartOfMetaConfiguration(context.Source!.LayerID, userContext.Transaction);
            });

        }
    }

    public class LayerSetType : ObjectGraphType<LayerSet>
    {
        public LayerSetType()
        {
            Field("ids", x => x.LayerIDs);
        }
    }


    public class LayerStatisticsType : ObjectGraphType<LayerStatistics>
    {
        public LayerStatisticsType()
        {
            Field("numActiveAttributes", x => x.NumActiveAttributes);
            Field("numAttributeChangesHistory", x => x.NumAttributeChangesHistory);
            Field("numActiveRelations", x => x.NumActiveRelations);
            Field("numRelationChangesHistory", x => x.NumRelationChangesHistory);
            Field("numLayerChangesetsHistory", x => x.NumLayerChangesetsHistory);
            Field("latestChange", x => x.LatestChange, type: typeof(DateTimeOffsetGraphType));
            Field("layer", x => x.LayerData, type: typeof(LayerDataType));
        }
    }
}
