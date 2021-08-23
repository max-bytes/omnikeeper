using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
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
        Task<IDictionary<string, FullContext>> GetFullContexts(LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<FullContext> GetFullContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, FullContext)> TryToGetFullContext(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);

        Task<(FullContext fullContext, bool changed)> InsertOrUpdate(string id, string speakingName, string description, GridViewConfiguration configuration, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
