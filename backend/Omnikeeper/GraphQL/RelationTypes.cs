using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader("GetMergedCINames", 
                        (IEnumerable<ICIIDSelection> ciidSelections) => FetchCINames(userContext, ciidSelections));
                    return loader.LoadAsync(SpecificCIIDsSelection.Build(context.Source!.FromCIID));
                });
            Field<StringGraphType>("toCIName",
                resolve: (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader("GetMergedCINames", 
                        (IEnumerable<ICIIDSelection> ciidSelections) => FetchCINames(userContext, ciidSelections));
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

        private async Task<IDictionary<ICIIDSelection, string?>> FetchCINames(OmnikeeperUserContext userContext, IEnumerable<ICIIDSelection> ciidSelections)
        {
            var layerset = userContext.LayerSet;
            if (layerset == null)
                throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

            var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

            var combinedNames = await attributeModel.GetMergedCINames(combinedCIIDSelection, layerset, userContext.Transaction, userContext.TimeThreshold);

            var ret = new Dictionary<ICIIDSelection, string?>(ciidSelections.Count());
            foreach(var ciidSelection in ciidSelections)
            {
                var ciids = await ciidSelection.GetCIIDsAsync(async () => await ciidModel.GetCIIDs(userContext.Transaction));
                var selectedNames = ciids.Where(combinedNames.ContainsKey).Select(ciid => combinedNames[ciid]);
                ret.Add(ciidSelection, selectedNames.FirstOrDefault());
            }
            return ret;
        }

        private async Task<ILookup<ICIIDSelection, MergedCI>> FetchMergedCIs(OmnikeeperUserContext userContext, IEnumerable<ICIIDSelection> ciidSelections)
        {
            var layerset = userContext.LayerSet;
            if (layerset == null)
                throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

            var combinedCIIDSelection = CIIDSelectionExtensions.UnionAll(ciidSelections);

            // TODO: implement attribute selection possibilities?
            var combinedCIs = (await ciModel.GetMergedCIs(combinedCIIDSelection, layerset, true, AllAttributeSelection.Instance, userContext.Transaction, userContext.TimeThreshold)).ToDictionary(ci => ci.ID);

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

                var loader = dataLoaderContextAccessor.Context.GetOrAddLoader("GetAllLayers", () => layerModel.GetLayers(userContext.Transaction));
                return loader.LoadAsync().Then(layers => layers.Where(l => layerstackIDs.Contains(l.ID)));
            });
        }
    }
}
