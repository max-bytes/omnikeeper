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
        public readonly IInnerRelationFilter Filter;
        public readonly string PredicateID;
        public readonly bool DirectionForward;

        public RelationFilter(string predicateID, bool directionForward, IInnerRelationFilter filter)
        {
            PredicateID = predicateID;
            DirectionForward = directionForward;
            Filter = filter;
        }

    }

    public interface IInnerRelationFilter {
    }

    public record class ExactAmountInnerRelationFilter(uint ExactAmount) : IInnerRelationFilter { }
    public record class ExactOtherCIIDInnerRelationFilter(Guid ExactOtherCIID) : IInnerRelationFilter { }

    public static class RelationFilterHelper
    {
        // NOTE: expects that the passed relations are exactly the correct relations applicable for this filter: correct predicateID, direction, CI, ...
        public static IDataLoaderResult<IEnumerable<Guid>> MatchAgainstNonEmpty(this RelationFilter filter, IEnumerable<IGrouping<Guid, MergedRelation>> relations)
        {
            switch (filter.Filter)
            {
                case ExactAmountInnerRelationFilter ff:
                    if (ff.ExactAmount == 0)
                        throw new Exception("Must not be");
                    return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations.Where(r => r.Count() == ff.ExactAmount).Select(r => r.Key)));
                case ExactOtherCIIDInnerRelationFilter ff:
                    return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations.Where(r => {
                        if (r.Count() != 1) return false;
                        return ff.ExactOtherCIID == ((filter.DirectionForward) ? r.First().Relation.ToCIID : r.First().Relation.FromCIID);
                    }).Select(r => r.Key)));
                default:
                    throw new Exception("Encountered relation filter in unknown state");
            }
        }

        public static IDataLoaderResult<IEnumerable<Guid>> MatchAgainstEmpty(this RelationFilter filter, IEnumerable<Guid> relations)
        {
            switch (filter.Filter)
            {
                case ExactAmountInnerRelationFilter ff:
                    if (ff.ExactAmount != 0)
                        throw new Exception("Must not be");
                    return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations));
                case ExactOtherCIIDInnerRelationFilter ff:
                    throw new Exception("Must not be");
                default:
                    throw new Exception("Encountered relation filter in unknown state");
            }
        }

        public static bool RequiresCheckOfCIsWithEmptyRelations(this IInnerRelationFilter filter)
        {
            return filter switch
            {
                ExactAmountInnerRelationFilter ff => ff.ExactAmount == 0,
                ExactOtherCIIDInnerRelationFilter _ => false,
                _ => throw new Exception("Encountered relation filter in unknown state"),
            };
        }
        public static bool RequiresCheckOfCIsWithNonEmptyRelations(this IInnerRelationFilter filter)
        {
            return filter switch
            {
                ExactAmountInnerRelationFilter ff => ff.ExactAmount != 0,
                ExactOtherCIIDInnerRelationFilter _ => true,
                _ => throw new Exception("Encountered relation filter in unknown state"),
            };
        }
    }
}
