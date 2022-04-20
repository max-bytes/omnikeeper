using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(Layer targetLayer, JsonDocument config, IChangesetProxy changesetProxy, IModelContextBuilder modelContextBuilder, ILogger logger);

        Task<bool> CanSkipRun(DateTimeOffset? lastRun, JsonDocument config, ILogger logger, IModelContextBuilder modelContextBuilder);
    }
}
