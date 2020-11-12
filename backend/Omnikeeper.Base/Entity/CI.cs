using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Omnikeeper.Base.Entity
{
    public class MergedCI
    {
        public Guid ID { get; private set; }
        public string? Name { get; private set; }
        public IImmutableDictionary<string, MergedCIAttribute> MergedAttributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public MergedCI(Guid id, string? name, LayerSet layers, TimeThreshold atTime, IEnumerable<MergedCIAttribute> attributes)
            : this(id, name, layers, atTime, attributes.ToImmutableDictionary(a => a.Attribute.Name))
        {
        }

        public MergedCI(Guid id, string? name, LayerSet layers, TimeThreshold atTime, IImmutableDictionary<string, MergedCIAttribute> attributes)
        {
            Name = name;
            Layers = layers;
            AtTime = atTime;
            ID = id;
            MergedAttributes = attributes;
        }
    }

    //public class CI
    //{
    //    public Guid ID { get; private set; }
    //    public string Name { get; private set; }
    //    public IImmutableDictionary<string, CIAttribute> Attributes { get; private set; }
    //    public long LayerID { get; private set; }
    //    public TimeThreshold AtTime { get; private set; }

    //    public CI(Guid id, string name, long layerID, TimeThreshold atTime, IEnumerable<CIAttribute> attributes)
    //    {
    //        Name = name;
    //        LayerID = layerID;
    //        AtTime = atTime;
    //        ID = id;
    //        Attributes = attributes.ToImmutableDictionary(a => a.Name);
    //    }
    //}

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
