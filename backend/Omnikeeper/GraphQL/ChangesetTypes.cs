using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System.Linq;

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
        public ChangesetType(IDataLoaderContextAccessor dataLoaderContextAccessor, ILayerModel layerModel)
        {
            Field("id", x => x.ID);
            Field(x => x.Timestamp);
            Field(x => x.User, type: typeof(UserInDatabaseType));
            Field(x => x.LayerID);
            Field(x => x.DataOrigin, type: typeof(DataOriginGQL));
            Field<LayerType>("layer",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerID = context.Source!.LayerID; 
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var loader = dataLoaderContextAccessor.Context.GetOrAddLoader($"GetAllLayers_{timeThreshold}", () => layerModel.GetLayers(userContext.Transaction, timeThreshold));
                return loader.LoadAsync().Then(layers => layers.FirstOrDefault(l => l.ID == layerID));
            });
            FieldAsync<ChangesetStatisticsType>("statistics",
            resolve: async (context) =>
            {
                // TODO: use dataloader
                var statisticsModel = context.RequestServices!.GetRequiredService<IChangesetStatisticsModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source!.ID;
                return await statisticsModel.GetStatistics(changesetID, userContext.Transaction);
            });
            FieldAsync<ListGraphType<CIAttributeType>>("attributes",
            resolve: async (context) =>
            {
                // TODO: use dataloader
                var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source!.ID;
                return await attributeModel.GetAttributesOfChangeset(changesetID, false, userContext.Transaction);
            });
            FieldAsync<ListGraphType<CIAttributeType>>("removedAttributes",
            resolve: async (context) =>
            {
                // TODO: use dataloader
                var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source!.ID;
                return await attributeModel.GetAttributesOfChangeset(changesetID, true, userContext.Transaction);
            });
            FieldAsync<ListGraphType<RelationType>>("relations",
            resolve: async (context) =>
            {
                // TODO: use dataloader
                var relationModel = context.RequestServices!.GetRequiredService<IRelationModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source!.ID;
                return await relationModel.GetRelationsOfChangeset(changesetID, false, userContext.Transaction);
            });
            FieldAsync<ListGraphType<RelationType>>("removedRelations",
            resolve: async (context) =>
            {
                // TODO: use dataloader
                var relationModel = context.RequestServices!.GetRequiredService<IRelationModel>();
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var changesetID = context.Source!.ID;
                return await relationModel.GetRelationsOfChangeset(changesetID, true, userContext.Transaction);
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
