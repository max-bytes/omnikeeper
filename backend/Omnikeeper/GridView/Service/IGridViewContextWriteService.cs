using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.GridView.Entity;
using System.Threading.Tasks;

namespace Omnikeeper.GridView.Service
{
    public interface IGridViewContextWriteService
    {
        Task<(FullContext context, bool changed)> InsertOrUpdate(string id, string speakingName, string description, GridViewConfiguration configuration, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
        Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, AuthenticatedUser user, IModelContext trans);
    }
}
