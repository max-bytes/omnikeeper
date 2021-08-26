using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OKPluginValidation.Validation
{
    public interface IValidationIssueModel
    {
        //Task<ValidationIssue> GetValidationIssue(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        //Task<(Guid, ValidationIssue)> TryToGetValidationIssue(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);
        Task<IDictionary<string, ValidationIssue>> GetValidationIssues(LayerSet layerSet, IModelContext trans, TimeThreshold timeThreshold);

        Task<(ValidationIssue validationIssue, bool changed)> InsertOrUpdate(string id, string message, IEnumerable<Guid> affectedCIs, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }
}
