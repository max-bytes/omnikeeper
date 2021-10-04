using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(Layer targetLayer, JObject config, IModelContextBuilder modelContextBuilder, ILogger logger);
    }
}
