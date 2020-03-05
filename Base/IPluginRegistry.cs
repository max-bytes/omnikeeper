using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Landscape.Base
{
    public interface IPluginRegistry
    {
        public void RegisterComputeLayerBrains(IEnumerable<IComputeLayerBrain> brains);
    }
}
