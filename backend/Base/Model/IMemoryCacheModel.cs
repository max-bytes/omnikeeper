using System.Collections.Generic;

namespace Landscape.Base.Model
{
    public interface IMemoryCacheModel
    {
        IEnumerable<string> GetKeys();
    }
}
