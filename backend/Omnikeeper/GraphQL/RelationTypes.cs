using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(IDataLoaderService dataLoaderService, IAttributeModel attributeModel, ICIIDModel ciidModel, ICIModel ciModel)
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.ChangesetID);

            Field<StringGraphType>("fromCIName",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.FromCIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field<StringGraphType>("toCIName",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.ToCIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field<MergedCIType>("toCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);
                return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.ToCIID), ciModel, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t => t.First());
            });
            Field<MergedCIType>("fromCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);
                return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.FromCIID), ciModel, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t => t.First());
            });
        }
    }

    public class MergedRelationType : ObjectGraphType<MergedRelation>
    {
        public MergedRelationType(IDataLoaderService dataLoaderService, ILayerModel layerModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.LayerID);
            Field(x => x.Relation, type: typeof(RelationType));

            Field<ListGraphType<LayerType>>("layerStack",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);

                return dataLoaderService.SetupAndLoadAllLayers(layerModel, timeThreshold, userContext.Transaction)
                    .Then(layers => layers
                        .Where(l => layerstackIDs.Contains(l.ID))
                        .OrderBy(l => layerstackIDs.IndexOf(l.ID))
                    );
            });
        }
    }
}
