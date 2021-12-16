using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ILayerModel
    {
        Task<LayerSet> BuildLayerSet(string[] ids, IModelContext trans);

        Task<(Layer layer, bool created)> CreateLayerIfNotExists(string id, IModelContext trans);
        Task<bool> TryToDelete(string id, IModelContext trans);

        Task<IEnumerable<Layer>> GetLayers(IModelContext trans, TimeThreshold timeThreshold);
        Task<Layer?> GetLayer(string layerID, IModelContext trans, TimeThreshold timeThreshold);
    }

    public interface ILayerDataModel
    {
        static readonly AnchorState DefaultState = AnchorState.Active;
        static readonly Color DefaultColor = Color.White;

        Task<IDictionary<string, LayerData>> GetLayerData(IModelContext trans, TimeThreshold timeThreshold);

        Task<(LayerData layerData, bool changed)> UpsertLayerData(string id, string description, long color, string state, string clConfigID, string oiaReference, string[] generators, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
        Task<bool> TryToDelete(string id, DataOriginV1 dataOrigin, IChangesetProxy changesetProxy, IModelContext trans);
    }

    public static class LayerDataModelExtensions
    {
        public static async Task<LayerData?> GetLayerData(this ILayerDataModel layerDataModel, string layerID, IModelContext trans, TimeThreshold timeThreshold)
        {
            IDValidations.ValidateLayerIDThrow(layerID);

            var layerData = await layerDataModel.GetLayerData(trans, timeThreshold);

            return layerData.GetOrWithClass(layerID, null);
        }


        public static async Task<IEnumerable<LayerData>> GetLayerData(this ILayerDataModel layerDataModel, AnchorStateFilter stateFilter, IModelContext trans, TimeThreshold timeThreshold)
        {
            var layerData = await layerDataModel.GetLayerData(trans, timeThreshold);
            return layerData.Values.Where(ld => {
                return stateFilter.Contains(Enum.Parse<AnchorState>(ld.State));
            });
        }
    }

    public static class LayerModelExtensions
    {
        public static async Task<IEnumerable<Layer>> GetLayers(this ILayerModel layerModel, IEnumerable<string> layerIDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (layerIDs.IsEmpty()) return new List<Layer>();

            IDValidations.ValidateLayerIDsThrow(layerIDs);

            var layers = (await layerModel.GetLayers(trans, timeThreshold)).ToList();

            // HACK, TODO: wonky re-sorting of layers according to input layerIDs
            return layerIDs.Select(id => layers.Find(l => l.ID == id)).WhereNotNull();
        }
    }
}
