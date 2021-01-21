using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Service
{
    public class CIBasedAuthorizationService : ICIBasedAuthorizationService
    {
        public bool CanReadAllCIs(IEnumerable<Guid> ciids, out Guid? notAllowedCI)
        {
            notAllowedCI = null;
            return true; // TODO: implement
        }

        // TODO: add to interface, test and use
        public IEnumerable<T> FilterReadableCIs<T>(IEnumerable<T> t, Func<T, Guid> f)
        {
            var d = t.ToDictionary(tt => f(tt));
            var filtered = FilterReadableCIs(d.Keys);
            return d.Where(dd => filtered.Contains(dd.Key)).Select(dd => dd.Value);
        }

        public IEnumerable<Guid> FilterReadableCIs(IEnumerable<Guid> ciids)
        {
            return ciids; // TODO: implement
        }

        public bool CanReadCI(Guid ciid)
        {
            return true; // TODO: implement
        }

        public bool CanWriteToAllCIs(IEnumerable<Guid> enumerable, out Guid? notAllowedCI)
        {
            notAllowedCI = null;
            return true; // TODO: implement
        }

        public bool CanWriteToCI(Guid cIID)
        {
            return true; // TODO: implement
        }
    }
}
