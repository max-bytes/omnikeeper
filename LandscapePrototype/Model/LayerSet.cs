using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model
{
    public class LayerSet : IEnumerable<long>
    {
        public long[] LayerIDs { get; private set; }

        public LayerSet(long[] layerIDs)
        {
            LayerIDs = layerIDs;
        }

        public IEnumerator<long> GetEnumerator() => ((IEnumerable<long>)LayerIDs).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => LayerIDs.GetEnumerator();
    }
}
