using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IChangesetStatisticsModel
    {
        public Task<ChangesetStatistics> GetStatistics(Guid changesetID, IModelContext trans);
    }
}
