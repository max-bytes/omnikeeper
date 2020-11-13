using Omnikeeper.Base.Entity.GridView;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IGridViewConfigModel
    {
        Task<GridViewConfiguration> GetConfiguration(string configName, IModelContext trans);
        Task<List<Context>> GetContexts(IModelContext trans);
        Task<bool> AddContext(string name, string speakingName, string description, GridViewConfiguration configuration, IModelContext trans);
        Task<bool> EditContext(string name, string speakingName, string description, GridViewConfiguration configuration, IModelContext trans);
        Task<bool> DeleteContext(string name, IModelContext trans);
    }
}
