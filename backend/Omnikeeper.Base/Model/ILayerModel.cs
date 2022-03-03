using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
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
        Task<(Layer layer, bool created)> CreateLayerIfNotExists(string id, IModelContext trans);
        Task<bool> TryToDelete(string id, IModelContext trans);

        Task<IEnumerable<Layer>> GetLayers(IModelContext trans);
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
            return layerData.Values.Where(ld =>
            {
                return stateFilter.Contains(Enum.Parse<AnchorState>(ld.State));
            });
        }
    }

    public static class LayerModelExtensions
    {
        public static async Task<Layer?> GetLayer(this ILayerModel layerModel, string id, IModelContext trans)
        {
            IDValidations.ValidateLayerIDThrow(id);

            var layers = (await layerModel.GetLayers(trans)).ToDictionary(l => l.ID);
            return layers.GetOrWithClass(id, null);
        }

        public static async Task<IEnumerable<Layer>> GetLayers(this ILayerModel layerModel, IEnumerable<string> layerIDs, IModelContext trans)
        {
            if (layerIDs.IsEmpty()) return new List<Layer>();

            IDValidations.ValidateLayerIDsThrow(layerIDs);

            var layers = (await layerModel.GetLayers(trans)).ToList();

            // HACK, TODO: wonky re-sorting of layers according to input layerIDs
            return layerIDs.Select(id => layers.Find(l => l.ID == id)).WhereNotNull();
        }

        public static async Task<LayerSet> BuildLayerSet(this ILayerModel layerModel, string[] ids, IModelContext trans)
        {
            IDValidations.ValidateLayerIDsThrow(ids);

            if (ids.Length == 0)
                return new LayerSet(); // empty layerset
            if (ids.Length == 1)
            {
                var layer = await layerModel.GetLayer(ids[0], trans);
                if (layer == null)
                    throw new Exception(@$"Could not find layer with ID ""{ids[0]}""");
                else
                    return new LayerSet(ids[0]);
            }
            else
            {
                var layers = await layerModel.GetLayers(ids, trans);
                if (layers.Count() < ids.Length)
                {
                    var notFound = ids.Except(layers.Select(l => l.ID));
                    throw new Exception(@$"Could not find layers with IDs ""{string.Join(",", notFound)}""");
                }
                else
                    return new LayerSet(ids);
            }
        }
    }
}
