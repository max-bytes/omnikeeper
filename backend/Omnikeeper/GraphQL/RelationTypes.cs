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
        private readonly IAttributeModel attributeModel;
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
                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader($"GetMergedCINames_{layerset}_{timeThreshold}",
                        (IEnumerable<ICIIDSelection> ciidSelections) => FetchCINames(layerset, timeThreshold, ciidSelections, userContext.Transaction));
                    return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.FromCIID));
                });
            Field<StringGraphType>("toCIName",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader($"GetMergedCINames_{layerset}_{timeThreshold}", 
                        (IEnumerable<ICIIDSelection> ciidSelections) => FetchCINames(layerset, timeThreshold, ciidSelections, userContext.Transaction));
                    return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.ToCIID));
                });
            Field<MergedCIType>("toCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);
                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedCIs_{layerSet}_{timeThreshold}", 
                    (IEnumerable<ICIIDSelection> ciidSelections) => FetchMergedCIs(ciidSelections, layerSet, timeThreshold, userContext.Transaction));
                return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.ToCIID)).Then(t => t.First());
            });
            Field<MergedCIType>("fromCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var layerSet = userContext.GetLayerSet(context.Path);
                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader($"GetMergedCIs_{layerSet}_{timeThreshold}",
                    (IEnumerable<ICIIDSelection> ciidSelections) => FetchMergedCIs(ciidSelections, layerSet, timeThreshold, userContext.Transaction));
                return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.FromCIID)).Then(t => t.First());
            });
            this.attributeModel = attributeModel;
            this.ciidModel = ciidModel;
            this.ciModel = ciModel;
        }

        private async Task<IDictionary<ICIIDSelection, string?>> FetchCINames(LayerSet layerSet, TimeThreshold timeThreshold, IEnumerable<ICIIDSelection> ciidSelections, IModelContext trans)
        {
            var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

            var combinedNames = await attributeModel.GetMergedCINames(combinedCIIDSelection, layerSet, trans, timeThreshold);

            var ret = new Dictionary<ICIIDSelection, string?>(ciidSelections.Count());
            foreach(var ciidSelection in ciidSelections)
            {
                var ciids = await ciidSelection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
                var selectedNames = ciids.Where(combinedNames.ContainsKey).Select(ciid => combinedNames[ciid]);
                ret.Add(ciidSelection, selectedNames.FirstOrDefault());
            }
            return ret;
        }

        private async Task<ILookup<ICIIDSelection, MergedCI>> FetchMergedCIs(IEnumerable<ICIIDSelection> ciidSelections, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans)
        {
            var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

            // TODO: implement attribute selection possibilities?
            var combinedCIs = (await ciModel.GetMergedCIs(combinedCIIDSelection, layerSet, true, AllAttributeSelection.Instance, trans, timeThreshold)).ToDictionary(ci => ci.ID);

            var ret = new List<(ICIIDSelection, MergedCI)>(); // NOTE: seems weird, cant lookup be created better?
            foreach (var ciidSelection in ciidSelections)
            {
                var ciids = await ciidSelection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(trans));
                var selectedCIs = ciids.Where(combinedCIs.ContainsKey).Select(ciid => combinedCIs[ciid]);
                ret.AddRange(selectedCIs.Select(ci => (ciidSelection, ci)));
            }
            return ret.ToLookup(t => t.Item1, t => t.Item2);
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
                var loader = dataLoaderContextAccessor.Context.GetOrAddLoader($"GetAllLayers_{timeThreshold}", () => layerModel.GetLayers(userContext.Transaction, timeThreshold));
                return loader.LoadAsync().Then(layers => layers
                        .Where(l => layerstackIDs.Contains(l.ID))
                        .OrderBy(l => layerstackIDs.IndexOf(l.ID))
                    );
            });
        }
    }
}
