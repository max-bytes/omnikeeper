using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IPartitionModel
    {
        Task<DateTimeOffset> GetLatestPartitionIndex(TimeThreshold timeThreshold, IModelContext trans);
        Task StartNewPartition(TimeThreshold timeThreshold, IModelContext trans);
    }
}
