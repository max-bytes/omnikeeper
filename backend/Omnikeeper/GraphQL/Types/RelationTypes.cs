using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using System.Linq;

namespace Omnikeeper.GraphQL.Types
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType(IDataLoaderService dataLoaderService, IAttributeModel attributeModel, ICIIDModel ciidModel, ICIModel ciModel, ITraitsProvider traitsProvider)
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.ChangesetID);
            Field(x => x.Mask);

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
            FieldAsync<MergedCIType>("toCI",
            resolve: async (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);

                IAttributeSelection attributeSelection = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, userContext.Transaction, timeThreshold);

                return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.ToCIID), attributeSelection, false, ciModel, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t => t.First());
            });
            FieldAsync<MergedCIType>("fromCI",
            resolve: async (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);

                IAttributeSelection attributeSelection = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, userContext.Transaction, timeThreshold);

                return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.FromCIID), attributeSelection, false, ciModel, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t => t.First());
            });
        }
    }

    public class MergedRelationType : ObjectGraphType<MergedRelation>
    {
        public MergedRelationType(IDataLoaderService dataLoaderService, ILayerDataModel layerDataModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Relation, type: typeof(RelationType));

            Field<ListGraphType<LayerDataType>>("layerStack",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);

                return dataLoaderService.SetupAndLoadAllLayers(layerDataModel, timeThreshold, userContext.Transaction)
                    .Then(layers => layers
                        .Where(kv => layerstackIDs.Contains(kv.Key))
                        .OrderBy(kv => layerstackIDs.IndexOf(kv.Key))
                        .Select(kv => kv.Value)
                    );
            });
        }
    }
}
