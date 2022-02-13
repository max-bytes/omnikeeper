using Omnikeeper.Base.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public class LayerSet : IEnumerable<string>, IEquatable<LayerSet>
    {
        // can be unsorted
        public string[] LayerIDs { get; private set; }
        public long LayerHash // TODO: can this be removed? Who uses this? or at least make it private
        {
            get
            {
                unchecked // we expect overflows
                {
                    return LayerIDs.Aggregate(2341L, (hash, item) => hash * 37L + item.GetHashCode());
                }
            }
        }

        public LayerSet(params string[] layerIDs)
        {
            LayerIDs = layerIDs;
        }
        public LayerSet(IEnumerable<string> layerIDs)
        {
            LayerIDs = layerIDs.ToArray();
        }

        public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)LayerIDs).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => LayerIDs.GetEnumerator();

        public bool IsEmpty => LayerIDs.Length <= 0;

        public int Length => LayerIDs.Length;

        public int GetOrder(string layerID)
        {
            return LayerIDs.IndexOf(layerID);
        }

        public override string ToString()
        {
            return string.Join(",", LayerIDs);
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                foreach (var layerID in LayerIDs)
                    hash = (hash * 16777619) ^ layerID.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as LayerSet);
        public bool Equals(LayerSet? other) => other != null && LayerIDs.SequenceEqual(other.LayerIDs);
    }
}
