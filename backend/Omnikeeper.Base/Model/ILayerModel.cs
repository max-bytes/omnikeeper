using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerModel
    {
        static readonly OnlineInboundAdapterLink DefaultOILP = OnlineInboundAdapterLink.Build("");
        static readonly AnchorState DefaultState = AnchorState.Active;
        static readonly Color DefaultColor = Color.White;

        Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans);

        Task<Layer> UpsertLayer(string id, string description, Color color, AnchorState state, string clConfigID, OnlineInboundAdapterLink oilp, string[] generators, IModelContext trans);
        Task<bool> TryToDelete(string id, IModelContext trans);

        Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold);
        Task<Layer?> GetLayer(string layerID, IModelContext trans, TimeThreshold timeThreshold);
    }

    public static class LayerModelExtensions
    {
        public static async Task<Layer> UpsertLayer(this ILayerModel layerModel, string id, IModelContext trans)
        {
            return await layerModel.UpsertLayer(id, "", ILayerModel.DefaultColor, ILayerModel.DefaultState, "", ILayerModel.DefaultOILP, System.Array.Empty<string>(), trans);
        }

        public static async Task<IEnumerable<Layer>> GetLayers(this ILayerModel layerModel, IEnumerable<string> layerIDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (layerIDs.IsEmpty()) return new List<Layer>();

            IDValidations.ValidateLayerIDsThrow(layerIDs);

            var layers = (await layerModel.GetLayers(trans, timeThreshold)).ToList();

            // HACK, TODO: wonky re-sorting of layers according to input layerIDs
            return layerIDs.Select(id => layers.Find(l => l.ID == id)).WhereNotNull();
        }

        public static async Task<IEnumerable<Layer>> GetLayers(this ILayerModel layerModel, AnchorStateFilter stateFilter, IModelContext trans, TimeThreshold timeThreshold)
        {
            var layers = await layerModel.GetLayers(trans, timeThreshold);

            return layers.Where(layer => stateFilter.Contains(layer.State));
        }
    }
}
