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
        public readonly InnerRelationFilter Filter;
        public readonly string PredicateID;
        public readonly bool DirectionForward;

        public RelationFilter(string predicateID, bool directionForward, InnerRelationFilter filter)
        {
            PredicateID = predicateID;
            DirectionForward = directionForward;
            Filter = filter;
        }

    }

    public class InnerRelationFilter
    {
        public uint? ExactAmount;
        public Guid? ExactOtherCIID;

        private InnerRelationFilter() { }

        public static object Build(uint? exactAmount, Guid? exactOtherCIID)
        {
            if (exactAmount == null && exactOtherCIID == null)
                throw new Exception("At least one filter option needs to be set for RelationFilter");
            return new InnerRelationFilter()
            {
                ExactAmount = exactAmount,
                ExactOtherCIID = exactOtherCIID,
            };
        }
    }

    public static class RelationFilterHelper
    {
        // NOTE: expects that the passed relations are exactly the correct relations applicable for this filter: correct predicateID, direction, CI, ...
        public static IDataLoaderResult<IEnumerable<Guid>> MatchAgainstNonEmpty(this RelationFilter filter, IEnumerable<IGrouping<Guid, MergedRelation>> relations)
        {
            if (filter.Filter.ExactAmount != null)
            {
                if (filter.Filter.ExactAmount == 0)
                    throw new Exception("Must not be");
                return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations.Where(r => r.Count() == filter.Filter.ExactAmount).Select(r => r.Key)));
            } else if (filter.Filter.ExactOtherCIID != null)
            {
                return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations.Where(r => {
                    if (r.Count() != 1) return false;
                    return filter.Filter.ExactOtherCIID.Value == ((filter.DirectionForward) ? r.First().Relation.ToCIID : r.First().Relation.FromCIID);
                }).Select(r => r.Key)));
            }
            throw new Exception("Encountered relation filter in unknown state");
        }

        public static IDataLoaderResult<IEnumerable<Guid>> MatchAgainstEmpty(this InnerRelationFilter filter, IEnumerable<Guid> relations)
        {
            if (filter.ExactAmount != null)
            {
                if (filter.ExactAmount != 0)
                    throw new Exception("Must not be");
                return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations));
            } else if (filter.ExactOtherCIID != null)
            {
                throw new Exception("Must not be");
            }
            throw new Exception("Encountered relation filter in unknown state");
        }

        public static bool RequiresCheckOfCIsWithEmptyRelations(this InnerRelationFilter filter)
        {
            if (filter.ExactAmount != null)
            {
                return filter.ExactAmount == 0;
            } else if (filter.ExactOtherCIID != null)
            {
                return false;
            }
            throw new Exception("Encountered relation filter in unknown state");
        }
        public static bool RequiresCheckOfCIsWithNonEmptyRelations(this InnerRelationFilter filter)
        {
            if (filter.ExactAmount != null)
            {
                return filter.ExactAmount != 0;
            } else if (filter.ExactOtherCIID != null)
            {
                return true;
            }
            throw new Exception("Encountered relation filter in unknown state");
        }
    }
}
