using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Collections.Immutable;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(IDataLoaderService dataLoaderService, ITraitsProvider traitsProvider)
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.ChangesetID);
            Field(x => x.Mask);

            Field<StringGraphType>("fromCIName")
                .Resolve((context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.FromCIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field<StringGraphType>("toCIName")
                .Resolve((context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.ToCIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field<MergedCIType>("toCI")
            .ResolveAsync(async (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);

                IAttributeSelection attributeSelection = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, userContext.Transaction, timeThreshold);

                return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.ToCIID), attributeSelection, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t =>
                    {
                        // NOTE: we kind of know that the CI must exist, we return an empty MergedCI object if the CI query returns null
                        return t.FirstOrDefault() ?? new MergedCI(context.Source!.ToCIID, null, layerSet, timeThreshold, ImmutableDictionary<string, MergedCIAttribute>.Empty);
                    });
            });
            Field<MergedCIType>("fromCI")
            .ResolveAsync(async (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);

                IAttributeSelection attributeSelection = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, userContext.Transaction, timeThreshold);

                return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.FromCIID), attributeSelection, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t =>
                    {
                        // NOTE: we kind of know that the CI must exist, we return an empty MergedCI object if the CI query returns null
                        return t.FirstOrDefault() ?? new MergedCI(context.Source!.ToCIID, null, layerSet, timeThreshold, ImmutableDictionary<string, MergedCIAttribute>.Empty);
                    });
            });
        }
    }

    public class MergedRelationType : ObjectGraphType<MergedRelation>
    {
        public MergedRelationType(IDataLoaderService dataLoaderService)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Relation, type: typeof(RelationType));

            Field<ListGraphType<LayerDataType>>("layerStack")
            .Resolve((context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);

                return dataLoaderService.SetupAndLoadAllLayers(timeThreshold, userContext.Transaction)
                    .Then(layers => layers
                        .Where(kv => layerstackIDs.Contains(kv.Key))
                        .OrderBy(kv => layerstackIDs.IndexOf(kv.Key))
                        .Select(kv => kv.Value)
                    );
            });
        }
    }
}
