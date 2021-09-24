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
    public interface IGeneratorModel
    {
        Task<IEnumerable<GeneratorV1>> GetGenerators(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);
        Task<GeneratorV1> GetGenerator(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<(Guid, GeneratorV1)> TryToGetGenerator(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);

        Task<(GeneratorV1 generator, bool changed)> InsertOrUpdate(string id, string attributeName, string attributeValueTemplate, 
            LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
