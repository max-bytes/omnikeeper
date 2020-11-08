using Omnikeeper.Base.Entity.GridView;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IGridViewConfigModel
    {
        Task<GridViewConfiguration> GetConfiguration(string configName);
        Task<List<Context>> GetContexts();
        Task<bool> AddContext(string name, string speakingName, string description, GridViewConfiguration configuration);
        Task<bool> EditContext(string name, string speakingName, string description, GridViewConfiguration configuration);
        Task<bool> DeleteContext(string name);
    }
}
