using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class CI
    {
        public string Identity { get; private set; }
        public CIAttribute[] Attributes { get; private set; }

        public static CI Build(string CIIdentity, IEnumerable<CIAttribute> attributes)
        {
            var r = new CI
            {
                Identity = CIIdentity,
                Attributes = attributes.ToArray()
            };
            return r;
        }
    }
}
