using Omnikeeper.Base.Entity;
using Npgsql;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerStatisticsModel
    {
        Task<long> GetActiveAttributes(Layer layer, NpgsqlTransaction trans);
        Task<long> GetAttributeChangesHistory(Layer layer, NpgsqlTransaction trans);
        Task<long> GetActiveRelations(Layer layer, NpgsqlTransaction trans);
        Task<long> GetRelationChangesHistory(Layer layer, NpgsqlTransaction trans);
        Task<long> GetLayerChangesetsHistory(Layer layer, NpgsqlTransaction trans);
    }
}
