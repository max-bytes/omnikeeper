using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;

namespace Omnikeeper.Base.GraphQL
{
    public interface IOmnikeeperUserContext
    {
        TimeThreshold GetTimeThreshold(IEnumerable<object> contextPath);
        LayerSet GetLayerSet(IEnumerable<object> contextPath);

        IModelContext Transaction { get; }
        IChangesetProxy ChangesetProxy { get; }
        IAuthenticatedUser User { get; }
        IServiceProvider ServiceProvider { get; }
    }
}
