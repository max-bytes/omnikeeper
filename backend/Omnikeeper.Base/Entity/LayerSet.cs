using Omnikeeper.Base.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Entity
{
    public class LayerSet : IEnumerable<long>
    {
        // can be unsorted
        public long[] LayerIDs { get; private set; }
        public long LayerHash
        {
            get
            {
                unchecked // we expect overflows
                {
                    return LayerIDs.Aggregate(2341L, (hash, item) => hash * 37L + item);
                }
            }
        }

        public LayerSet(params long[] layerIDs)
        {
            LayerIDs = layerIDs;
        }

        public IEnumerator<long> GetEnumerator() => ((IEnumerable<long>)LayerIDs).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => LayerIDs.GetEnumerator();

        public bool IsEmpty => LayerIDs.Length <= 0;

        public static string CreateLayerSetSQLValues(LayerSet layers)
        {
            if (layers.IsEmpty) throw new Exception("Cannot create valid SQL values from an empty layerset");

            var order = 0;
            var items = new List<string>();
            foreach (var layerID in layers)
            {
                items.Add($"({layerID}, {order++})");
            }
            return $"VALUES{string.Join(',', items)}";
        }

        public int GetOrder(long layerID)
        {
            return LayerIDs.IndexOf(layerID);
        }
    }
}
