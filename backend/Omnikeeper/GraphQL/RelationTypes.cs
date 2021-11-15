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
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layers = context.GetArgument<string[]>("layers", null);
                    var layerset = (layers == null) ? userContext.LayerSet : new LayerSet(layers);
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    var timeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest(); // NOTE: this is - unfortunately - a problem TODO: describe that we cannot use usercontext.timethreshold
                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader($"GetMergedCINames_{layerset}_{timeThreshold}",
                        (IEnumerable<ICIIDSelection> ciidSelections) => FetchCINames(layerset, timeThreshold, ciidSelections, userContext.Transaction));
                    return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.FromCIID));
                });
            Field<StringGraphType>("toCIName",
                arguments: new QueryArguments(
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "layers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "timeThreshold" }),
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layers = context.GetArgument<string[]>("layers", null);
                    var layerset = (layers == null) ? userContext.LayerSet : new LayerSet(layers);
                    var ts = context.GetArgument<DateTimeOffset?>("timeThreshold", null);
                    var timeThreshold = (ts.HasValue) ? TimeThreshold.BuildAtTime(ts.Value) : TimeThreshold.BuildLatest(); // NOTE: this is - unfortunately - a problem TODO: describe
                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader($"GetMergedCINames_{layerset}_{timeThreshold}", 
                        (IEnumerable<ICIIDSelection> ciidSelections) => FetchCINames(layerset, timeThreshold, ciidSelections, userContext.Transaction));
                    return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.ToCIID));
                });
            Field<MergedCIType>("toCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader("GetMergedCIs", 
                    (IEnumerable<ICIIDSelection> ciidSelections) => FetchMergedCIs(userContext, ciidSelections));
                return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.ToCIID)).Then(t => t.First());
            });
            Field<MergedCIType>("fromCI",
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader("GetMergedCIs",
                    (IEnumerable<ICIIDSelection> ciidSelections) => FetchMergedCIs(userContext, ciidSelections));
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

        private async Task<ILookup<ICIIDSelection, MergedCI>> FetchMergedCIs(OmnikeeperUserContext userContext, IEnumerable<ICIIDSelection> ciidSelections)
        {
            var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

            // TODO: implement attribute selection possibilities?
            var combinedCIs = (await ciModel.GetMergedCIs(combinedCIIDSelection, userContext.LayerSet, true, AllAttributeSelection.Instance, userContext.Transaction, userContext.TimeThreshold)).ToDictionary(ci => ci.ID);

            var ret = new List<(ICIIDSelection, MergedCI)>(); // NOTE: seems weird, cant lookup be created better?
            foreach (var ciidSelection in ciidSelections)
            {
                var ciids = await ciidSelection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(userContext.Transaction));
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

                var loader = dataLoaderContextAccessor.Context.GetOrAddLoader("GetAllLayers", () => layerModel.GetLayers(userContext.Transaction, userContext.TimeThreshold));
                return loader.LoadAsync().Then(layers => layers
                        .Where(l => layerstackIDs.Contains(l.ID))
                        .OrderBy(l => layerstackIDs.IndexOf(l.ID))
                    );
            });
        }
    }
}
