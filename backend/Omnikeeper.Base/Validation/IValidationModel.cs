using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Validation
{
    public interface IValidationModel
    {
        //Task<Validation> GetValidation(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        //Task<(Guid, Validation)> TryToGetValidation(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<IDictionary<string, Validation>> GetValidations(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);

        Task<(Validation validationIssue, bool changed)> InsertOrUpdate(string id, string ruleName, JObject ruleConfig, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
