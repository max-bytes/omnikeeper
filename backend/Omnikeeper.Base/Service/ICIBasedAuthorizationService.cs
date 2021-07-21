using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.Service
{
    public interface ICIBasedAuthorizationService
    {
        bool CanReadCI(Guid ciid);
        bool CanWriteToCI(Guid cIID);
        bool CanReadAllCIs(IEnumerable<Guid> ciids, out Guid? notAllowedCI);
        bool CanWriteToAllCIs(IEnumerable<Guid> enumerable, out Guid? notAllowedCI);
        IEnumerable<Guid> FilterReadableCIs(IEnumerable<Guid> ciids);
        IEnumerable<T> FilterReadableCIs<T>(IEnumerable<T> t, Func<T, Guid> f);
    }
}
