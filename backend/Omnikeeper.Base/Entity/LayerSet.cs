using Omnikeeper.Base.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public class LayerSet : IEnumerable<string>
    {
        // can be unsorted
        public string[] LayerIDs { get; private set; }
        public long LayerHash
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
    }
}
