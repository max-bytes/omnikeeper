using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Immutable;
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
        public ChangesetType(IDataLoaderService dataLoaderService, ChangesetDataModel changesetDataModel, ITraitsHolder traitsHolder)
        {
            Field("id", x => x.ID);
            Field(x => x.Timestamp);
            Field(x => x.UserID);
            Field<UserInDatabaseType>("user")
                .Resolve(context =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                return dataLoaderService.SetupAndLoadUser(context.Source!.UserID, timeThreshold, userContext.Transaction);
            });
            Field(x => x.LayerID);
            Field(x => x.DataOrigin, type: typeof(DataOriginGQL));
            Field<LayerDataType>("layer")
                .Resolve(context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerID = context.Source!.LayerID;
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);

                    return dataLoaderService.SetupAndLoadAllLayers(timeThreshold, userContext.Transaction)
                        .Then(layers => layers.GetOrWithClass(layerID, null));
                });
            Field<ChangesetStatisticsType>("statistics")
                .ResolveAsync(async context =>
                {
                    // TODO: use dataloader
                    var statisticsModel = context.RequestServices!.GetRequiredService<IChangesetStatisticsModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    return await statisticsModel.GetStatistics(changesetID, userContext.Transaction);
                });

            Field<ListGraphType<ChangesetCIAttributesType>>("ciAttributes")
                .ResolveAsync(async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var attributes = await attributeModel.GetAttributesOfChangeset(changesetID, false, userContext.Transaction);
                    var grouped = attributes.GroupBy(a => a.CIID);
                    return grouped.Select(g => new ChangesetCIAttributes(g.Key, g));
                });
            Field<ListGraphType<ChangesetCIAttributesType>>("removedCIAttributes")
                .ResolveAsync(async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var attributes = await attributeModel.GetAttributesOfChangeset(changesetID, true, userContext.Transaction);
                    var grouped = attributes.GroupBy(a => a.CIID);
                    return grouped.Select(g => new ChangesetCIAttributes(g.Key, g));
                });

            Field<ListGraphType<RelationType>>("relations")
                .ResolveAsync(async (context) =>
                {
                    // TODO: use dataloader
                    var relationModel = context.RequestServices!.GetRequiredService<IRelationModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    return await relationModel.GetRelationsOfChangeset(changesetID, false, userContext.Transaction);
                });
            Field<ListGraphType<RelationType>>("removedRelations")
                .ResolveAsync(async (context) =>
                {
                    // TODO: use dataloader
                    var relationModel = context.RequestServices!.GetRequiredService<IRelationModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    return await relationModel.GetRelationsOfChangeset(changesetID, true, userContext.Transaction);
                });

            // TODO: finish, not production ready yet
            Field<ListGraphType<ChangesetCIChangesType>>("ciChanges")
                .ResolveAsync(async (context) =>
                {
                    // TODO: use dataloader
                    var attributeModel = context.RequestServices!.GetRequiredService<IBaseAttributeModel>();
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var attributes = await attributeModel.GetAttributesOfChangeset(changesetID, false, userContext.Transaction);
                    var changedCIIDs = attributes.Select(a => a.CIID).ToHashSet();
                    var changedAttributeNames = attributes.Select(a => a.Name).ToHashSet();
                    // TODO: test minimum change
                    var previousDT = context.Source.Timestamp.Subtract(new TimeSpan(10)); // maximum supported precision by postgres' timestamptz is 1 microsecond, which is 1000 nanoseconds and 10 times as much as C# supports with DateTimeOffset
                    var previousTT = TimeThreshold.BuildAtTime(previousDT);
                    var previousAttributes = attributeModel.GetAttributes(SpecificCIIDsSelection.Build(changedCIIDs), NamedAttributesSelection.Build(changedAttributeNames), context.Source!.LayerID, userContext.Transaction, previousTT);
                    var previousAttributesByCIIDAndAttributeNameLookup = await previousAttributes.ToDictionaryAsync(a => (a.CIID, a.Name));

                    var removedAttributes = await attributeModel.GetAttributesOfChangeset(changesetID, true, userContext.Transaction);

                    // TODO: relations

                    return attributes.Select(a =>
                    {
                        if (previousAttributesByCIIDAndAttributeNameLookup.TryGetValue((a.CIID, a.Name), out var previousAttribute))
                            return (before: (CIAttribute?)previousAttribute, after: (CIAttribute?)a);
                        else
                            return (null, a);
                    })
                        .Concat(removedAttributes.Select(ra => (before: (CIAttribute?)ra, after: (CIAttribute?)null))) // add in removed attributes
                        .GroupBy(t => (t.before?.CIID ?? t.after?.CIID)!.Value) // we know either before or after is non-null, but compiler needs convincing
                        .Select(g => new ChangesetCIChanges(g.Key, g.Select(g => new CIAttributeChange(g.before, g.after))));
                });

            Field<GuidGraphType>("dataCIID")
                .ResolveAsync(async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var layerID = context.Source!.LayerID;
                    var layerset = new LayerSet(layerID);

                    // TODO: use data loader
                    var (changesetData, ciid) = await changesetDataModel.GetSingleByDataID(changesetID.ToString(), layerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    if (ciid == default)
                        return null;
                    return ciid;
                });

            Field<MergedCIType>("data")
                .ResolveAsync(async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var changesetID = context.Source!.ID;
                    var layerID = context.Source!.LayerID;
                    var layerset = new LayerSet(layerID);

                    // TODO: use data loader
                    var (changesetData, ciid) = await changesetDataModel.GetSingleByDataID(changesetID.ToString(), layerset, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    if (ciid == default)
                        return null;

                    IAttributeSelection forwardAS = MergedCIType.ForwardInspectRequiredAttributes(context, traitsHolder, userContext.Transaction, userContext.GetTimeThreshold(context.Path));
                    var finalCI = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(ciid), forwardAS, layerset, userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(cis => cis.FirstOrDefault() ?? new MergedCI(ciid, null, layerset, userContext.GetTimeThreshold(context.Path), ImmutableDictionary<string, MergedCIAttribute>.Empty));

                    return finalCI;
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
        public ChangesetCIAttributesType(IDataLoaderService dataLoaderService)
        {
            Field("ciid", x => x.CIID);
            Field<StringGraphType>("ciName")
                .Resolve((context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.CIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field("attributes", x => x.Attributes, type: typeof(ListGraphType<CIAttributeType>));
        }
    }

    public class ChangesetCIChangesType : ObjectGraphType<ChangesetCIChanges>
    {
        public ChangesetCIChangesType(IDataLoaderService dataLoaderService)
        {
            Field("ciid", x => x.CIID);
            Field<StringGraphType>("ciName")
                .Resolve((context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.CIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field("attributeChanges", x => x.AttributeChanges, type: typeof(ListGraphType<CIAttributeChangeType>));
        }
    }

    public class CIAttributeChangeType : ObjectGraphType<CIAttributeChange>
    {
        public CIAttributeChangeType()
        {
            Field("before", x => x.Before, type: typeof(CIAttributeType), nullable: true);
            Field("after", x => x.After, type: typeof(CIAttributeType), nullable: true);
            Field("name", x => x.Name);
        }
    }
}
