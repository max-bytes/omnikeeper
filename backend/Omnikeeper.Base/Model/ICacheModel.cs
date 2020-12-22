using System.Collections.Generic;

namespace Omnikeeper.Base.Model
{
    public interface ICacheModel
    {
        IEnumerable<string> GetKeys();
    }
}
