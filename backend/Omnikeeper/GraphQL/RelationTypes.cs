using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        private readonly ICIIDModel ciidModel;
        private readonly ICIModel ciModel;

        public RelationType(IDataLoaderContextAccessor dataLoaderContextAccessor, IAttributeModel attributeModel, ICIIDModel ciidModel, ICIModel ciModel)
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
                    return DataLoaderUtils.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), dataLoaderContextAccessor, attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field<StringGraphType>("toCIName",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.ToCIID;
                    return DataLoaderUtils.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), dataLoaderContextAccessor, attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field<MergedCIType>("toCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);
                return DataLoaderUtils.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.ToCIID), dataLoaderContextAccessor, ciModel, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t => t.First());
            });
            Field<MergedCIType>("fromCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);
                return DataLoaderUtils.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(context.Source!.FromCIID), dataLoaderContextAccessor, ciModel, layerSet, timeThreshold, userContext.Transaction)
                    .Then(t => t.First());
            });
            this.ciidModel = ciidModel;
            this.ciModel = ciModel;
        }
    }

    public class MergedRelationType : ObjectGraphType<MergedRelation>
    {
        public MergedRelationType(IDataLoaderContextAccessor dataLoaderContextAccessor, ILayerModel layerModel)
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

                return DataLoaderUtils.SetupAndLoadAllLayers(dataLoaderContextAccessor, layerModel, timeThreshold, userContext.Transaction)
                    .Then(layers => layers
                        .Where(l => layerstackIDs.Contains(l.ID))
                        .OrderBy(l => layerstackIDs.IndexOf(l.ID))
                    );
            });
        }
    }
}
