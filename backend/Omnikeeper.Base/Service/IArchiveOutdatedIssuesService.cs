using Microsoft.Extensions.Logging;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IArchiveOutdatedIssuesService
    {
        Task<int> ArchiveOutdatedIssues(IModelContextBuilder modelContextBuilder, ILogger logger);
    }
}
