using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Generator;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICLConfigModel
    {
        Task<IDictionary<string, CLConfigV1>> GetCLConfigs(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);
        Task<CLConfigV1> GetCLConfig(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, CLConfigV1)> TryToGetCLConfig(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);

        Task<(CLConfigV1 config, bool changed)> InsertOrUpdate(string id, string clBrainReference, JObject clBrainConfig, 
            LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
