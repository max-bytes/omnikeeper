using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(Layer targetLayer, JObject config, IChangesetProxy changesetProxy, IModelContextBuilder modelContextBuilder, ILogger logger);

        Task<bool> CanSkipRun(JObject config, ILogger logger, IModelContextBuilder modelContextBuilder);
        void SetLastRun(DateTimeOffset lr);
    }
}
