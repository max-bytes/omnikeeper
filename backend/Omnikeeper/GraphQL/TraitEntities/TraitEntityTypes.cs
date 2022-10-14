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
using System.Threading.Tasks;

namespace Omnikeeper.GraphQL.TraitEntities
{

    public class TraitEntities { }

    public class TraitEntitiesType : ObjectGraphType<TraitEntities>
    {
        public TraitEntitiesType()
        {
            // NOTE: because graphql types MUST define at least one field, we define a placeholder field whose single purpose is to simply exist and fulfill the requirement
            // when there are no traits
            Field<StringGraphType>("placeholder").Resolve(ctx => "placeholder");
        }
    }


    public class TraitEntityRootType : ObjectGraphType
    {
        public TraitEntityRootType(ITrait at, IEffectiveTraitModel effectiveTraitModel, ICIModel ciModel, IAttributeModel attributeModel, IRelationModel relationModel,
            IChangesetModel changesetModel, IDataLoaderService dataLoaderService, ITraitsProvider traitsProvider,
            ElementWrapperType wrapperElementGraphType, FilterInputType filterGraphType, IDInputType? idGraphType)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityRootGraphTypeName(at);

            var traitEntityModel = new TraitEntityModel(at, effectiveTraitModel, ciModel, attributeModel, relationModel, changesetModel);

            Field("all", new ListGraphType(wrapperElementGraphType))
                .ResolveAsync(async context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;

                    // TODO: use dataloader
                    var ets = await traitEntityModel.GetByCIID(AllCIIDsSelection.Instance, layerset, trans, timeThreshold);
                    return ets.Select(kv => kv.Value);
                });

            Field("filtered", new ListGraphType(wrapperElementGraphType))
                .Arguments(
                    new QueryArgument(new NonNullGraphType(filterGraphType)) { Name = "filter" }
                )
                .Resolve(context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;

                    // use filter to reduce list of potential cis
                    var filter = context.GetArgument<FilterInput>("filter");

                    var matchingCIIDs = filter.Apply(AllCIIDsSelection.Instance, attributeModel, relationModel, ciModel, effectiveTraitModel, dataLoaderService, traitsProvider, layerset, trans, timeThreshold);

                    return matchingCIIDs.Then(async matchingCIIDs =>
                    {
                        // TODO: use dataloader
                        var ets = await traitEntityModel.GetByCIID(matchingCIIDs, layerset, trans, timeThreshold);

                        return ets.Select(kv => kv.Value);
                    });
                });
            Field("filteredSingle", wrapperElementGraphType)
                .Arguments(
                    new QueryArgument(filterGraphType) { Name = "filter" }
                )
                .Resolve(context =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;

                    // use filter to reduce list of potential cis
                    var filter = context.GetArgument<FilterInput>("filter");

                    var matchingCIIDs = filter.Apply(AllCIIDsSelection.Instance, attributeModel, relationModel, ciModel, effectiveTraitModel, dataLoaderService, traitsProvider, layerset, trans, timeThreshold);

                    return matchingCIIDs.Then(async matchingCIIDs =>
                    {
                        var ciids = await matchingCIIDs.GetCIIDsAsync(async () => await ciModel.GetCIIDs(trans));
                        if (ciids.IsEmpty())
                            return null;

                        // if the foundCIIDs contains any CIs that have the trait, we need to use this, not just the first CIID (which might NOT have the trait)
                        var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(ciids, traitEntityModel, layerset, trans, timeThreshold);
                        return bestMatchingET;
                    });
                });

            Field("byCIID", wrapperElementGraphType)
                .Arguments(
                    new QueryArgument<NonNullGraphType<GuidGraphType>> { Name = "ciid" }
                )
                .ResolveAsync(async context =>
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
                Field("byDataID", wrapperElementGraphType)
                    .Arguments(
                        new QueryArgument(new NonNullGraphType(idGraphType)) { Name = "id" }
                    )
                    .ResolveAsync(async context =>
                    {
                        var userContext = (context.UserContext as OmnikeeperUserContext)!;
                        var layerset = userContext.GetLayerSet(context.Path);
                        var timeThreshold = userContext.GetTimeThreshold(context.Path);
                        var trans = userContext.Transaction;

                        var id = context.GetArgument<IDInput>("id");

                        // TODO: use data loader
                        var idAttributeTuples = id.IDAttributeValues.Select(t => (t.traitAttribute.AttributeTemplate.Name, t.value)).ToArray();
                        var foundCIIDs = await TraitEntityHelper.GetMatchingCIIDsByAttributeValues(attributeModel, idAttributeTuples, layerset, trans, timeThreshold);

                        if (foundCIIDs.IsEmpty())
                            return null;

                        // if the foundCIIDs contains any CIs that have the trait, we need to use this, not just the first CIID (which might NOT have the trait)
                        var (bestMatchingCIID, bestMatchingET) = await TraitEntityHelper.GetSingleBestMatchingCI(foundCIIDs, traitEntityModel, layerset, trans, timeThreshold);
                        return bestMatchingET;
                    })
                    .DeprecationReason("Superseded by filteredSingle*");
            }
        }
    }

    public class ElementWrapperType : ObjectGraphType<EffectiveTrait>
    {
        public readonly ITrait UnderlyingTrait;

        public ElementWrapperType(ITrait underlyingTrait, ElementType elementGraphType, ITraitsProvider traitsProvider, IDataLoaderService dataLoaderService, TraitEntityModel traitEntityModel,
            ICIModel ciModel, IAttributeModel attributeModel)
        {
            Name = TraitEntityTypesNameGenerator.GenerateTraitEntityWrapperGraphTypeName(underlyingTrait);

            Field<GuidGraphType>("ciid")
                .Resolve(context =>
                {
                    var et = context.Source;
                    return et?.CIID;
                });
            Field<MergedCIType>("ci")
                .ResolveAsync(async context =>
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
            Field("entity", elementGraphType)
                .Resolve(context =>
                {
                    var et = context.Source;
                    return et;
                });
            Field<ChangesetType>("latestChange")
                .Resolve(context =>
                {
                    var et = context.Source!;

                    if (et == null)
                        return null;

                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var trans = userContext.Transaction;

                    return dataLoaderService.SetupAndLoadLatestRelevantChangesetPerTraitEntity(SpecificCIIDsSelection.Build(et.CIID), false, false, traitEntityModel, layerset, timeThreshold, trans)
                        .Then(r =>
                        {
                            if (r.TryGetValue(et.CIID, out var res))
                                return res;
                            return null;
                        });
                });

            this.UnderlyingTrait = underlyingTrait;
        }
    }

    public class ElementType : ObjectGraphType<EffectiveTrait>
    {
        public delegate bool ElementTypeContainerLookup(string key, [MaybeNullWhen(false)] out ElementTypesContainer etc);
        public void Init(ITrait underlyingTrait, RelatedCIType relatedCIType, ElementTypeContainerLookup elementTypesContainerLookup, IRelationModel relationModel, 
            IDataLoaderService dataLoaderService, IEffectiveTraitModel effectiveTraitModel, ITraitsProvider traitsProvider, ICIModel ciModel, IAttributeModel attributeModel, ILogger logger)
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
                    if (elementTypesContainerLookup(traitIDHint, out var elementTypesContainer))
                    {
                        AddField(new FieldType()
                        {
                            Name = TraitEntityTypesNameGenerator.GenerateTraitRelationFieldWithTraitHintName(r, traitIDHint),
                            ResolvedType = new ListGraphType(elementTypesContainer.ElementWrapper),
                            Arguments = new QueryArguments(new QueryArgument(elementTypesContainer.FilterInput) { Name = "filter" }),
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

                                var filter = context.GetArgument<FilterInput?>("filter", null);

                                var trs = (directionForward) ? o.OutgoingTraitRelations : o.IncomingTraitRelations;
                                if (trs.TryGetValue(r.Identifier, out var tr))
                                {
                                    var otherCIIDs = (directionForward ? tr.Select(r => r.Relation.ToCIID) : tr.Select(r => r.Relation.FromCIID)).ToHashSet();

                                    IDataLoaderResult<ICIIDSelection> matchingOtherCIIDsDL;
                                    if (filter != null)
                                        matchingOtherCIIDsDL = filter.Apply(SpecificCIIDsSelection.Build(otherCIIDs), attributeModel, relationModel, ciModel, effectiveTraitModel, dataLoaderService, traitsProvider, layerSet, trans, timeThreshold);
                                    else
                                        matchingOtherCIIDsDL = new SimpleDataLoader<ICIIDSelection>(rfc => Task.FromResult(SpecificCIIDsSelection.Build(otherCIIDs)));

                                    var trait = await traitsProvider.GetActiveTrait(traitIDHint, trans, timeThreshold);
                                    if (trait == null)
                                        return ImmutableList<EffectiveTrait>.Empty;

                                    return matchingOtherCIIDsDL.Then(matchingOtherCIIDs =>
                                    {
                                        return dataLoaderService.SetupAndLoadEffectiveTraits(matchingOtherCIIDs, trait, ciModel, attributeModel, effectiveTraitModel, traitsProvider, layerSet, timeThreshold, trans)
                                            .Then(d => d.Values);
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

            Field<GuidGraphType>("relatedCIID")
                .Resolve(context =>
                {
                    var (relation, outgoing) = context.Source;
                    if (outgoing)
                        return relation?.Relation.ToCIID;
                    else
                        return relation?.Relation.FromCIID;
                });
            Field<MergedRelationType>("relation")
                .Resolve(context => context.Source.relation);
            Field<MergedCIType>("relatedCI")
                .ResolveAsync(async context =>
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
                Field<StringGraphType>("placeholder")
                .Resolve(ctx => "placeholder");
            }

            foreach (var typeContainer in typesContainers)
            {
                var traitID = typeContainer.Trait.ID;
                var fieldName = TraitEntityTypesNameGenerator.GenerateTraitIDFieldName(traitID);
                Field(fieldName, typeContainer.ElementWrapper)
                .Resolve(context =>
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
