using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;

namespace Omnikeeper.GraphQL
{
    public class UserTypeType : EnumerationGraphType<UserType>
    {
    }

    public class UserInDatabaseType : ObjectGraphType<UserInDatabase>
    {
        public UserInDatabaseType()
        {
            Field("id", x => x.ID);
            Field(x => x.Username);
            Field(x => x.DisplayName);
            Field(x => x.Timestamp);
            Field("type", x => x.UserType, type: typeof(UserTypeType));
        }
    }

    public class ChangesetType : ObjectGraphType<Changeset>
    {
        public ChangesetType()
        {
            Field("id", x => x.ID);
            Field(x => x.Timestamp);
            Field(x => x.User, type: typeof(UserInDatabaseType));
            Field(x => x.LayerID);
            Field(x => x.DataOrigin, type: typeof(DataOriginGQL));
            FieldAsync<LayerType>("layer",
            resolve: async (context) =>
            {
                var layerModel = context.RequestServices.GetRequiredService<ILayerModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerID = context.Source.LayerID;
                return await layerModel.GetLayer(layerID, userContext.Transaction);
            });
            FieldAsync<ChangesetStatisticsType>("statistics",
            resolve: async (context) =>
            {
                var statisticsModel = context.RequestServices.GetRequiredService<IChangesetStatisticsModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source.ID;
                return await statisticsModel.GetStatistics(changesetID, userContext.Transaction);
            });
            FieldAsync<ListGraphType<CIAttributeType>>("attributes",
            resolve: async (context) =>
            {
                var attributeModel = context.RequestServices.GetRequiredService<IAttributeModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source.ID;
                return await attributeModel.GetAttributesOfChangeset(changesetID, userContext.Transaction);
            });
            FieldAsync<ListGraphType<RelationType>>("relations",
            resolve: async (context) =>
            {
                var relationModel = context.RequestServices.GetRequiredService<IRelationModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source.ID;
                return await relationModel.GetRelationsOfChangeset(changesetID, userContext.Transaction);
            });
        }
    }

    public class ChangesetStatisticsType : ObjectGraphType<ChangesetStatistics>
    {
        public ChangesetStatisticsType()
        {
            Field(x => x.NumAttributeChanges);
            Field(x => x.NumRelationChanges);
            Field("id", x => x.ChangesetID);
        }
    }

}
