
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
        public CIType Type { get; private set; }
        public MergedCIAttribute[] Attributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public DateTimeOffset AtTime { get; private set; }

        public static CI Build(string CIIdentity, CIType type, LayerSet layers, DateTimeOffset atTime, IEnumerable<MergedCIAttribute> attributes)
        {
            return new CI
            {
                Type = type,
                Layers = layers,
                AtTime = atTime,
                Identity = CIIdentity,
                Attributes = attributes.ToArray()
            };
        }
    }
}
