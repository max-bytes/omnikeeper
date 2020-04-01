
using LandscapeRegistry.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Entity
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

    public class SimplifiedCI
    {
        public string Identity { get; private set; }
        public CIType Type { get; private set; }
        public IDictionary<string, SimplifiedCIAttribute> Attributes { get; private set; }

        public static SimplifiedCI Build(string CIIdentity, CIType type, IEnumerable<SimplifiedCIAttribute> attributes)
        {
            return new SimplifiedCI
            {
                Type = type,
                Identity = CIIdentity,
                Attributes = attributes.ToDictionary(a => a.Name)
            };
        }

        public static SimplifiedCI Build(MergedCI ci)
        {
            return new SimplifiedCI
            {
                Identity = ci.Identity,
                Type = ci.Type,
                Attributes = ci.MergedAttributes.Select(ma => 
                    SimplifiedCIAttribute.Build(ma.Attribute.Name, ma.Attribute.Value.ToGeneric(), ma.Attribute.State)
                ).ToDictionary(a => a.Name)
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
