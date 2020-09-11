using Landscape.Base.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Landscape.Base.Entity
{
    public class MergedCI
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public IImmutableDictionary<string, MergedCIAttribute> MergedAttributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public static MergedCI Build(Guid id, string name, LayerSet layers, TimeThreshold atTime, IEnumerable<MergedCIAttribute> attributes)
        {
            return Build(id, name, layers, atTime, attributes.ToImmutableDictionary(a => a.Attribute.Name));
        }

        public static MergedCI Build(Guid id, string name, LayerSet layers, TimeThreshold atTime, IImmutableDictionary<string, MergedCIAttribute> attributes)
        {
            return new MergedCI
            {
                Name = name,
                Layers = layers,
                AtTime = atTime,
                ID = id,
                MergedAttributes = attributes
            };
        }
    }

    public class CI
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public IImmutableDictionary<string, CIAttribute> Attributes { get; private set; }
        public long LayerID { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public static CI Build(Guid id, string name, long layerID, TimeThreshold atTime, IEnumerable<CIAttribute> attributes)
        {
            return new CI
            {
                Name = name,
                LayerID = layerID,
                AtTime = atTime,
                ID = id,
                Attributes = attributes.ToImmutableDictionary(a => a.Name)
            };
        }
    }

    public class CompactCI
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public TimeThreshold AtTime { get; private set; }
        public long LayerHash { get; private set; }

        public static CompactCI Build(Guid id, string name, long layerHash, TimeThreshold atTime)
        {
            return new CompactCI
            {
                Name = name,
                AtTime = atTime,
                ID = id,
                LayerHash = layerHash
            };
        }

        public static CompactCI Build(MergedCI mergedCI)
        {
            return new CompactCI
            {
                Name = mergedCI.Name,
                AtTime = mergedCI.AtTime,
                ID = mergedCI.ID
            };
        }
    }
}
