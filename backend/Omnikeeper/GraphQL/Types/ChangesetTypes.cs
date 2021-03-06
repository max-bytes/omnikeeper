using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
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
        public ChangesetType(IDataLoaderService dataLoaderService, ILayerDataModel layerDataModel, IAttributeModel attributeModel, ICIModel ciModel, ChangesetDataModel changesetDataModel)
        {
            Field("id", x => x.ID);
            Field(x => x.Timestamp);
            Field(x => x.User, type: typeof(UserInDatabaseType));
            Field(x => x.LayerID);
            Field(x => x.DataOrigin, type: typeof(DataOriginGQL));
            Field<LayerDataType>("layer",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerID = context.Source!.LayerID;
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);

                    return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, timeThreshold, userContext.Transaction)
                        .Then(layers => layers.GetOrWithClass(layerID, null));
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
            var attributes = FieldAsync<ListGraphType<CIAttributeType>>("attributes",
                resolve: async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    return await attributeModel.GetAttributesOfChangeset(changesetID, false, userContext.Transaction);
                });
            attributes.DeprecationReason = "Superseded by ciAttributes";
            var removedAttributes = FieldAsync<ListGraphType<CIAttributeType>>("removedAttributes",
                resolve: async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    return await attributeModel.GetAttributesOfChangeset(changesetID, true, userContext.Transaction);
                });
            removedAttributes.DeprecationReason = "Superseded by removedCIAttributes";

            FieldAsync<ListGraphType<ChangesetCIAttributesType>>("ciAttributes",
                resolve: async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var attributes = await attributeModel.GetAttributesOfChangeset(changesetID, false, userContext.Transaction);
                    var grouped = attributes.GroupBy(a => a.CIID);
                    return grouped.Select(g => new ChangesetCIAttributes(g.Key, g));
                });
            FieldAsync<ListGraphType<ChangesetCIAttributesType>>("removedCIAttributes",
                resolve: async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var attributes = await attributeModel.GetAttributesOfChangeset(changesetID, true, userContext.Transaction);
                    var grouped = attributes.GroupBy(a => a.CIID);
                    return grouped.Select(g => new ChangesetCIAttributes(g.Key, g));
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

            FieldAsync<GuidGraphType>("dataCIID",
                resolve: async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var layerID = context.Source!.LayerID;
                    var layerset = new LayerSet(layerID);

                    var (changesetData, ciid) = await changesetDataModel.GetSingleByDataID(changesetID.ToString(), layerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    if (ciid == default)
                        return null;
                    return ciid;
                });

            FieldAsync<MergedCIType>("data",
                resolve: async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var layerID = context.Source!.LayerID;
                    var layerset = new LayerSet(layerID);

                    // TODO: use data loader
                    var (changesetData, ciid) = await changesetDataModel.GetSingleByDataID(changesetID.ToString(), layerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    if (ciid == default)
                        return null;
                    return await ciModel.GetMergedCI(ciid, layerset, AllAttributeSelection.Instance, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
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

    public class ChangesetCIAttributesType : ObjectGraphType<ChangesetCIAttributes>
    {
        public ChangesetCIAttributesType(IDataLoaderService dataLoaderService, IAttributeModel attributeModel, ICIIDModel ciidModel)
        {
            Field("ciid", x => x.CIID);
            Field<StringGraphType>("ciName",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.CIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field("attributes", x => x.Attributes, type: typeof(ListGraphType<CIAttributeType>));
        }
    }

}
