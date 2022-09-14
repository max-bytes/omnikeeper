using GraphQL;
using GraphQL.DataLoader;
using GraphQL.Types;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.GraphQL;
using Omnikeeper.GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Omnikeeper.Base.Authz;

namespace Omnikeeper.Service
{
    public class DiffingCIService
    {
        public IList<CIAttributesComparison> DiffCross2CIs(MergedCI left, MergedCI right, bool showEqual, CrossCIDiffingSettings crossCIDiffingSettings)
        {
            var attributeComparison = CompareAttributesOf2CIs(left, right, showEqual);
            return new List<CIAttributesComparison>() {
                new CIAttributesComparison(crossCIDiffingSettings.leftCIID, crossCIDiffingSettings.rightCIID, left.CIName, right.CIName, attributeComparison)
            };
        }

        public IList<CIAttributesComparison> DiffCIs(IEnumerable<MergedCI> left, IEnumerable<MergedCI> right, bool showEqual)
        {
            var allCIIDs = new HashSet<Guid>();
            var leftDictionary = left.ToDictionary(ci => ci.ID);
            var rightDictionary = right.ToDictionary(ci => ci.ID);
            allCIIDs.UnionWith(leftDictionary.Keys);
            allCIIDs.UnionWith(rightDictionary.Keys);

            var ciComparisons = new List<CIAttributesComparison>();
            foreach (var ciid in allCIIDs)
            {
                if (!leftDictionary.TryGetValue(ciid, out var leftCI))
                {
                    var rightCI = rightDictionary[ciid];
                    var attributes = rightCI.MergedAttributes.Select(a => new AttributeComparison(a.Key, null, a.Value, ComparisonStatus.Unequal));
                    ciComparisons.Add(new CIAttributesComparison(ciid, ciid, null, rightCI.CIName, attributes));
                }
                else if (!rightDictionary.TryGetValue(ciid, out var rightCI))
                {
                    var attributes = leftCI.MergedAttributes.Select(a => new AttributeComparison(a.Key, a.Value, null, ComparisonStatus.Unequal));
                    ciComparisons.Add(new CIAttributesComparison(ciid, ciid, leftCI.CIName, null, attributes));
                }
                else
                {
                    IEnumerable<AttributeComparison> attributes = CompareAttributesOf2CIs(leftCI, rightCI, showEqual);
                    if (showEqual || !attributes.IsEmpty())
                        ciComparisons.Add(new CIAttributesComparison(ciid, ciid, leftCI.CIName, rightCI.CIName, attributes));
                }
            }

            return ciComparisons;
        }

        private static IEnumerable<AttributeComparison> CompareAttributesOf2CIs(MergedCI leftCI, MergedCI rightCI, bool showEqual)
        {
            var allNames = new HashSet<string>();
            allNames.UnionWith(leftCI.MergedAttributes.Keys);
            allNames.UnionWith(rightCI.MergedAttributes.Keys);
            var attributes = allNames.Select(name =>
            {
                if (!leftCI.MergedAttributes.TryGetValue(name, out var leftAttribute))
                {
                    return new AttributeComparison(name, null, rightCI.MergedAttributes[name], ComparisonStatus.Unequal);
                }
                else if (!rightCI.MergedAttributes.TryGetValue(name, out var rightAttribute))
                {
                    return new AttributeComparison(name, leftCI.MergedAttributes[name], null, ComparisonStatus.Unequal);
                }
                else
                {
                    var status = (leftAttribute.Attribute.Value.Equals(rightAttribute.Attribute.Value)) ? ComparisonStatus.Equal : ComparisonStatus.Unequal;
                    return new AttributeComparison(name, leftAttribute, rightAttribute, status);
                }
            }).Where(a => showEqual || a.status != ComparisonStatus.Equal);
            return attributes;
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

            var ret = dict.Select(kv => new CIRelationsComparison(kv.Key, kv.Key, kv.Value));
            return ret;
        }

        public IEnumerable<CIRelationsComparison> DiffRelationsOf2CIs(IEnumerable<MergedRelation> leftRelations, IEnumerable<MergedRelation> rightRelations, bool outgoing, bool showEqual, CrossCIDiffingSettings crossCIDiffingSetting)
        {
            // NOTE: this should succeed because there must only be one "this"CIID on both left and right relations, so we can exclude "this"CIID from the dict key generation
            var left = leftRelations.ToDictionary(r => r.Relation.PredicateID + ((outgoing) ? r.Relation.ToCIID : r.Relation.FromCIID));
            var right = rightRelations.ToDictionary(r => r.Relation.PredicateID + ((outgoing) ? r.Relation.ToCIID : r.Relation.FromCIID));

            var allKeys = new HashSet<string>();
            allKeys.UnionWith(left.Keys);
            allKeys.UnionWith(right.Keys);
            var list = new List<RelationComparison>();
            foreach (var key in allKeys)
            {
                if (!left.TryGetValue(key, out var leftRelation))
                {
                    var rightRelation = right[key];
                    var rc = new RelationComparison(rightRelation.Relation.PredicateID, rightRelation.Relation.FromCIID, rightRelation.Relation.ToCIID, null, rightRelation, ComparisonStatus.Unequal);
                    list.Add(rc);

                }
                else if (!right.TryGetValue(key, out var rightRelation))
                {
                    var rc = new RelationComparison(leftRelation.Relation.PredicateID, leftRelation.Relation.FromCIID, leftRelation.Relation.ToCIID, leftRelation, null, ComparisonStatus.Unequal);
                    list.Add(rc);
                }
                else
                {
                    if (showEqual)
                    {
                        var rc = new RelationComparison(leftRelation.Relation.PredicateID, leftRelation.Relation.FromCIID, leftRelation.Relation.ToCIID, leftRelation, rightRelation, ComparisonStatus.Equal);
                        list.Add(rc);
                    }
                }
            }

            return new List<CIRelationsComparison>() { new CIRelationsComparison(crossCIDiffingSetting.leftCIID, crossCIDiffingSetting.rightCIID, list) };
        }

        public IEnumerable<CIEffectiveTraitsComparison> DiffEffectiveTraits(IDictionary<Guid, (ISet<string> traitIDs, string? ciName)> left, IDictionary<Guid, (ISet<string> traitIDs, string? ciName)> right, bool showEqual)
        {
            var allCIIDs = new HashSet<Guid>();
            allCIIDs.UnionWith(left.Keys);
            allCIIDs.UnionWith(right.Keys);
            var dict = new List<CIEffectiveTraitsComparison>(allCIIDs.Count);
            foreach (var ciid in allCIIDs)
            {
                if (!left.TryGetValue(ciid, out var leftT))
                {
                    var rightT = right[ciid];
                    dict.Add(new CIEffectiveTraitsComparison(ciid, ciid, null, rightT.ciName, rightT.traitIDs.Select(traitID => new EffectiveTraitComparison(traitID, false, true)).ToList()));
                }
                else if (!right.TryGetValue(ciid, out var rightT))
                {
                    dict.Add(new CIEffectiveTraitsComparison(ciid, ciid, leftT.ciName, null, leftT.traitIDs.Select(traitID => new EffectiveTraitComparison(traitID, true, false)).ToList()));
                }
                else
                {
                    var onlyInLeft = leftT.traitIDs.Except(rightT.traitIDs);
                    var onlyInRight = rightT.traitIDs.Except(leftT.traitIDs);

                    var inBoth = (showEqual) ? leftT.traitIDs.Intersect(rightT.traitIDs) : Array.Empty<string>();

                    var all = onlyInLeft.Select(traitID => new EffectiveTraitComparison(traitID, true, false))
                        .Concat(onlyInRight.Select(traitID => new EffectiveTraitComparison(traitID, false, true)))
                        .Concat(inBoth.Select(traitID => new EffectiveTraitComparison(traitID, true, true)));
                    dict.Add(new CIEffectiveTraitsComparison(ciid, ciid, leftT.ciName, rightT.ciName, all.ToList()));
                }
            }
            return dict;
        }

        public IEnumerable<CIEffectiveTraitsComparison> DiffEffectiveTraitsOf2CIs(IDictionary<Guid, (ISet<string> traitIDs, string? ciName)> left, IDictionary<Guid, (ISet<string> traitIDs, string? ciName)> right, bool showEqual, CrossCIDiffingSettings crossCIDiffingSetting)
        {
            var leftT = left.GetValueOrDefault(crossCIDiffingSetting.leftCIID, () => (new HashSet<string>(), null));
            var rightT = right.GetValueOrDefault(crossCIDiffingSetting.rightCIID, () => (new HashSet<string>(), null));

            var list = new List<EffectiveTraitComparison>();
            foreach (var l in leftT.traitIDs)
            {
                if (rightT.traitIDs.Contains(l))
                {
                    rightT.traitIDs.Remove(l); // NOTE, HACK: watch out, we are modifying the passed in collection!
                    if (showEqual)
                    {
                        var et = new EffectiveTraitComparison(l, true, true);
                        list.Add(et);
                    }
                }
                else
                {
                    var et = new EffectiveTraitComparison(l, true, false);
                    list.Add(et);
                }
            }
            foreach (var r in rightT.traitIDs)
            {
                var et = new EffectiveTraitComparison(r, false, true);
                list.Add(et);
            }

            return new List<CIEffectiveTraitsComparison>() { new CIEffectiveTraitsComparison(crossCIDiffingSetting.leftCIID, crossCIDiffingSetting.rightCIID, leftT.ciName, rightT.ciName, list) };
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
        public readonly Guid leftCIID;
        public readonly Guid rightCIID;
        public readonly string? leftCIName;
        public readonly string? rightCIName;

        public readonly IEnumerable<AttributeComparison> attributes;

        public CIAttributesComparison(Guid leftCIID, Guid rightCIID, string? leftCIName, string? rightCIName, IEnumerable<AttributeComparison> attributes)
        {
            this.leftCIID = leftCIID;
            this.rightCIID = rightCIID;
            this.leftCIName = leftCIName;
            this.rightCIName = rightCIName;
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
        public readonly Guid leftCIID;
        public readonly Guid rightCIID;
        public readonly IEnumerable<RelationComparison> relations;

        public CIRelationsComparison(Guid leftCIID, Guid rightCIID, IEnumerable<RelationComparison> relations)
        {
            this.leftCIID = leftCIID;
            this.rightCIID = rightCIID;
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
        public readonly Guid leftCIID;
        public readonly Guid rightCIID;
        public readonly string? leftCIName;
        public readonly string? rightCIName;

        public readonly IEnumerable<EffectiveTraitComparison> effectiveTraits;

        public CIEffectiveTraitsComparison(Guid leftCIID, Guid rightCIID, string? leftCIName, string? rightCIName, IEnumerable<EffectiveTraitComparison> effectiveTraits)
        {
            this.leftCIID = leftCIID;
            this.rightCIID = rightCIID;
            this.leftCIName = leftCIName;
            this.rightCIName = rightCIName;
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
            Field("leftCIID", x => x.leftCIID);
            Field("rightCIID", x => x.rightCIID);
            Field("leftCIName", x => x.leftCIName, type: typeof(StringGraphType));
            Field("rightCIName", x => x.rightCIName, type: typeof(StringGraphType));
            Field("attributeComparisons", x => x.attributes, type: typeof(ListGraphType<AttributeComparisonType>));
        }
    }

    public class CIRelationsComparisonType : ObjectGraphType<CIRelationsComparison>
    {
        public CIRelationsComparisonType(IDataLoaderService dataLoaderService, IAttributeModel attributeModel, ICIIDModel ciidModel)
        {
            Field("leftCIID", x => x.leftCIID);
            Field<StringGraphType>("leftCIName")
                .Resolve((context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.leftCIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field("rightCIID", x => x.rightCIID);
            Field<StringGraphType>("rightCIName")
                .Resolve((context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var layerset = userContext.GetLayerSet(context.Path);
                    var timeThreshold = userContext.GetTimeThreshold(context.Path);
                    var ciid = context.Source!.rightCIID;
                    return dataLoaderService.SetupAndLoadCINames(SpecificCIIDsSelection.Build(ciid), attributeModel, ciidModel, layerset, timeThreshold, userContext.Transaction)
                        .Then(rr => rr.GetOrWithClass(ciid, null));
                });
            Field("relationComparisons", x => x.relations, type: typeof(ListGraphType<RelationComparisonType>));
        }
    }

    public class CIEffectiveTraitsComparisonType : ObjectGraphType<CIEffectiveTraitsComparison>
    {
        public CIEffectiveTraitsComparisonType()
        {
            Field("leftCIID", x => x.leftCIID);
            Field("rightCIID", x => x.rightCIID);
            Field("leftCIName", x => x.leftCIName, type: typeof(StringGraphType));
            Field("rightCIName", x => x.rightCIName, type: typeof(StringGraphType));
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

    public class CrossCIDiffingSettings
    {
        public readonly Guid leftCIID;
        public readonly Guid rightCIID;

        public CrossCIDiffingSettings(Guid leftCIID, Guid rightCIID)
        {
            this.leftCIID = leftCIID;
            this.rightCIID = rightCIID;
        }
    }

    public class DiffingResult
    {
        public readonly ICIIDSelection leftCIIDSelection;
        public readonly ICIIDSelection rightCIIDSelection;
        public readonly IAttributeSelection leftAttributes;
        public readonly IAttributeSelection rightAttributes;
        public readonly LayerSet leftLayers;
        public readonly LayerSet rightLayers;
        public readonly TimeThreshold leftTimeThreshold;
        public readonly TimeThreshold rightTimeThreshold;
        public readonly bool showEqual;
        public readonly CrossCIDiffingSettings? crossCIDiffingSettings;

        public DiffingResult(ICIIDSelection leftCIIDSelection, ICIIDSelection rightCIIDSelection, IAttributeSelection leftAttributes, IAttributeSelection rightAttributes, LayerSet leftLayers, LayerSet rightLayers,
            TimeThreshold leftTimeThreshold, TimeThreshold rightTimeThreshold, bool showEqual, CrossCIDiffingSettings? crossCIDiffingSettings)
        {
            this.leftCIIDSelection = leftCIIDSelection;
            this.rightCIIDSelection = rightCIIDSelection;
            this.leftAttributes = leftAttributes;
            this.rightAttributes = rightAttributes;
            this.leftLayers = leftLayers;
            this.rightLayers = rightLayers;
            this.leftTimeThreshold = leftTimeThreshold;
            this.rightTimeThreshold = rightTimeThreshold;
            this.showEqual = showEqual;
            this.crossCIDiffingSettings = crossCIDiffingSettings;
        }
    }

    public class DiffingResultType : ObjectGraphType<DiffingResult>
    {
        public DiffingResultType(IDataLoaderService dataLoaderService, DiffingCIService diffingCIService, ICIModel ciModel, IRelationModel relationModel, IEffectiveTraitModel effectiveTraitModel,
            ITraitsProvider traitsProvider, ICIBasedAuthorizationService ciBasedAuthorizationService)
        {
            Field<ListGraphType<CIAttributesComparisonType>>("cis")
                .ResolveAsync(async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var d = context.Source!;

                    // TODO: use dataloader?
                    IEnumerable<MergedCI> leftCIs = await ciModel.GetMergedCIs(d.leftCIIDSelection, d.leftLayers, false, d.leftAttributes, userContext.Transaction, d.leftTimeThreshold);
                    IEnumerable<MergedCI> rightCIs = await ciModel.GetMergedCIs(d.rightCIIDSelection, d.rightLayers, false, d.rightAttributes, userContext.Transaction, d.rightTimeThreshold);

                    // ci-based authz
                    leftCIs = ciBasedAuthorizationService.FilterReadableCIs(leftCIs, (ci) => ci.ID);
                    rightCIs = ciBasedAuthorizationService.FilterReadableCIs(rightCIs, (ci) => ci.ID);

                    var comparisons = (d.crossCIDiffingSettings != null) ?
                        diffingCIService.DiffCross2CIs(leftCIs.First(), rightCIs.First(), d.showEqual, d.crossCIDiffingSettings) :
                        diffingCIService.DiffCIs(leftCIs, rightCIs, d.showEqual);
                    return comparisons;
                });

            async Task<IEnumerable<CIRelationsComparison>> ResolveRelationComparisons(IResolveFieldContext context, bool outgoing, DiffingResult d)
            {
                var userContext = (context.UserContext as OmnikeeperUserContext)!;

                Func<IReadOnlySet<Guid>, IRelationSelection> srb = (ciids) => RelationSelectionFrom.BuildWithAllPredicateIDs(ciids);
                if (!outgoing) srb = (ciids) => RelationSelectionTo.BuildWithAllPredicateIDs(ciids);
                IRelationSelection leftRelationSelection = (d.leftCIIDSelection is SpecificCIIDsSelection leftSS) ? srb(leftSS.CIIDs) : RelationSelectionAll.Instance;
                IRelationSelection rightRelationSelection = (d.rightCIIDSelection is SpecificCIIDsSelection rightSS) ? srb(rightSS.CIIDs) : RelationSelectionAll.Instance;

                var leftLoaded = dataLoaderService.SetupAndLoadRelation(leftRelationSelection, relationModel, d.leftLayers, d.leftTimeThreshold, userContext.Transaction);
                var leftRelations = await leftLoaded.GetResultAsync();
                var rightLoaded = dataLoaderService.SetupAndLoadRelation(rightRelationSelection, relationModel, d.rightLayers, d.rightTimeThreshold, userContext.Transaction);
                var rightRelations = await rightLoaded.GetResultAsync();

                if (leftRelations == null)
                    throw new Exception("Could not load left relations");
                if (rightRelations == null)
                    throw new Exception("Could not load right relations");

                // ci-based authorization
                leftRelations = leftRelations.Where(r => ciBasedAuthorizationService.CanReadCI(r.Relation.FromCIID) && ciBasedAuthorizationService.CanReadCI(r.Relation.ToCIID));
                rightRelations = rightRelations.Where(r => ciBasedAuthorizationService.CanReadCI(r.Relation.FromCIID) && ciBasedAuthorizationService.CanReadCI(r.Relation.ToCIID));

                return (d.crossCIDiffingSettings != null) ?
                    diffingCIService.DiffRelationsOf2CIs(leftRelations, rightRelations, outgoing, d.showEqual, d.crossCIDiffingSettings) :
                    diffingCIService.DiffRelations(leftRelations, rightRelations, outgoing, d.showEqual);
            }

            Field<ListGraphType<CIRelationsComparisonType>>("outgoingRelations")
                .ResolveAsync(async (context) =>
                {
                    return await ResolveRelationComparisons(context, true, context.Source!);
                });
            Field<ListGraphType<CIRelationsComparisonType>>("incomingRelations")
                .ResolveAsync(async (context) =>
                {
                    return await ResolveRelationComparisons(context, false, context.Source!);
                });

            Field<ListGraphType<CIEffectiveTraitsComparisonType>>("effectiveTraits")
                .ResolveAsync(async (context) =>
                {
                    var userContext = (context.UserContext as OmnikeeperUserContext)!;
                    var d = context.Source!;

                    // TODO: use dataloader?
                    IEnumerable<MergedCI> leftCIs = await ciModel.GetMergedCIs(d.leftCIIDSelection, d.leftLayers, false, d.leftAttributes, userContext.Transaction, d.leftTimeThreshold);
                    IEnumerable<MergedCI> rightCIs = await ciModel.GetMergedCIs(d.rightCIIDSelection, d.rightLayers, false, d.rightAttributes, userContext.Transaction, d.rightTimeThreshold);

                    // ci-based authz
                    leftCIs = ciBasedAuthorizationService.FilterReadableCIs(leftCIs, (ci) => ci.ID);
                    rightCIs = ciBasedAuthorizationService.FilterReadableCIs(rightCIs, (ci) => ci.ID);

                    var leftTraits = (await traitsProvider.GetActiveTraits(userContext.Transaction, d.leftTimeThreshold)).Values;
                    var rightTraits = (await traitsProvider.GetActiveTraits(userContext.Transaction, d.rightTimeThreshold)).Values;

                    IDictionary<Guid, (ISet<string> traitIDs, string? ciName)> left = new Dictionary<Guid, (ISet<string> traitIDs, string? ciName)>();
                    IDictionary<Guid, (ISet<string> traitIDs, string? ciName)> right = new Dictionary<Guid, (ISet<string> traitIDs, string? ciName)>();

                    foreach (var trait in leftTraits)
                    {
                        var leftCIsWithTrait = effectiveTraitModel.FilterCIsWithTrait(leftCIs, trait, d.leftLayers);
                        foreach (var ci in leftCIsWithTrait)
                            left.AddOrUpdate(ci.ID, () => (new HashSet<string>() { trait.ID }, ci.CIName), (current) => { current.traitIDs.Add(trait.ID); return current; });
                    }
                    foreach (var trait in rightTraits)
                    {
                        var rightCIsWithTrait = effectiveTraitModel.FilterCIsWithTrait(rightCIs, trait, d.rightLayers);
                        foreach (var ci in rightCIsWithTrait)
                            right.AddOrUpdate(ci.ID, () => (new HashSet<string>() { trait.ID }, ci.CIName), (current) => { current.traitIDs.Add(trait.ID); return current; });
                    }

                    var comparisons = (d.crossCIDiffingSettings != null) ?
                        diffingCIService.DiffEffectiveTraitsOf2CIs(left, right, d.showEqual, d.crossCIDiffingSettings) :
                        diffingCIService.DiffEffectiveTraits(left, right, d.showEqual);
                    return comparisons;
                });


        }
    }
}
