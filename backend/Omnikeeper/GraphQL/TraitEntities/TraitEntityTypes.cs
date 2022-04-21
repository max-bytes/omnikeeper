using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Resolvers;
using GraphQL.Types;
using Microsoft.Extensions.Logging;
using Omnikeeper.Base.AttributeValues;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.GraphQL.TraitEntities
{

    public class TraitEntities { }

    public class TraitEntitiesType : ObjectGraphType<TraitEntities>
    {
        public TraitEntitiesType()
        {
            // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement
            // when there are no traits
            Field<StringGraphType>("placeholder", resolve: ctx => "placeholder");
        }
    }


    public class TraitEntityRootType : ObjectGraphType
    {
        public TraitEntityRootType(ITrait at, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, ICIIDModel ciidModel, IAttributeModel attributeModel, IRelationModel relationModel,
            IDataLoaderService dataLoaderService,
            ElementWrapperType wrapperElementGraphType, FilterInputType? filterGraphType, IDInputType? idGraphType)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityRootGraphTypeName(at);

            var traitEntityModel = new TraitEntityModel(at, effectiveTraitModel, ciModel, attributeModel, relationModel);

            this.FieldAsync("all", new ListGraphType(wrapperElementGraphType), resolve: async context =>
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.GetLayerSet(context.Path);
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var trans = userContext.Transaction;

                // TODO: use dataloader
                var ets = await traitEntityModel.GetByCIID(new AllCIIDsSelection(), layerset, trans, timeThreshold);
                return ets.Select(kv => kv.Value);
            });

            if (filterGraphType != null)
            {
                this.Field("filtered", new ListGraphType(wrapperElementGraphType),
                    arguments: new QueryArguments(
                        new QueryArgument(filterGraphType) { Name = "filter" }
                    ),
                    resolve: context =>
                    {
                        var userContext = (context.UserContext as OmnikeeperUserContext)!;
                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        // use filter to reduce list of potential cis
                        var filter = context.GetArgument<FilterInput>("filter");

                        IDataLoaderResult<ISet<Guid>> matchingCIIDs;
                        if (!filter.RelationFilters.IsEmpty() && !filter.AttributeFilters.IsEmpty())
                        {
                            matchingCIIDs = TraitEntityHelper.GetMatchingCIIDsByRelationFilters(relationModel, ciidModel, filter.RelationFilters, layerset, trans, timeThreshold, dataLoaderService)
                            .Then(matchingCIIDs => TraitEntityHelper.GetMatchingCIIDsByAttributeFilters(SpecificCIIDsSelection.Build(matchingCIIDs), attributeModel, filter.AttributeFilters, layerset, trans, timeThreshold, dataLoaderService))
                            .ResolveNestedResults(); // resolve one level to be correct type again

                        }
                        else if (!filter.AttributeFilters.IsEmpty() && filter.RelationFilters.IsEmpty())
                        {
                            matchingCIIDs = TraitEntityHelper.GetMatchingCIIDsByAttributeFilters(new AllCIIDsSelection(), attributeModel, filter.AttributeFilters, layerset, trans, timeThreshold, dataLoaderService);
                        }
                        else if (filter.AttributeFilters.IsEmpty() && !filter.RelationFilters.IsEmpty())
                        {
                            matchingCIIDs = TraitEntityHelper.GetMatchingCIIDsByRelationFilters(relationModel, ciidModel, filter.RelationFilters, layerset, trans, timeThreshold, dataLoaderService);
                        }
                        else
                        {
                            throw new Exception("At least one filter must be set");
                        }

                        return matchingCIIDs.Then(async matchingCIIDs =>
                        {
                            // TODO: use dataloader
                            var ets = await traitEntityModel.GetByCIID(SpecificCIIDsSelection.Build(matchingCIIDs), layerset, trans, timeThreshold);

                            return ets.Select(kv => kv.Value);
                        });
                    });
            }

            this.FieldAsync("byCIID", wrapperElementGraphType,
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" }
                ),
                resolve: async context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;
                    var ciid = context.GetArgument<Guid>("ciid");

                    // TODO: use dataloader
                    var et = await traitEntityModel.GetSingleByCIID(ciid, layerset, trans, timeThreshold);
                    return et;
                });

            if (idGraphType != null)
            {
                this.FieldAsync("byDataID", wrapperElementGraphType,
                    arguments: new QueryArguments(
                        new QueryArgument(new NonNullGraphType(idGraphType)) { Name = "id" }
                    ),
                    resolve: async context =>
                    {
                        var userContext = (context.UserContext as OmnikeeperUserContext)!;
                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        var id = context.GetArgument<IDInput>("id");

                        // TODO: use data loader
                        var foundCIID = await TraitEntityHelper.GetMatchingCIIDByAttributeValues(attributeModel, id.AttributeValues, layerset, trans, timeThreshold);

                        if (!foundCIID.HasValue)
                        {
                            return null;
                        }

                        // TODO: use data loader
                        return await traitEntityModel.GetSingleByCIID(foundCIID.Value, layerset, trans, timeThreshold);
                    });
            }
        }
    }

    public class ElementWrapperType : ObjectGraphType<EffectiveTrait>
    {
        public readonly ITrait UnderlyingTrait;

        public ElementWrapperType(ITrait underlyingTrait, ElementType elementGraphType, ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService,
            ICIModel ciModel, IChangesetModel changesetModel, IAttributeModel attributeModel)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityWrapperGraphTypeName(underlyingTrait);

            this.Field<GuidGraphType>("ciid", resolve: context =>
            {
                var et = context.Source;
                return et?.CIID;
            });
            this.FieldAsync<MergedCIType>("ci", resolve: async context =>
            {
                var et = context.Source;

                if (et == null)
                    return null;

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.GetLayerSet(context.Path);
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var trans = userContext.Transaction;

                IAttributeSelection forwardAS = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, trans, timeThreshold);

                var finalCI = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(et.CIID), forwardAS, ciModel, attributeModel, layerset, timeThreshold, trans)
                    .Then(cis =>
                    {
                        // NOTE: we kind of know that the CI must exist, we return an empty MergedCI object if the CI query returns null
                        return cis.FirstOrDefault() ?? new MergedCI(et.CIID, null, layerset, timeThreshold, ImmutableDictionary<string, MergedCIAttribute>.Empty);
                    });

                return finalCI;
            });
            this.Field("entity", elementGraphType, resolve: context =>
            {
                var et = context.Source;
                return et;
            });
            this.Field<ChangesetType>("latestChange", resolve: (context) =>
            {
                var et = context.Source!;

                if (et == null)
                    return null;

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var trans = userContext.Transaction;

                var relevantChangesets = et.GetRelevantChangesetIDs();

                return dataLoaderService.SetupAndLoadChangesets(relevantChangesets, changesetModel, trans)
                .Then(changesets =>
                {
                    return changesets.Aggregate((a, b) => a.Timestamp > b.Timestamp ? a : b);
                });
            });


            this.UnderlyingTrait = underlyingTrait;
        }
    }

    public class ElementType : ObjectGraphType<EffectiveTrait>
    {
        public ElementType() { }

        public delegate bool ElementWrapperTypeLookup(string key, [MaybeNullWhen(false)] out ElementWrapperType ew);
        public void Init(ITrait underlyingTrait, RelatedCIType relatedCIType, ElementWrapperTypeLookup elementWrapperTypeLookup, IDataLoaderService dataLoaderService,
            IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider, ICIModel ciModel, IAttributeModel attributeModel, ILogger logger)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityGraphTypeName(underlyingTrait);

            void Add(TraitAttribute ta, bool isRequired)
            {
                var graphType = AttributeValueHelper.AttributeValueType2GraphQLType(ta.AttributeTemplate.Type.GetValueOrDefault(AttributeValueType.Text), ta.AttributeTemplate.IsArray.GetValueOrDefault(false));

                var attributeFieldName = TraitEntityTypesNameGenerator.GenerateTraitAttributeFieldName(ta);
                var resolvedType = (isRequired) ? new NonNullGraphType(graphType) : graphType;
                AddField(new FieldType()
                {
                    Name = attributeFieldName,
                    ResolvedType = resolvedType,
                    Resolver = new FuncFieldResolver<object>(ctx =>
                    {
                        if (ctx.Source is not EffectiveTrait o)
                        {
                            return null;
                        }

                        var fn = ctx.FieldDefinition.Name;
                        if (o.TraitAttributes.TryGetValue(fn, out var v))
                        {
                            return v.Attribute.Value.ToGraphQLValue();
                        }
                        else return null;
                    })
                });
            }

            foreach (var ta in underlyingTrait.RequiredAttributes)
            {
                Add(ta, true);
            }
            foreach (var ta in underlyingTrait.OptionalAttributes)
            {
                Add(ta, false);
            }

            foreach (var r in underlyingTrait.OptionalRelations)
            {
                var directionForward = r.RelationTemplate.DirectionForward;
                var traitHints = r.RelationTemplate.TraitHints;

                foreach (var traitIDHint in traitHints)
                {
                    if (elementWrapperTypeLookup(traitIDHint, out var elementWrapperType))
                    {
                        AddField(new FieldType()
                        {
                            Name = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldWithTraitHintName(r, traitIDHint),
                            ResolvedType = new ListGraphType(elementWrapperType),
                            Resolver = new FuncFieldResolver<object>(async context =>
                            {
                                if (context.Source is not EffectiveTrait o)
                                {
                                    return ImmutableList<EffectiveTrait>.Empty;
                                }
                                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                                var layerSet = userContext.GetLayerSet(context.Path);
                                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                                var trans = userContext.Transaction;

                                var trs = (directionForward) ? o.OutgoingTraitRelations : o.IncomingTraitRelations;
                                if (trs.TryGetValue(r.Identifier, out var tr))
                                {
                                    var otherCIIDs = (directionForward ? tr.Select(r => r.Relation.ToCIID) : tr.Select(r => r.Relation.FromCIID)).ToHashSet();

                                    var trait = await traitsProvider.GetActiveTrait(traitIDHint, trans, timeThreshold);
                                    if (trait == null)
                                        return ImmutableList<EffectiveTrait>.Empty;

                                    var attributeSelection = NamedAttributesSelection.Build(
                                        trait.RequiredAttributes.Select(ra => ra.AttributeTemplate.Name).Union(
                                        trait.OptionalAttributes.Select(oa => oa.AttributeTemplate.Name)).ToHashSet()
                                    );

                                    return dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(otherCIIDs), attributeSelection, ciModel, attributeModel, layerSet, timeThreshold, trans)
                                        .Then(cis =>
                                        {
                                            var ret = new List<IDataLoaderResult<EffectiveTrait?>>();
                                            foreach (var ci in cis)
                                            {
                                                var et = dataLoaderService.SetupAndLoadEffectiveTraits(ci, NamedTraitsSelection.Build(traitIDHint), effectiveTraitModel, traitsProvider, layerSet, timeThreshold, trans)
                                                    .Then(ets => ets.FirstOrDefault());
                                                ret.Add(et);
                                            }
                                            return ret.ToResultOfList().Then(items => items.Where(item => item != null));
                                        });

                                }
                                else return ImmutableList<EffectiveTrait>.Empty;
                            })
                        });
                    }
                    else
                    {
                        logger.LogError($"Could not create trait relation fields for trait-hint: could not find trait with ID \"{traitIDHint}\"");
                    }
                }

                // non-trait hinted field
                AddField(new FieldType()
                {
                    Name = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldName(r),
                    ResolvedType = new ListGraphType(relatedCIType),
                    Resolver = new FuncFieldResolver<object>(ctx =>
                    {
                        if (ctx.Source is not EffectiveTrait o)
                        {
                            return ImmutableList<(MergedRelation relation, bool outgoing)>.Empty;
                        }

                        var trs = (directionForward) ? o.OutgoingTraitRelations : o.IncomingTraitRelations;
                        if (trs.TryGetValue(r.Identifier, out var tr))
                        {
                            return tr.Select(r => (r, directionForward));
                        }
                        else return ImmutableList<(MergedRelation relation, bool outgoing)>.Empty;
                    })
                });
            }
        }
    }

    public class RelatedCIType : ObjectGraphType<(MergedRelation relation, bool outgoing)>
    {
        public RelatedCIType(ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService, ICIModel ciModel, IAttributeModel attributeModel)
        {
            Name = "RelatedCIType";

            Field<GuidGraphType>("relatedCIID", resolve: context =>
            {
                var (relation, outgoing) = context.Source;
                if (outgoing)
                    return relation?.Relation.ToCIID;
                else
                    return relation?.Relation.FromCIID;
            });
            Field<MergedRelationType>("relation", resolve: context => context.Source.relation);
            this.FieldAsync<MergedCIType>("relatedCI", resolve: async context =>
            {
                var (relation, outgoing) = context.Source;

                var otherCIID = (outgoing) ? relation.Relation.ToCIID : relation.Relation.FromCIID;

                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var layerset = userContext.GetLayerSet(context.Path);
                var timeThreshold = userContext.GetTimeThreshold(context.Path);
                var trans = userContext.Transaction;

                IAttributeSelection forwardAS = await MergedCIType.ForwardInspectRequiredAttributes(context, traitsProvider, trans, timeThreshold);

                var finalCI = dataLoaderService.SetupAndLoadMergedCIs(SpecificCIIDsSelection.Build(otherCIID), forwardAS, ciModel, attributeModel, layerset, timeThreshold, trans)
                    .Then(cis =>
                    {
                        // NOTE: we kind of know that the CI must exist, we return an empty MergedCI object if the CI query returns null
                        return cis.FirstOrDefault() ?? new MergedCI(otherCIID, null, layerset, timeThreshold, ImmutableDictionary<string, MergedCIAttribute>.Empty);
                    });

                return finalCI;
            });
        }
    }


    public class MergedCI2TraitEntityWrapper : ObjectGraphType
    {
        public static readonly string StaticName = "MergedCI2TraitEntityWrapper";

        public MergedCI2TraitEntityWrapper(IEnumerable<ElementTypesContainer> typesContainers, IDataLoaderService dataLoaderService, IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider)
        {
            this.Name = StaticName;

            // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement
            // when there are no types
            if (typesContainers.IsEmpty())
            {
                Field<StringGraphType>("placeholder", resolve: ctx => "placeholder");
            }

            foreach (var typeContainer in typesContainers)
            {
                var traitID = typeContainer.Trait.ID;
                var fieldName = TraitEntityTypesNameGenerator.GenerateTraitIDFieldName(traitID);
                this.Field(fieldName, typeContainer.ElementWrapper, resolve: context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;

                    if (context.Parent?.Source is not MergedCI ci)
                        throw new Exception("Could not get MergedCI from context... implementation bug?");

                    // NOTE: we assume here that the ci has all relevant attributes loaded for properly checking for effective trait/trait entity
                    // this is ensured through ForwardInspectRequiredAttributes()

                    var ret = dataLoaderService.SetupAndLoadEffectiveTraits(ci, NamedTraitsSelection.Build(traitID), effectiveTraitModel, traitsProvider, userContext.GetLayerSet(context.Path), userContext.GetTimeThreshold(context.Path), userContext.Transaction)
                        .Then(ets =>
                        {
                            var et = ets.FirstOrDefault();
                            return et;
                        });

                    return ret;
                });
            }
        }
    }
}
