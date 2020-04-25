using System;

namespace Landscape.Base.Entity
{
    public class RelatedCI
    {
        public Relation Relation { get; private set; }
        public string CIName { get; private set; }
        public Guid CIID { get; private set; }
        public bool IsForward { get; private set; }

        public static RelatedCI Build(Relation relation, Guid ciid, string ciName, bool isForward)
        {
            var r = new RelatedCI
            {
                Relation = relation,
                CIID = ciid,
                CIName = ciName,
                IsForward = isForward
            };
            return r;
        }
    }
}
