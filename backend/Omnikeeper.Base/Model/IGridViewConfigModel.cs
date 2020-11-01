using Omnikeeper.Base.Entity.GridView;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IGridViewConfigModel
    {
        Task<GridViewConfiguration> GetConfiguration(string configName);
        Task<bool> AddContext(string name, GridViewConfiguration configuration);
        Task<bool> EditContext(string name, GridViewConfiguration configuration);
        Task<bool> DeleteContext(string name);
    }
}
