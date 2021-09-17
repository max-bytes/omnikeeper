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

        public RelationType(IDataLoaderContextAccessor dataLoaderContextAccessor, IAttributeModel attributeModel)
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);

            Field<StringGraphType>("fromCIName",
                resolve: (context) =>
                {
                    var ciid = context.Source!.FromCIID;
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader("GetMergedCINames", (IEnumerable<SpecificCIIDsSelection> ciidSelections) => Fetch(userContext, ciidSelections));
                    return loader.LoadAsync((SpecificCIIDsSelection)SpecificCIIDsSelection.Build(ciid));
                });
            Field<StringGraphType>("toCIName",
                resolve: (context) =>
                {
                    var ciid = context.Source!.ToCIID;
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    var loader = dataLoaderContextAccessor.Context.GetOrAddBatchLoader("GetMergedCINames", (IEnumerable<SpecificCIIDsSelection> ciidSelections) => Fetch(userContext, ciidSelections));
                    return loader.LoadAsync((SpecificCIIDsSelection)SpecificCIIDsSelection.Build(ciid));
                });
            this.attributeModel = attributeModel;
        }

        private async Task<IDictionary<SpecificCIIDsSelection, string?>> Fetch(OmnikeeperUserContext userContext, IEnumerable<SpecificCIIDsSelection> ciidSelections)
        {
            var layerset = userContext.LayerSet;
            if (layerset == null)
                throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

            var combinedCIIDSelection = SpecificCIIDsSelection.Build(ciidSelections.SelectMany(ciidSelection => ciidSelection.CIIDs).ToHashSet());

            var combinedNames = await attributeModel.GetMergedCINames(combinedCIIDSelection, layerset, userContext.Transaction, userContext.TimeThreshold);

            var ret = new Dictionary<SpecificCIIDsSelection, string?>(ciidSelections.Count());
            foreach(var ciidSelection in ciidSelections)
            {
                var ciids = ciidSelection.CIIDs;
                var selectedNames = ciids.Where(combinedNames.ContainsKey).Select(ciid => combinedNames[ciid]);
                ret.Add(ciidSelection, selectedNames.FirstOrDefault());
            }
            return ret;
        }
    }

    public class RelationStateType : EnumerationGraphType<RelationState>
    {
    }

    public class MergedRelationType : ObjectGraphType<MergedRelation>
    {
        public MergedRelationType()
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.LayerID);
            Field(x => x.Relation, type: typeof(RelationType));

            FieldAsync<ListGraphType<LayerType>>("layerStack",
            resolve: async (context) =>
            {
                var layerModel = context.RequestServices!.GetRequiredService<ILayerModel>();

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerstackIDs = context.Source!.LayerStackIDs;
                return await layerModel.GetLayers(layerstackIDs, userContext.Transaction);
            });
        }
    }
}
