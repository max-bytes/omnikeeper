
using LandscapePrototype.Model;
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
        public LayerSet Layers { get; private set; }
        public DateTimeOffset AtTime { get; private set; }

        public static CI Build(string CIIdentity, LayerSet layers, DateTimeOffset atTime, IEnumerable<CIAttribute> attributes)
        {
            var r = new CI
            {
                Layers = layers,
                AtTime = atTime,
                Identity = CIIdentity,
                Attributes = attributes.ToArray()
            };
            return r;
        }
    }
}
