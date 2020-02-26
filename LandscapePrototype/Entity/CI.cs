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
        public long ID { get; private set; }
        public CIAttribute[] Attributes { get; private set; }
        public LayerSet Layers { get; private set; }

        public static CI Build(long id, string CIIdentity, LayerSet layers, IEnumerable<CIAttribute> attributes)
        {
            var r = new CI
            {
                Layers = layers,
                ID = id,
                Identity = CIIdentity,
                Attributes = attributes.ToArray()
            };
            return r;
        }
    }
}
