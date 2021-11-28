using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Omnikeeper.Base.DataFlow
{
    public class DataFlowLatestAttributeKeeper
    {
        // layerID, ciid, attribute name
        private readonly IDictionary<string, IDictionary<Guid, IDictionary<string, CIAttribute>>> attributes = new Dictionary<string, IDictionary<Guid, IDictionary<string, CIAttribute>>>();

        public void Update(AttributeChange change)
        {
            if (change.IsRemoved)
            {
                if (attributes.TryGetValue(change.LayerID, out var v))
                {
                    if (v.TryGetValue(change.CIID, out var vv)) {
                        vv.Remove(change.NewAttribute.Name);
                    }
                }
            } else {
                attributes.AddOrUpdate(change.LayerID, 
                    () => new Dictionary<Guid, IDictionary<string, CIAttribute>>() { { change.CIID, new Dictionary<string, CIAttribute>() { { change.NewAttribute.Name, change.NewAttribute } } } },
                    (current) => current.AddOrUpdate(change.CIID, 
                        () => new Dictionary<string, CIAttribute>() { { change.NewAttribute.Name, change.NewAttribute } }, 
                        (current) => { current[change.NewAttribute.Name] = change.NewAttribute; return current; })
                    );
            }
        }

        //internal bool TryGet(string layerID, Guid ciid, [MaybeNullWhen(false)] out IDictionary<string, CIAttribute> foundAttributes)
        //{
        //    if (attributes.TryGetValue(layerID, out var v))
        //    {
        //        if (v.TryGetValue(ciid, out foundAttributes))
        //        {
        //            return true;
        //        }
        //    }

        //    foundAttributes = null;
        //    return false;
        //}

        internal bool TryGet(string layerID, [MaybeNullWhen(false)] out IDictionary<Guid, IDictionary<string, CIAttribute>> foundAttributes)
        {
            if (attributes.TryGetValue(layerID, out foundAttributes))
            {
                return true;
            }

            foundAttributes = null;
            return false;
        }
    }
}
