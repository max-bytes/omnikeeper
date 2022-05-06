using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ICIIDModel
    {
        Task<IReadOnlySet<Guid>> GetCIIDs(IModelContext trans);
        Task<bool> CIIDExists(Guid id, IModelContext trans);
    }
}
