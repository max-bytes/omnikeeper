using Landscape.Base.Model;
using LandscapePrototype.Entity;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapePrototype.Model.Cached
{
    public class CachedLayerModel : ILayerModel
    {
        private ILayerModel Model { get; }

        private readonly Dictionary<long, Layer> IDLayerCache = new Dictionary<long, Layer>();
        private readonly Dictionary<string, Layer> NameLayerCache = new Dictionary<string, Layer>();
        private IEnumerable<Layer> AllLayersHash = null;

        public CachedLayerModel(ILayerModel model)
        {
            Model = model;
        }

        private Layer AddToCache(Layer l)
        {
            IDLayerCache.Add(l.ID, l);
            NameLayerCache.Add(l.Name, l);
            return l;
        }

        public async Task<LayerSet> BuildLayerSet(string[] layerNames, NpgsqlTransaction trans) => await Model.BuildLayerSet(layerNames, trans);
        public async Task<LayerSet> BuildLayerSet(NpgsqlTransaction trans) => await Model.BuildLayerSet(trans);

        public async Task<Layer> GetLayer(long layerID, NpgsqlTransaction trans)
        {
            IDLayerCache.TryGetValue(layerID, out var ret);
            return ret ?? AddToCache(await Model.GetLayer(layerID, trans));
        }

        public async Task<Layer> GetLayer(string layerName, NpgsqlTransaction trans)
        {
            NameLayerCache.TryGetValue(layerName, out var ret);
            return ret ?? AddToCache(await Model.GetLayer(layerName, trans));
        }

        public async Task<IEnumerable<Layer>> GetLayers(long[] layerIDs, NpgsqlTransaction trans)
        {
            var tmp = layerIDs.Select(id => {
                IDLayerCache.TryGetValue(id, out var ret);
                return (id, ret);
            });
            // TODO: probably not the most performant implementation
            var notInCache = await Model.GetLayers(tmp.Where(t => t.ret == null).Select(t => t.id).ToArray(), trans);
            return tmp.Select(t => t.ret ?? AddToCache(notInCache.FirstOrDefault(l => l.ID == t.id)));
        }

        public async Task<IEnumerable<Layer>> GetLayers(NpgsqlTransaction trans)
        {
            if (AllLayersHash == null)
            {
                AllLayersHash = await Model.GetLayers(trans);
            }
            return AllLayersHash;
        }
    }
}
