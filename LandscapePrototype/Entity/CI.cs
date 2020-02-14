using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class CI
    {
        public CIAttribute[] Attributes { get; private set; }

        public static CI Build(IEnumerable<CIAttribute> attributes)
        {
            var r = new CI();
            r.Attributes = attributes.ToArray();
            return r;
        }
    }
}
