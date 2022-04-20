using Omnikeeper.Base.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Model.TraitBased
{
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
        public static bool Matches(this RelationFilter filter, IEnumerable<MergedRelation> relations)
        {
            if (filter.ExactAmount != null)
            {
                if (relations.Count() != filter.ExactAmount)
                {
                    return false;
                }
                else
                {
                    return true;
                }
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
