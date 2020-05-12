using Landscape.Base.Entity;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface ILayerModel
    {
        Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans);
        Task<LayerSet> BuildLayerSet(NpgsqlTransaction trans);


        Task<Layer> CreateLayer(string name, NpgsqlTransaction trans);
        Task<Layer> CreateLayer(string name, AnchorState state, ComputeLayerBrain computeLayerBrain, NpgsqlTransaction trans);
        Task<Layer> Update(long id, AnchorState state, ComputeLayerBrain computeLayerBrain, NpgsqlTransaction trans);
        Task<bool> TryToDelete(long id, NpgsqlTransaction trans);

        Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans);
        Task<Layer> GetLayer(string layerName, NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans);
        Task<IEnumerable<Layer>> GetLayers(AnchorStateFilter stateFilter, NpgsqlTransaction trans);
    }
}
