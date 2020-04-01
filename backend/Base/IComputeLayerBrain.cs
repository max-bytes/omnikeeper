using LandscapePrototype.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public interface IComputeLayerBrain
    {
        string Name { get; }

        Task<bool> Run(long layerID, Changeset changeset, CLBErrorHandler errorHandler, Npgsql.NpgsqlTransaction trans);

        void RunSync(CLBSettings settings);
    }
}
