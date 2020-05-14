using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, Npgsql.NpgsqlTransaction trans, ILogger logger);
        Task<bool> Run(CLBSettings settings, ILogger logger);
    }
}
