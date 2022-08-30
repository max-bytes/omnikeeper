using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IChangesetProxy
    {
        Task<Changeset> GetChangeset(string layerID, IModelContext trans);
        IEnumerable<Changeset> GetAllActiveChangesets();
        Changeset? GetActiveChangeset(string layerID);
        TimeThreshold TimeThreshold { get; }
        UserInDatabase User { get; }
    }
}
