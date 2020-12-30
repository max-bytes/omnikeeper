using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public class DataPartitionService : IDataPartitionService
    {
        private readonly ILogger<DataPartitionService> logger;
        private readonly IPartitionModel partitionModel;
        private readonly IModelContextBuilder modelContextBuilder;

        public DataPartitionService(ILogger<DataPartitionService> logger, IPartitionModel partitionModel, IModelContextBuilder modelContextBuilder)
        {
            this.logger = logger;
            this.partitionModel = partitionModel;
            this.modelContextBuilder = modelContextBuilder;
        }

        public async Task<bool> StartNewPartition()
        {
            try
            {
                var timeThreshold = TimeThreshold.BuildLatest();
                var mc = modelContextBuilder.BuildDeferred();// TODO: explore isolation level System.Data.IsolationLevel.Serializable
                await partitionModel.StartNewPartition(timeThreshold, mc);
                mc.Commit();
            } catch (Exception e)
            {
                logger.LogWarning("Could not start new data partition", e);
                return false;
            }
            return true;
        }
    }
}
