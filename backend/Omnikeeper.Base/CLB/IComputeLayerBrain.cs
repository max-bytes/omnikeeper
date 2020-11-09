using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System.Threading.Tasks;

namespace Omnikeeper.Base.CLB
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        string[] RequiredPredicates { get; }
        RecursiveTraitSet DefinedTraits { get; }

        Task<bool> Run(Layer targetLayer, IChangesetProxy changesetProxy, CLBErrorHandler errorHandler, Npgsql.NpgsqlTransaction trans, ILogger logger);
        Task<bool> Run(CLBSettings settings, ILogger logger);
    }
}
