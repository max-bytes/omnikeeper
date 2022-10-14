using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.GraphQL;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class RelationFilter
    {
        public readonly IInnerRelationFilter InnerFilter;
        public readonly string PredicateID;
        public readonly bool DirectionForward;

        public RelationFilter(string predicateID, bool directionForward, IInnerRelationFilter innerFilter)
        {
            PredicateID = predicateID;
            DirectionForward = directionForward;
            InnerFilter = innerFilter;
        }
    }

    public interface IInnerRelationFilter {
    }

    public record class ExactAmountInnerRelationFilter(uint ExactAmount) : IInnerRelationFilter { }
    public record class ExactOtherCIIDInnerRelationFilter(Guid ExactOtherCIID) : IInnerRelationFilter { }
    public record class RelatedToCIInnerRelationFilter(FilterInput Filter, ITrait trait) : IInnerRelationFilter { }

    public static class RelationFilterHelper
    {
        // NOTE: expects that the passed relations are exactly the correct relations applicable for this filter: correct predicateID, direction, CI, ...
        public static IDataLoaderResult<ICIIDSelection> MatchAgainstNonEmpty(this RelationFilter filter, IEnumerable<IGrouping<Guid, MergedRelation>> relations, 
            IDataLoaderService dataLoaderService, LayerSet layerset, IModelContext trans, TimeThreshold timeThreshold)
        {
            switch (filter.InnerFilter)
            {
                case ExactAmountInnerRelationFilter ff:
                    if (ff.ExactAmount == 0)
                        throw new Exception("Must not be");
                    return new SimpleDataLoader<ICIIDSelection>(c => Task.FromResult(SpecificCIIDsSelection.Build(relations.Where(r => r.Count() == ff.ExactAmount).Select(r => r.Key).ToHashSet())));
                case ExactOtherCIIDInnerRelationFilter ff:
                    return new SimpleDataLoader<ICIIDSelection>(c => Task.FromResult(SpecificCIIDsSelection.Build(relations.Where(r => {
                        if (r.Count() != 1) return false;
                        return ff.ExactOtherCIID == ((filter.DirectionForward) ? r.First().Relation.ToCIID : r.First().Relation.FromCIID);
                    }).Select(r => r.Key).ToHashSet())));
                case RelatedToCIInnerRelationFilter ff:
                    var inverseLookup = relations.SelectMany(g => g.Select(gg => (key: g.Key, value: (filter.DirectionForward) ? gg.Relation.ToCIID : gg.Relation.FromCIID))).ToLookup(g => g.value, g => g.key);
                    var otherCIIDs = relations.SelectMany(g => g.Select(r => (filter.DirectionForward) ? r.Relation.ToCIID : r.Relation.FromCIID)).ToHashSet();
                    var r = ff.Filter.Apply(SpecificCIIDsSelection.Build(otherCIIDs), dataLoaderService, layerset, trans, timeThreshold)
                        .Then(filteredCIs =>
                        {
                            // check who actually fulfills the trait
                            return dataLoaderService.SetupAndLoadEffectiveTraits(filteredCIs, ff.trait, layerset, timeThreshold, trans)
                                .Then(ets =>
                                {
                                    var ciidsHavingTrait = ets.Keys;
                                    var baseCIIDs = ciidsHavingTrait.SelectMany(ciid => inverseLookup[ciid]).ToHashSet();
                                    return SpecificCIIDsSelection.Build(baseCIIDs);
                                });
                        }).ResolveNestedResults();
                    return r;
                default:
                    throw new Exception("Encountered relation filter in unknown state");
            }
        }

        public static IDataLoaderResult<ICIIDSelection> MatchAgainstEmpty(this RelationFilter filter, ICIIDSelection relations)
        {
            switch (filter.InnerFilter)
            {
                case ExactAmountInnerRelationFilter ff:
                    if (ff.ExactAmount != 0)
                        throw new Exception("Must not be");
                    return new SimpleDataLoader<ICIIDSelection>(c => Task.FromResult(relations));
                case ExactOtherCIIDInnerRelationFilter ff:
                    throw new Exception("Must not be");
                case RelatedToCIInnerRelationFilter ff:
                    throw new Exception("Must not be");
                default:
                    throw new Exception("Encountered relation filter in unknown state");
            }
        }

        public static bool RequiresCheckOfCIsWithEmptyRelations(this RelationFilter filter)
        {
            return filter.InnerFilter switch
            {
                ExactAmountInnerRelationFilter ff => ff.ExactAmount == 0,
                ExactOtherCIIDInnerRelationFilter _ => false,
                RelatedToCIInnerRelationFilter _ => false,
                _ => throw new Exception("Encountered relation filter in unknown state"),
            };
        }
        public static bool RequiresCheckOfCIsWithNonEmptyRelations(this RelationFilter filter)
        {
            return filter.InnerFilter switch
            {
                ExactAmountInnerRelationFilter ff => ff.ExactAmount != 0,
                ExactOtherCIIDInnerRelationFilter _ => true,
                RelatedToCIInnerRelationFilter _ => true,
                _ => throw new Exception("Encountered relation filter in unknown state"),
            };
        }
    }
}
