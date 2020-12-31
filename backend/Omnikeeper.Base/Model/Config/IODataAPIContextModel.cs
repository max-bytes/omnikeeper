using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IODataAPIContextModel
    {
        Task<IEnumerable<ODataAPIContext>> GetContexts(IModelContext trans);
        Task<ODataAPIContext> GetContextByID(string id, IModelContext trans);
        Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, IModelContext trans);
        Task<ODataAPIContext> Delete(string id, IModelContext trans);
    }
}
