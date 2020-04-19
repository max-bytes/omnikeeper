using System;

namespace Landscape.Base.Entity
{
    public class RelatedCI
    {
        public Relation Relation { get; private set; }
        //public CI CI { get; private set; }
        public Guid CIID { get; private set; }
        public bool IsForward { get; private set; }

        public static RelatedCI Build(Relation relation, Guid ciid, bool isForward)
        {
            var r = new RelatedCI
            {
                Relation = relation,
                CIID = ciid,
                IsForward = isForward
            };
            return r;
        }
    }
}
