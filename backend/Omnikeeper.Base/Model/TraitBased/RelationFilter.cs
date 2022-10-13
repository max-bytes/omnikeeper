using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
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
    //public record class RelatedToCIInnerRelationFilter(FilterInput Filter) : IInnerRelationFilter { } // TODO: should this not know about the trait?

    public static class RelationFilterHelper
    {
        // NOTE: expects that the passed relations are exactly the correct relations applicable for this filter: correct predicateID, direction, CI, ...
        public static IDataLoaderResult<ICIIDSelection> MatchAgainstNonEmpty(this RelationFilter filter, IEnumerable<IGrouping<Guid, MergedRelation>> relations)
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
                //case RelatedToCIInnerRelationFilter ff:
                //    var ciids = relations.Select(g => g.Key).ToHashSet();
                //    return ff.Filter.Apply(SpecificCIIDsSelection.Build(ciids), attributeModel, relationModel, ciidModel, dataLoaderService, layerset, trans, timeThreshold);

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
                _ => throw new Exception("Encountered relation filter in unknown state"),
            };
        }
        public static bool RequiresCheckOfCIsWithNonEmptyRelations(this RelationFilter filter)
        {
            return filter.InnerFilter switch
            {
                ExactAmountInnerRelationFilter ff => ff.ExactAmount != 0,
                ExactOtherCIIDInnerRelationFilter _ => true,
                _ => throw new Exception("Encountered relation filter in unknown state"),
            };
        }
    }
}
