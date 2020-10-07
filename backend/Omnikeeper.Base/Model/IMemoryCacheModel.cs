using System.Collections.Generic;

namespace Omnikeeper.Base.Model
{
    public interface IMemoryCacheModel
    {
        IEnumerable<string> GetKeys();
    }
}
