using Landscape.Base.Entity;
using LandscapeRegistry.Entity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(Layer targetLayer, Changeset changeset, CLBErrorHandler errorHandler, Npgsql.NpgsqlTransaction trans, ILogger logger);
        Task<bool> Run(CLBSettings settings, ILogger logger);
    }
}
