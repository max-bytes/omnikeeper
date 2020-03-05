using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base
{
    public interface IComputeLayerBrain
    {
        Task<bool> Run();
    }
}
