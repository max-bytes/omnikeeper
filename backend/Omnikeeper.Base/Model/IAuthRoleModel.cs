using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IAuthRoleModel
    {
        Task<IDictionary<string, AuthRole>> GetAuthRoles(LayerSet layerSet, IModelContext trans, TimeThreshold atTime);
        Task<AuthRole> GetAuthRole(string id, LayerSet layerSet, TimeThreshold atTime, IModelContext trans);
        Task<(Guid, AuthRole)> TryToGetAuthRole(string id, LayerSet layerSet, TimeThreshold timeThreshold, IModelContext trans);

        Task<(AuthRole authRole, bool changed)> InsertOrUpdate(string id, IEnumerable<string> permissions, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, LayerSet layerSet, string writeLayerID, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }

}
