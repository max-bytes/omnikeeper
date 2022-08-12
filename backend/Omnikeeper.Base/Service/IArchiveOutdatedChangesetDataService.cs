using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IArchiveOutdatedChangesetDataService
    {
        public Task<int> Archive(ILogger logger, IModelContext trans);
    }
}
