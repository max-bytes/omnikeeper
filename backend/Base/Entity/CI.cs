using Landscape.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Landscape.Base.Entity
{
    public class MergedCI
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public CIType Type { get; private set; }
        public IDictionary<string, MergedCIAttribute> MergedAttributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public static MergedCI Build(Guid id, string name, CIType type, LayerSet layers, TimeThreshold atTime, IEnumerable<MergedCIAttribute> attributes)
        {
            return Build(id, name, type, layers, atTime, attributes.ToDictionary(a => a.Attribute.Name));
        }

        public static MergedCI Build(Guid id, string name, CIType type, LayerSet layers, TimeThreshold atTime, IDictionary<string, MergedCIAttribute> attributes)
        {
            return new MergedCI
            {
                Type = type,
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
        public CIType Type { get; private set; }
        public CIAttribute[] Attributes { get; private set; } // TODO: rewrite to dictionary
        public long LayerID { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public static CI Build(Guid id, string name, CIType type, long layerID, TimeThreshold atTime, IEnumerable<CIAttribute> attributes)
        {
            return new CI
            {
                Type = type,
                Name = name,
                LayerID = layerID,
                AtTime = atTime,
                ID = id,
                Attributes = attributes.ToArray()
            };
        }
    }

    public class CompactCI
    {
        public Guid ID { get; private set; }
        public string Name { get; private set; }
        public CIType Type { get; private set; }
        public TimeThreshold AtTime { get; private set; }
        public long LayerHash { get; private set; }

        public static CompactCI Build(Guid id, string name, CIType type, long layerHash, TimeThreshold atTime)
        {
            return new CompactCI
            {
                Type = type,
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
                Type = mergedCI.Type,
                Name = mergedCI.Name,
                AtTime = mergedCI.AtTime,
                ID = mergedCI.ID
            };
        }
    }
}
