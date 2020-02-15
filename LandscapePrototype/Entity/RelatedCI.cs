using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class RelatedCI
    {
        public Relation Relation { get; private set; }
        public CI CI { get; private set; }

        public static RelatedCI Build(Relation relation, CI ci)
        {
            var r = new RelatedCI
            {
                Relation = relation,
                CI = ci
            };
            return r;
        }
    }
}
