using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerStatisticsModel
    {
        Task<long> GetCIIDs(IModelContext trans);
        Task<long> GetActiveAttributes(string? layerID, IModelContext trans);
        Task<long> GetAttributeChangesHistory(string? layerID, IModelContext trans);
        Task<long> GetActiveRelations(string? layerID, IModelContext trans);
        Task<long> GetRelationChangesHistory(string? layerID, IModelContext trans);
        Task<long> GetLayerChangesetsHistory(string layerID, IModelContext trans);

        Task<bool> IsLayerEmpty(string layerID, IModelContext trans);
    }
}
