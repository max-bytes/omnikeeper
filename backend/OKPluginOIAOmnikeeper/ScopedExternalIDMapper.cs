using Landscape.Base.Inbound;
using Landscape.Base.Service;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginOIAOmnikeeper
{
    public class ScopedExternalIDMapper : ScopedExternalIDMapper<ExternalIDGuid>
    {
        public ScopedExternalIDMapper(string scope, IExternalIDMapPersister persister) : base(scope, persister, (s) => new ExternalIDGuid(Guid.Parse(s)))
        {
        }
    }
}
