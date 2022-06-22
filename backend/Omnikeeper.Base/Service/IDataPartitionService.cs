using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Service
{
    public interface IDataPartitionService
    {
        Task<bool> StartNewPartition(IModelContextBuilder modelContextBuilder);
    }
}
