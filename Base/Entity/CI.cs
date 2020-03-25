
using LandscapePrototype.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Entity
{
    public class MergedCI
    {
        public string Identity { get; private set; }
        public CIType Type { get; private set; }
        public MergedCIAttribute[] MergedAttributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public DateTimeOffset AtTime { get; private set; }

        public static MergedCI Build(string CIIdentity, CIType type, LayerSet layers, DateTimeOffset atTime, IEnumerable<MergedCIAttribute> attributes)
        {
            return new MergedCI
            {
                Type = type,
                Layers = layers,
                AtTime = atTime,
                Identity = CIIdentity,
                MergedAttributes = attributes.ToArray()
            };
        }
    }

    public class CI
    {
        public string Identity { get; private set; }
        public CIType Type { get; private set; }
        public CIAttribute[] Attributes { get; private set; }
        public long LayerID { get; private set; }
        public DateTimeOffset AtTime { get; private set; }

        public static CI Build(string CIIdentity, CIType type, long layerID, DateTimeOffset atTime, IEnumerable<CIAttribute> attributes)
        {
            return new CI
            {
                Type = type,
                LayerID = layerID,
                AtTime = atTime,
                Identity = CIIdentity,
                Attributes = attributes.ToArray()
            };
        }
    }
}
