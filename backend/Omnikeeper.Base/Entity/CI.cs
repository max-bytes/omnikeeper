using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public class MergedCI
    {
        public Guid ID { get; private set; }
        public string? Name { get; private set; }
        public IDictionary<string, MergedCIAttribute> MergedAttributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public MergedCI(Guid id, string? name, LayerSet layers, TimeThreshold atTime, IEnumerable<MergedCIAttribute> attributes)
            : this(id, name, layers, atTime, attributes.ToDictionary(a => a.Attribute.Name))
        {
        }

        public MergedCI(Guid id, string? name, LayerSet layers, TimeThreshold atTime, IDictionary<string, MergedCIAttribute> attributes)
        {
            Name = name;
            Layers = layers;
            AtTime = atTime;
            ID = id;
            MergedAttributes = attributes;
        }
    }

    public class CompactCI
    {
        public Guid ID { get; private set; }
        public string? Name { get; private set; }
        public TimeThreshold AtTime { get; private set; }
        public long LayerHash { get; private set; }

        public CompactCI(Guid id, string? name, long layerHash, TimeThreshold atTime)
        {
            Name = name;
            AtTime = atTime;
            ID = id;
            LayerHash = layerHash;
        }

        public static CompactCI BuildFromMergedCI(MergedCI mergedCI)
        {
            return new CompactCI(mergedCI.ID, mergedCI.Name, mergedCI.Layers.LayerHash, mergedCI.AtTime);
        }
    }
}
