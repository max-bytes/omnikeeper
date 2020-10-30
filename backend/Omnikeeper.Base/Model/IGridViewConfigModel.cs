using Omnikeeper.Base.Entity.GridView;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IGridViewConfigModel
    {
        Task<GridViewConfiguration> GetConfiguration(string configName);
    }
}
