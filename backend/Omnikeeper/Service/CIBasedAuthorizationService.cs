using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;
using Omnikeeper.Base.Authz;

namespace Omnikeeper.Service
{
    public class CIBasedAuthorizationService : ICIBasedAuthorizationService
    {
        public bool CanReadAllCIs(IEnumerable<Guid> ciids, out Guid? notAllowedCI)
        {
            notAllowedCI = null;
            return true; // TODO: implement
        }

        public IEnumerable<T> FilterReadableCIs<T>(IEnumerable<T> t, Func<T, Guid> f)
        {
            foreach (var tt in t)
            {
                if (CanReadCI(f(tt)))
                    yield return tt;
            }
        }

        public IReadOnlySet<Guid> FilterReadableCIs(IReadOnlySet<Guid> ciids)
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
