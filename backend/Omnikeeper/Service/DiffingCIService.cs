using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using Omnikeeper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class DiffingCIService
    {
        public IDictionary<Guid, CIAttributesComparison> DiffCIs(IEnumerable<MergedCI> left, IEnumerable<MergedCI> right, bool showEqual)
        {
            var allCIIDs = new HashSet<Guid>();
            var leftDictionary = left.ToDictionary(ci => ci.ID);
            var rightDictionary = right.ToDictionary(ci => ci.ID);
            allCIIDs.UnionWith(leftDictionary.Keys);
            allCIIDs.UnionWith(rightDictionary.Keys);

            var ciComparisons = new Dictionary<Guid, CIAttributesComparison>();
            foreach (var ciid in allCIIDs)
            {
                if (!leftDictionary.TryGetValue(ciid, out var leftCI))
                {
                    var rightCI = rightDictionary[ciid];
                    var attributes = rightCI.MergedAttributes.Select(a => new AttributeComparison(a.Key, null, a.Value, ComparisonStatus.Unequal));
                    ciComparisons.Add(ciid, new CIAttributesComparison(ciid, null, rightCI, attributes));
                } else if (!rightDictionary.TryGetValue(ciid, out var rightCI))
                {
                    var attributes = leftCI.MergedAttributes.Select(a => new AttributeComparison(a.Key, a.Value, null, ComparisonStatus.Unequal));
                    ciComparisons.Add(ciid, new CIAttributesComparison(ciid, leftCI, null, attributes));
                } else
                {
                    var allNames = new HashSet<string>();
                    allNames.UnionWith(leftCI.MergedAttributes.Keys);
                    allNames.UnionWith(rightCI.MergedAttributes.Keys);
                    var attributes = allNames.Select(name => {
                        if (!leftCI.MergedAttributes.TryGetValue(name, out var leftAttribute))
                        {
                            return new AttributeComparison(name, null, rightCI.MergedAttributes[name], ComparisonStatus.Unequal);
                        } else if (!rightCI.MergedAttributes.TryGetValue(name, out var rightAttribute))
                        {
                            return new AttributeComparison(name, leftCI.MergedAttributes[name], null, ComparisonStatus.Unequal);
                        } else
                        {
                            var status = (leftAttribute.Attribute.Value.Equals(rightAttribute.Attribute.Value)) ? ComparisonStatus.Equal : ComparisonStatus.Unequal;
                            return new AttributeComparison(name, leftAttribute, rightAttribute, status);
                        }
                    }).Where(a => showEqual || a.status != ComparisonStatus.Equal);
                    if (showEqual || !attributes.IsEmpty())
                        ciComparisons.Add(ciid, new CIAttributesComparison(ciid, leftCI, rightCI, attributes));
                }
            }

            return ciComparisons;
        }

        public IEnumerable<CIRelationsComparison> DiffRelations(IEnumerable<MergedRelation> leftRelations, IEnumerable<MergedRelation> rightRelations, bool outgoing, bool showEqual)
        {
            var left = leftRelations.ToDictionary(r => r.Relation.InformationHash);
            var right = rightRelations.ToDictionary(r => r.Relation.InformationHash);

            var allKeys = new HashSet<string>();
            allKeys.UnionWith(left.Keys);
            allKeys.UnionWith(right.Keys);
            var dict = new Dictionary<Guid, IList<RelationComparison>>();
            foreach (var key in allKeys)
            {
                if (!left.TryGetValue(key, out var leftRelation))
                {
                    var rightRelation = right[key];
                    var rc = new RelationComparison(rightRelation.Relation.PredicateID, rightRelation.Relation.FromCIID, rightRelation.Relation.ToCIID, null, rightRelation, ComparisonStatus.Unequal);
                    dict.AddOrUpdate((outgoing) ? rc.fromCIID : rc.toCIID, () => new List<RelationComparison>() { rc }, (l) => { l.Add(rc); return l; });
                    
                }
                else if (!right.TryGetValue(key, out var rightRelation))
                {
                    var rc = new RelationComparison(leftRelation.Relation.PredicateID, leftRelation.Relation.FromCIID, leftRelation.Relation.ToCIID, leftRelation, null, ComparisonStatus.Unequal);
                    dict.AddOrUpdate((outgoing) ? rc.fromCIID : rc.toCIID, () => new List<RelationComparison>() { rc }, (l) => { l.Add(rc); return l; });
                }
                else
                {
                    if (showEqual)
                    {
                        var rc = new RelationComparison(leftRelation.Relation.PredicateID, leftRelation.Relation.FromCIID, leftRelation.Relation.ToCIID, leftRelation, rightRelation, ComparisonStatus.Equal);
                        dict.AddOrUpdate((outgoing) ? rc.fromCIID : rc.toCIID, () => new List<RelationComparison>() { rc }, (l) => { l.Add(rc); return l; });
                    }
                }
            }

            var ret = dict.Select(kv => new CIRelationsComparison(kv.Key, kv.Value));
            return ret;
        }

        public IEnumerable<CIEffectiveTraitsComparison> DiffEffectiveTraits(ISet<(Guid ciid, string traitID)> left, ISet<(Guid ciid, string traitID)> right, bool showEqual)
        {
            var dict = new Dictionary<Guid, IList<EffectiveTraitComparison>>();
            foreach (var l in left)
            {
                if (right.Contains(l))
                {
                    right.Remove(l);
                    if (showEqual)
                    {
                        var et = new EffectiveTraitComparison(l.traitID, true, true);
                        dict.AddOrUpdate(l.ciid, () => new List<EffectiveTraitComparison>() { et }, (l) => { l.Add(et); return l; });
                    }
                } else
                {
                    var et = new EffectiveTraitComparison(l.traitID,true, false);
                    dict.AddOrUpdate(l.ciid, () => new List<EffectiveTraitComparison>() { et }, (l) => { l.Add(et); return l; });
                }
            }
            foreach (var r in right)
            {
                var et = new EffectiveTraitComparison(r.traitID, false, true);
                dict.AddOrUpdate(r.ciid, () => new List<EffectiveTraitComparison>() { et }, (l) => { l.Add(et); return l; });
            }

            return dict.Select(kv => new CIEffectiveTraitsComparison(kv.Key, kv.Value));
        }
    }

    public enum ComparisonStatus
    {
        Equal,
        Unequal,
        Similar
    }

    public class CIAttributesComparison
    {
        public readonly Guid ciid;
        public readonly MergedCI? left;
        public readonly MergedCI? right;

        public readonly IEnumerable<AttributeComparison> attributes;

        public CIAttributesComparison(Guid ciid, MergedCI? left, MergedCI? right, IEnumerable<AttributeComparison> attributes)
        {
            this.ciid = ciid;
            this.left = left;
            this.right = right;
            this.attributes = attributes;
        }
    }

    public class AttributeComparison
    {
        public readonly string name;
        public readonly MergedCIAttribute? left;
        public readonly MergedCIAttribute? right;

        public readonly ComparisonStatus status;

        public AttributeComparison(string name, MergedCIAttribute? left, MergedCIAttribute? right, ComparisonStatus status)
        {
            this.name = name;
            this.left = left;
            this.right = right;
            this.status = status;
        }
    }

    public class CIRelationsComparison
    {
        public readonly Guid ciid;
        public readonly IEnumerable<RelationComparison> relations;

        public CIRelationsComparison(Guid ciid, IEnumerable<RelationComparison> relations)
        {
            this.ciid = ciid;
            this.relations = relations;
        }
    }

    public class RelationComparison
    {
        public readonly string predicateID;
        public readonly Guid fromCIID;
        public readonly Guid toCIID;
        public readonly MergedRelation? left;
        public readonly MergedRelation? right;

        public readonly ComparisonStatus status;

        public RelationComparison(string predicateID, Guid fromCIID, Guid toCIID, MergedRelation? left, MergedRelation? right, ComparisonStatus status)
        {
            this.predicateID = predicateID;
            this.fromCIID = fromCIID;
            this.toCIID = toCIID;
            this.left = left;
            this.right = right;
            this.status = status;
        }
    }

    public class CIEffectiveTraitsComparison
    {
        public readonly Guid ciid;

        public readonly IEnumerable<EffectiveTraitComparison> effectiveTraits;

        public CIEffectiveTraitsComparison(Guid ciid, IEnumerable<EffectiveTraitComparison> effectiveTraits)
        {
            this.ciid = ciid;
            this.effectiveTraits = effectiveTraits;
        }
    }

    public class EffectiveTraitComparison
    {
        public readonly string traitID;
        public readonly bool leftHasTrait;
        public readonly bool rightHasTrait;

        public readonly ComparisonStatus status;

        public EffectiveTraitComparison(string traitID, bool leftHasTrait, bool rightHasTrait)
        {
            this.traitID = traitID;
            this.leftHasTrait = leftHasTrait;
            this.rightHasTrait = rightHasTrait;
            this.status = (leftHasTrait == rightHasTrait) ? ComparisonStatus.Equal : ComparisonStatus.Unequal;
        }
    }

    public class ComparisonStatusType : EnumerationGraphType<ComparisonStatus> { }

    public class AttributeComparisonType : ObjectGraphType<AttributeComparison>
    {
        public AttributeComparisonType()
        {
            Field("name", x => x.name);
            Field("left", x => x.left, type: typeof(MergedCIAttributeType));
            Field("right", x => x.right, type: typeof(MergedCIAttributeType));
            Field("status", x => x.status, type: typeof(ComparisonStatusType));
        }
    }

    public class CIAttributesComparisonType : ObjectGraphType<CIAttributesComparison>
    {
        public CIAttributesComparisonType()
        {
            Field("ciid", x => x.ciid);
            Field("left", x => x.left, type: typeof(MergedCIType));
            Field("right", x => x.right, type: typeof(MergedCIType));
            Field("attributeComparisons", x => x.attributes, type: typeof(ListGraphType<AttributeComparisonType>));
        }
    }

    public class CIRelationsComparisonType : ObjectGraphType<CIRelationsComparison>
    {
        public CIRelationsComparisonType()
        {
            Field("ciid", x => x.ciid);
            Field("relationComparisons", x => x.relations, type: typeof(ListGraphType<RelationComparisonType>));
        }
    }

    public class CIEffectiveTraitsComparisonType : ObjectGraphType<CIEffectiveTraitsComparison>
    {
        public CIEffectiveTraitsComparisonType()
        {
            Field("ciid", x => x.ciid);
            Field("effectiveTraitComparisons", x => x.effectiveTraits, type: typeof(ListGraphType<EffectiveTraitComparisonType>));
        }
    }

    public class RelationComparisonType : ObjectGraphType<RelationComparison>
    {
        public RelationComparisonType()
        {
            Field("predicateID", x => x.predicateID);
            Field("fromCIID", x => x.fromCIID);
            Field("toCIID", x => x.toCIID);
            Field("left", x => x.left, type: typeof(MergedRelationType));
            Field("right", x => x.right, type: typeof(MergedRelationType));
            Field("status", x => x.status, type: typeof(ComparisonStatusType));
        }
    }

    public class EffectiveTraitComparisonType : ObjectGraphType<EffectiveTraitComparison>
    {
        public EffectiveTraitComparisonType()
        {
            Field("traitID", x => x.traitID);
            Field("leftHasTrait", x => x.leftHasTrait);
            Field("rightHasTrait", x => x.rightHasTrait);
            Field("status", x => x.status, type: typeof(ComparisonStatusType));
        }
    }

    public class DiffingResult { }

    public class DiffingResultType : ObjectGraphType<DiffingResult>
    {
        public DiffingResultType(IDataLoaderContextAccessor dataLoaderContextAccessor, DiffingCIService diffingCIService, ILayerModel layerModel, ICIModel ciModel, IRelationModel relationModel, IEffectiveTraitModel effectiveTraitModel,
            ITraitsProvider traitsProvider, ILayerBasedAuthorizationService layerBasedAuthorizationService, ICIBasedAuthorizationService ciBasedAuthorizationService)
        {
            async Task<(LayerSet, LayerSet)> GetLayerArguments(IResolveFieldContext context)
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var leftLayers = await layerModel.BuildLayerSet(context.GetArgument<string[]>($"leftLayers")!, userContext.Transaction);
                var rightLayers = await layerModel.BuildLayerSet(context.GetArgument<string[]>($"rightLayers")!, userContext.Transaction);
                if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, leftLayers))
                    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', leftLayers.LayerIDs)}");
                if (!layerBasedAuthorizationService.CanUserReadFromAllLayers(userContext.User, rightLayers))
                    throw new ExecutionError($"User \"{userContext.User.Username}\" does not have permission to read from at least one of the following layerIDs: {string.Join(',', rightLayers.LayerIDs)}");
                return (leftLayers, rightLayers);
            }

            (TimeThreshold, TimeThreshold) GetTimeThresholdArguments(IResolveFieldContext context)
            {
                var leftTimeThresholdDTO = context.GetArgument<DateTimeOffset?>($"leftTimeThreshold");
                var leftTimeThreshold = (!leftTimeThresholdDTO.HasValue) ? TimeThreshold.BuildLatest() : TimeThreshold.BuildAtTime(leftTimeThresholdDTO.Value);
                var rightTimeThresholdDTO = context.GetArgument<DateTimeOffset?>($"rightTimeThreshold");
                var rightTimeThreshold = (!rightTimeThresholdDTO.HasValue) ? TimeThreshold.BuildLatest() : TimeThreshold.BuildAtTime(rightTimeThresholdDTO.Value);
                return (leftTimeThreshold, rightTimeThreshold);
            }

            (Guid[]?, Guid[]?) GetCIIDArguments(IResolveFieldContext context)
            {
                var leftCIIDs = context.GetArgument<Guid[]?>($"leftCIIDs", null);
                var rightCIIDs = context.GetArgument<Guid[]?>($"rightCIIDs", null);
                return (leftCIIDs, rightCIIDs);
            }

            bool GetShowEqual(IResolveFieldContext context)
            {
                return context.GetArgument<bool?>($"showEqual").GetValueOrDefault(true);
            }

            (IAttributeSelection, IAttributeSelection) GetAttributeSelectionArguments(IResolveFieldContext context)
            {
                var leftAttributes = context.GetArgument<string[]?>($"leftAttributes", null);
                var rightAttributes = context.GetArgument<string[]?>($"rightAttributes", null);
                IAttributeSelection leftAttributeSelection = AllAttributeSelection.Instance;
                if (leftAttributes != null) leftAttributeSelection = NamedAttributesSelection.Build(leftAttributes);
                IAttributeSelection rightAttributeSelection = AllAttributeSelection.Instance;
                if (rightAttributes != null) rightAttributeSelection = NamedAttributesSelection.Build(rightAttributes);
                return (leftAttributeSelection, rightAttributeSelection);
            }

            FieldAsync<ListGraphType<CIAttributesComparisonType>>("cis",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "leftLayers" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "rightLayers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "leftTimeThreshold" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "rightTimeThreshold" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "leftCIIDs" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "rightCIIDs" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "leftAttributes" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "rightAttributes" },
                    new QueryArgument<BooleanGraphType> { Name = "showEqual" }
                    ),
                resolve: async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var (leftLayers, rightLayers) = await GetLayerArguments(context);
                    var (leftTimeThreshold, rightTimeThreshold) = GetTimeThresholdArguments(context);
                    var (leftCIIDs, rightCIIDs) = GetCIIDArguments(context);
                    var (leftAttributes, rightAttributes) = GetAttributeSelectionArguments(context);
                    ICIIDSelection leftCIIDSelection = (leftCIIDs != null) ? SpecificCIIDsSelection.Build(leftCIIDs) : new AllCIIDsSelection();
                    ICIIDSelection rightCIIDSelection = (rightCIIDs != null) ? SpecificCIIDsSelection.Build(rightCIIDs) : new AllCIIDsSelection();
                    var showEqual = GetShowEqual(context);

                    // create sub contexts for left and right branches of graphql query
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "left" }));
                    userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "right" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "left" }));
                    userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "right" }));
                    userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "attributeComparisons", "left" }));
                    userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "attributeComparisons", "right" }));
                    userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "attributeComparisons", "left" }));
                    userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "attributeComparisons", "right" }));

                    // TODO: use dataloader?
                    var leftCIs = await ciModel.GetMergedCIs(leftCIIDSelection, leftLayers, false, leftAttributes, userContext.Transaction, leftTimeThreshold);
                    var rightCIs = await ciModel.GetMergedCIs(rightCIIDSelection, rightLayers, false, rightAttributes, userContext.Transaction, rightTimeThreshold);

                    // ci-based authz
                    leftCIs = ciBasedAuthorizationService.FilterReadableCIs(leftCIs, (ci) => ci.ID);
                    rightCIs = ciBasedAuthorizationService.FilterReadableCIs(rightCIs, (ci) => ci.ID);

                    var comparisons = diffingCIService.DiffCIs(leftCIs, rightCIs, showEqual);
                    return comparisons.Values;
                });

            async Task<IEnumerable<CIRelationsComparison>> ResolveRelationComparisons(IResolveFieldContext context, bool outgoing)
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;
                var (leftLayers, rightLayers) = await GetLayerArguments(context);
                var (leftTimeThreshold, rightTimeThreshold) = GetTimeThresholdArguments(context);
                var (leftCIIDs, rightCIIDs) = GetCIIDArguments(context);
                var showEqual = GetShowEqual(context);

                Func<ISet<Guid>, IRelationSelection> srb = (ciids) => RelationSelectionFrom.Build(ciids);
                if (!outgoing) srb = (ciids) => RelationSelectionTo.Build(ciids);
                IRelationSelection leftRelationSelection = (leftCIIDs != null) ? srb(leftCIIDs.ToHashSet()) : RelationSelectionAll.Instance;
                IRelationSelection rightRelationSelection = (rightCIIDs != null) ? srb(rightCIIDs.ToHashSet()) : RelationSelectionAll.Instance;

                // create sub contexts for left and right branches of graphql query
                userContext.WithTimeThreshold(leftTimeThreshold, context.Path.Concat(new List<object>() { "relationComparisons", "left" }));
                userContext.WithTimeThreshold(rightTimeThreshold, context.Path.Concat(new List<object>() { "relationComparisons", "right" }));
                userContext.WithLayerset(leftLayers, context.Path.Concat(new List<object>() { "relationComparisons", "left" }));
                userContext.WithLayerset(rightLayers, context.Path.Concat(new List<object>() { "relationComparisons", "right" }));

                var leftLoaded = DataLoaderUtils.SetupAndLoadRelation(leftRelationSelection, dataLoaderContextAccessor, relationModel, leftLayers, leftTimeThreshold, userContext.Transaction);
                var leftRelations = await leftLoaded.GetResultAsync();
                var rightLoaded = DataLoaderUtils.SetupAndLoadRelation(rightRelationSelection, dataLoaderContextAccessor, relationModel, rightLayers, rightTimeThreshold, userContext.Transaction);
                var rightRelations = await rightLoaded.GetResultAsync();

                // ci-based authorization
                leftRelations = leftRelations.Where(r => ciBasedAuthorizationService.CanReadCI(r.Relation.FromCIID) && ciBasedAuthorizationService.CanReadCI(r.Relation.ToCIID));
                rightRelations = rightRelations.Where(r => ciBasedAuthorizationService.CanReadCI(r.Relation.FromCIID) && ciBasedAuthorizationService.CanReadCI(r.Relation.ToCIID));

                return diffingCIService.DiffRelations(leftRelations, rightRelations, outgoing, showEqual);
            }

            FieldAsync<ListGraphType<CIRelationsComparisonType>>("outgoingRelations",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "leftLayers" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "rightLayers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "leftTimeThreshold" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "rightTimeThreshold" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "leftCIIDs" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "rightCIIDs" },
                    new QueryArgument<BooleanGraphType> { Name = "showEqual" }
                    ),
                resolve: async (context) =>
                {
                    return await ResolveRelationComparisons(context, true);
                });
            FieldAsync<ListGraphType<CIRelationsComparisonType>>("incomingRelations",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "leftLayers" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "rightLayers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "leftTimeThreshold" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "rightTimeThreshold" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "leftCIIDs" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "rightCIIDs" },
                    new QueryArgument<BooleanGraphType> { Name = "showEqual" }
                    ),
                resolve: async (context) =>
                {
                    return await ResolveRelationComparisons(context, false);
                });

            FieldAsync<ListGraphType<CIEffectiveTraitsComparisonType>>("effectiveTraits",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "leftLayers" },
                    new QueryArgument<NonNullGraphType<ListGraphType<StringGraphType>>> { Name = "rightLayers" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "leftTimeThreshold" },
                    new QueryArgument<DateTimeOffsetGraphType> { Name = "rightTimeThreshold" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "leftCIIDs" },
                    new QueryArgument<ListGraphType<GuidGraphType>> { Name = "rightCIIDs" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "leftAttributes" },
                    new QueryArgument<ListGraphType<StringGraphType>> { Name = "rightAttributes" },
                    new QueryArgument<BooleanGraphType> { Name = "showEqual" }
                    ),
                resolve: async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var (leftLayers, rightLayers) = await GetLayerArguments(context);
                    var (leftTimeThreshold, rightTimeThreshold) = GetTimeThresholdArguments(context);
                    var (leftCIIDs, rightCIIDs) = GetCIIDArguments(context);
                    var (leftAttributes, rightAttributes) = GetAttributeSelectionArguments(context);
                    var showEqual = GetShowEqual(context);
                    ICIIDSelection leftCIIDSelection = (leftCIIDs != null) ? SpecificCIIDsSelection.Build(leftCIIDs) : new AllCIIDsSelection();
                    ICIIDSelection rightCIIDSelection = (rightCIIDs != null) ? SpecificCIIDsSelection.Build(rightCIIDs) : new AllCIIDsSelection();

                    // TODO: use dataloader?
                    var leftCIs = await ciModel.GetMergedCIs(leftCIIDSelection, leftLayers, false, leftAttributes, userContext.Transaction, leftTimeThreshold);
                    var rightCIs = await ciModel.GetMergedCIs(rightCIIDSelection, rightLayers, false, rightAttributes, userContext.Transaction, rightTimeThreshold);

                    // ci-based authz
                    leftCIs = ciBasedAuthorizationService.FilterReadableCIs(leftCIs, (ci) => ci.ID);
                    rightCIs = ciBasedAuthorizationService.FilterReadableCIs(rightCIs, (ci) => ci.ID);

                    var leftTraits = (await traitsProvider.GetActiveTraits(userContext.Transaction, leftTimeThreshold)).Values;
                    var rightTraits = (await traitsProvider.GetActiveTraits(userContext.Transaction, rightTimeThreshold)).Values;
                    ISet<(Guid ciid, string traitID)> left = new HashSet<(Guid ciid, string traitID)>();
                    ISet<(Guid ciid, string traitID)> right = new HashSet<(Guid ciid, string traitID)>();
                    foreach (var trait in leftTraits)
                    {
                        var leftCIsWithTrait = await effectiveTraitModel.FilterCIsWithTrait(leftCIs, trait, leftLayers, userContext.Transaction, leftTimeThreshold);
                        left.UnionWith(leftCIsWithTrait.Select(ci => (ci.ID, trait.ID)));
                    }
                    foreach(var trait in rightTraits)
                    {
                        var rightCIsWithTrait = await effectiveTraitModel.FilterCIsWithTrait(rightCIs, trait, rightLayers, userContext.Transaction, rightTimeThreshold);
                        right.UnionWith(rightCIsWithTrait.Select(ci => (ci.ID, trait.ID)));
                    }

                    var comparisons = diffingCIService.DiffEffectiveTraits(left, right, showEqual);
                    return comparisons;
                });


        }
    }
}
