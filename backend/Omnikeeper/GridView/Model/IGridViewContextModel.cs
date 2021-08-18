using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Model
{
    public interface IGridViewContextModel
    {
        Task<IDictionary<string, Context>> GetContexts(TimeThreshold timeThreshold, IModelContext trans);

        Task<FullContext> GetFullContext(string id, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, FullContext)> TryToGetFullContext(string id, TimeThreshold timeThreshold, IModelContext trans);
    }
}
