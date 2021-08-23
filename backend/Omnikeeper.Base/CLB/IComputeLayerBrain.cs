using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, IModelContext trans, ILogger logger);
        Task<bool> Run(CLBSettings settings, IModelContextBuilder modelContextBuilder, ILogger logger);
    }
}
