using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITemplatesProvider
    {
        public Task<Templates> GetTemplates(IModelContext trans);
    }
}
