using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Entity
{
    public class MergedCI
    {
        public Guid ID { get; private set; }
        public string? CIName { get; private set; }
        public IDictionary<string, MergedCIAttribute> MergedAttributes { get; private set; }
        public LayerSet Layers { get; private set; }
        public TimeThreshold AtTime { get; private set; }

        public MergedCI(Guid id, string? ciName, LayerSet layers, TimeThreshold atTime, IDictionary<string, MergedCIAttribute> attributes)
        {
            CIName = ciName;
            Layers = layers;
            AtTime = atTime;
            ID = id;
            MergedAttributes = attributes;
        }
    }
}
