﻿using GraphQL.Types;
using GraphQL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System;
using System.Linq;

namespace Omnikeeper.GraphQL
{
    public class RelationType : ObjectGraphType<Relation>
    {
        public RelationType()
        {
            Field("id", x => x.ID);
            Field(x => x.FromCIID);
            Field(x => x.ToCIID);
            Field(x => x.PredicateID);
            Field(x => x.State, type: typeof(RelationStateType));
            Field(x => x.ChangesetID);

            FieldAsync<StringGraphType>("fromCIName",
                resolve: async (context) =>
                {
                    var ciModel = context.RequestServices!.GetRequiredService<ICIModel>();
                    var fromCIID = context.Source!.FromCIID;
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.LayerSet;
                    if (layerset == null)
                        throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

                    // TODO: find better way to get CI's name
                    var compactCIs = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(fromCIID), layerset, userContext.Transaction, userContext.TimeThreshold);
                    if (compactCIs.Count() != 1)
                        throw new Exception("TODO");
                    return compactCIs.First().Name;
                });
            FieldAsync<StringGraphType>("toCIName",
                resolve: async (context) =>
                {
                    var ciModel = context.RequestServices!.GetRequiredService<ICIModel>();
                    var toCIID = context.Source!.ToCIID;
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.LayerSet;
                    if (layerset == null)
                        throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

                    // TODO: find better way to get CI's name
                    var compactCIs = await ciModel.GetCompactCIs(SpecificCIIDsSelection.Build(toCIID), layerset, userContext.Transaction, userContext.TimeThreshold);
                    if (compactCIs.Count() != 1)
                        throw new Exception("TODO");
                    return compactCIs.First().Name;
                });
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
