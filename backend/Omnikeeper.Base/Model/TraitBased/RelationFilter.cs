using GraphQL.DataLoader;
using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model.TraitBased
{
    public class TraitRelationFilter
    {
        public readonly TraitRelation traitRelation;
        public readonly RelationFilter filter;

        public TraitRelationFilter(TraitRelation traitRelation, RelationFilter filter)
        {
            this.traitRelation = traitRelation;
            this.filter = filter;
        }
    }

    public class RelationFilter
    {
        public uint? ExactAmount;

        private RelationFilter() { }

        public static object Build(uint? exactAmount)
        {
            if (exactAmount == null)
                throw new Exception("At least one filter option needs to be set for RelationFilter");
            return new RelationFilter()
            {
                ExactAmount = exactAmount
            };
        }
    }

    public static class RelationFilterHelper
    {
        // NOTE: expects that the passed relations are exactly the correct relations applicable for this filter: correct predicateID, direction, CI, ...
        public static IDataLoaderResult<IEnumerable<Guid>> MatchAgainstNonEmpty(this RelationFilter filter, IEnumerable<IGrouping<Guid, MergedRelation>> relations)
        {
            if (filter.ExactAmount != null)
            {
                if (filter.ExactAmount == 0)
                    throw new Exception("Must not be");
                return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations.Where(r => r.Count() == filter.ExactAmount).Select(r => r.Key)));
            }
            throw new Exception("Encountered relation filter in unknown state");
        }

        public static IDataLoaderResult<IEnumerable<Guid>> MatchAgainstEmpty(this RelationFilter filter, IEnumerable<Guid> relations)
        {
            if (filter.ExactAmount != null)
            {
                if (filter.ExactAmount != 0)
                    throw new Exception("Must not be");
                return new SimpleDataLoader<IEnumerable<Guid>>(c => Task.FromResult(relations));
            }
            throw new Exception("Encountered relation filter in unknown state");
        }

        public static bool RequiresCheckOfCIsWithEmptyRelations(this RelationFilter filter)
        {
            if (filter.ExactAmount != null)
            {
                return filter.ExactAmount == 0;
            }
            throw new Exception("Encountered relation filter in unknown state");
        }
        public static bool RequiresCheckOfCIsWithNonEmptyRelations(this RelationFilter filter)
        {
            if (filter.ExactAmount != null)
            {
                return filter.ExactAmount != 0;
            }
            throw new Exception("Encountered relation filter in unknown state");
        }
    }
}
