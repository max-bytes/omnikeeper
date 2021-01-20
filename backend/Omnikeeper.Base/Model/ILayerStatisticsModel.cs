using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerStatisticsModel
    {
        Task<long> GetActiveAttributes(Layer layer, IModelContext trans);
        Task<long> GetAttributeChangesHistory(Layer layer, IModelContext trans);
        Task<long> GetActiveRelations(Layer layer, IModelContext trans);
        Task<long> GetRelationChangesHistory(Layer layer, IModelContext trans);
        Task<long> GetLayerChangesetsHistory(Layer layer, IModelContext trans);
    }
}
