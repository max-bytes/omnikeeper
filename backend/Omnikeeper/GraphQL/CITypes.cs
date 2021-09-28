using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Entity.DTO;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL
{

    public class MergedCIType : ObjectGraphType<MergedCI>
    {
        private readonly IRelationModel relationModel;

        public MergedCIType(IDataLoaderContextAccessor dataLoaderContextAccessor, IRelationModel relationModel)
        {
            Field("id", x => x.ID);
            Field("name", x => x.CIName, nullable: true);
            Field("layerhash", x => x.Layers.LayerHash);
            Field(x => x.AtTime, type: typeof(TimeThresholdType));
            Field<ListGraphType<MergedCIAttributeType>>("mergedAttributes",
                arguments: new QueryArguments(new QueryArgument<ListGraphType<StringGraphType>> { Name = "attributeNames" }),
                resolve: context => 
                {
                    var mergedAttributes = context.Source!.MergedAttributes.Values;

                    // TODO: only fetch what attributes are requested instead of fetching all, then filtering
                    var attributeNames = context.GetArgument<string[]?>("attributeNames", null)?.ToHashSet();
                    if (attributeNames != null)
                        return mergedAttributes.Where(a => attributeNames.Contains(a.Attribute.Name));
                    return mergedAttributes;
                }
            );

            Field<ListGraphType<MergedRelationType>>("outgoingMergedRelations",
            arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "requiredPredicateID" }),
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var CIIdentity = context.Source!.ID;
                var requiredPredicateID = context.GetArgument<string?>("requiredPredicateID", null);

                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader("GetMergedRelations", (IEnumerable<IRelationSelection> relationSelections) => FetchRelations(userContext, relationSelections));
                return loader.LoadAsync(RelationSelectionFrom.Build(CIIdentity)).Then(ret =>
                { // TODO: move predicateID filtering into fetch and RelationSelection
                    if (requiredPredicateID != null)
                        return ret.Where(r => r.Relation.PredicateID == requiredPredicateID);
                    else 
                        return ret;
                });
            });
            Field<ListGraphType<MergedRelationType>>("incomingMergedRelations",
            arguments: new QueryArguments(new QueryArgument<StringGraphType> { Name = "requiredPredicateID" }),
            resolve: (context) =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var CIIdentity = context.Source!.ID;
                var requiredPredicateID = context.GetArgument<string?>("requiredPredicateID", null);

                var loader = dataLoaderContextAccessor.Context.GetOrAddCollectionBatchLoader("GetMergedRelations", (IEnumerable<IRelationSelection> relationSelections) => FetchRelations(userContext, relationSelections));
                return loader.LoadAsync(RelationSelectionTo.Build(CIIdentity)).Then(ret =>
                { // TODO: move predicateID filtering into fetch and RelationSelection
                    if (requiredPredicateID != null)
                        return ret.Where(r => r.Relation.PredicateID == requiredPredicateID);
                    else
                        return ret;
                });
            });

            FieldAsync<ListGraphType<EffectiveTraitType>>("effectiveTraits",
            resolve: async (context) =>
            {
                var traitModel = context.RequestServices!.GetRequiredService<IEffectiveTraitModel>();
                var traitsProvider = context.RequestServices!.GetRequiredService<ITraitsProvider>();

                var userContext = (context.UserContext as OmnikeeperUserContext)!;

                var traits = (await traitsProvider.GetActiveTraits(userContext.Transaction, userContext.TimeThreshold)).Values;

                var et = await traitModel.GetEffectiveTraitsForCI(traits, context.Source!, userContext.LayerSet!, userContext.Transaction, userContext.TimeThreshold);
                return et;
            });
            this.relationModel = relationModel;
        }

        private async Task<ILookup<IRelationSelection, MergedRelation>> FetchRelations(OmnikeeperUserContext userContext, IEnumerable<IRelationSelection> relationSelections)
        {
            var layerset = userContext.LayerSet;
            if (layerset == null)
                throw new Exception("Got to this resolver without getting any layer informations set... fix this bug!");

            var combinedRelationsTo = new HashSet<Guid>();
            var combinedRelationsFrom = new HashSet<Guid>();
            foreach (var rs in relationSelections)
            {
                switch (rs)
                {
                    case RelationSelectionTo t:
                        combinedRelationsTo.UnionWith(t.ToCIIDs);
                        break;
                    case RelationSelectionFrom f:
                        combinedRelationsFrom.UnionWith(f.FromCIIDs);
                        break;
                    default:
                        throw new NotSupportedException("Not supported (yet)");
                }
            }

            var relationsTo = await relationModel.GetMergedRelations(RelationSelectionTo.Build(combinedRelationsTo), layerset, userContext.Transaction, userContext.TimeThreshold);
            var relationsFrom = await relationModel.GetMergedRelations(RelationSelectionFrom.Build(combinedRelationsFrom), layerset, userContext.Transaction, userContext.TimeThreshold);

            var relationsToMap = relationsTo.ToLookup(t => t.Relation.ToCIID);
            var relationsFromMap = relationsFrom.ToLookup(t => t.Relation.FromCIID);

            var ret = new List<(IRelationSelection, MergedRelation)>();
            foreach (var rs in relationSelections)
            {
                switch (rs)
                {
                    case RelationSelectionTo t:
                        foreach (var ciid in t.ToCIIDs) ret.AddRange(relationsToMap[ciid].Select(t => (rs, t)));
                        break;
                    case RelationSelectionFrom f:
                        foreach (var ciid in f.FromCIIDs) ret.AddRange(relationsFromMap[ciid].Select(t => (rs, t)));
                        break;
                    default:
                        throw new NotSupportedException("Not supported (yet)");
                }
            }
            return ret.ToLookup(t => t.Item1, t => t.Item2);
        }
    }


    public class CompactCIType : ObjectGraphType<CompactCI>
    {
        public CompactCIType()
        {
            Field("id", x => x.ID);
            Field("name", x => x.Name, nullable: true);
            Field(x => x.AtTime, type: typeof(TimeThresholdType));
            Field("layerhash", x => x.LayerHash);
        }
    }

    public class MergedCIAttributeType : ObjectGraphType<MergedCIAttribute>
    {
        public MergedCIAttributeType(IDataLoaderContextAccessor dataLoaderContextAccessor, ILayerModel layerModel)
        {
            Field(x => x.LayerStackIDs);
            Field(x => x.Attribute, type: typeof(CIAttributeType));

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

    public class CIAttributeType : ObjectGraphType<CIAttribute>
    {
        public CIAttributeType()
        {
            Field("id", x => x.ID);
            Field("ciid", x => x.CIID);
            Field(x => x.ChangesetID);
            Field(x => x.Name);
            //Field(x => x.State, type: typeof(AttributeStateType));
            Field("value", x => AttributeValueDTO.Build(x.Value), type: typeof(AttributeValueDTOType));
        }
    }


    public class AttributeValueTypeType : EnumerationGraphType<AttributeValueType>
    {
    }

    public class AttributeValueDTOType : ObjectGraphType<AttributeValueDTO>
    {
        public AttributeValueDTOType()
        {
            Field(x => x.Type, type: typeof(AttributeValueTypeType));
            Field("Value", x => x.Values[0]);
            Field(x => x.Values);
            Field(x => x.IsArray);
        }
    }

    public class DataOriginGQL : ObjectGraphType<DataOriginV1>
    {
        public DataOriginGQL()
        {
            Field(x => x.Type, type: typeof(DataOriginTypeGQL));
        }
    }

    public class DataOriginTypeGQL : EnumerationGraphType<DataOriginType>
    {
    }

    public class TimeThresholdType : ObjectGraphType<TimeThreshold>
    {
        public TimeThresholdType()
        {
            Field(x => x.Time);
            Field(x => x.IsLatest);
        }
    }
}