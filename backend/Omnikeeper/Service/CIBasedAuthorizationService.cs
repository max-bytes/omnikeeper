using Omnikeeper.Base.Service;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Service
{
    public class CIBasedAuthorizationService : ICIBasedAuthorizationService
    {
        public bool CanReadAllCIs(IEnumerable<Guid> ciids, out Guid? notAllowedCI)
        {
            notAllowedCI = null;
            return true; // TODO: implement
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
