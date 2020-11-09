using Npgsql;
using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerModel
    {
        Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans);
        Task<LayerSet> BuildLayerSet(NpgsqlTransaction trans);


        Task<Layer> CreateLayer(string name, NpgsqlTransaction trans);
        Task<Layer> CreateLayer(string name, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, NpgsqlTransaction trans);
        Task<Layer> Update(long id, Color color, AnchorState state, ComputeLayerBrainLink computeLayerBrain, OnlineInboundAdapterLink oilp, NpgsqlTransaction trans);
        Task<bool> TryToDelete(long id, NpgsqlTransaction trans);

        Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans);
        Task<Layer> GetLayer(string layerName, NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, NpgsqlTransaction trans);
    }
}
