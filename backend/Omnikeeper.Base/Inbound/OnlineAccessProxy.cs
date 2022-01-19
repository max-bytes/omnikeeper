using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    public class OnlineAccessProxy : IOnlineAccessProxy
    {
        private readonly ILayerDataModel layerDataModel;
        private readonly IInboundAdapterManager pluginManager;

        public OnlineAccessProxy(ILayerDataModel layerDataModel, IInboundAdapterManager pluginManager)
        {
            this.layerDataModel = layerDataModel;
            this.pluginManager = pluginManager;
        }

        public async Task<bool> IsOnlineInboundLayer(string layerID, IModelContext trans)
        {
            var layerData = await layerDataModel.GetLayerData(layerID, trans, TimeThreshold.BuildLatest());
            if (layerData == null) return false;
            var adapterName = layerData.OIAReference;
            return await pluginManager.IsValidOnlinePluginInstance(adapterName, trans);
        }

        public async Task<bool> ContainsOnlineInboundLayer(LayerSet layerset, IModelContext trans)
        {
            var layerData = await layerDataModel.GetLayerData(trans, TimeThreshold.BuildLatest());
            foreach (var layerID in layerset)
            {
                if (layerData.TryGetValue(layerID, out var ld))
                {
                    var adapterName = ld.OIAReference;
                    if (await pluginManager.IsValidOnlinePluginInstance(adapterName, trans))
                        return true;
                }
            }
            return false;
        }

        private async IAsyncEnumerable<(ILayerAccessProxy? proxy, int index)> GetAccessProxies(string[] layerIDs, IModelContext trans)
        {
            var i = 0;
            var layerData = await layerDataModel.GetLayerData(trans, TimeThreshold.BuildLatest());
            foreach (var layerID in layerIDs)
            {
                if (layerData.TryGetValue(layerID, out var ld))
                {
                    //await layerModel.GetLayers(layerIDs, trans, TimeThreshold.BuildLatest()))
                    var plugin = await pluginManager.GetOnlinePluginInstance(ld.OIAReference, trans);
                    if (plugin != null)
                    {
                        var layer = Layer.Build(ld.LayerID); // HACK: we shouldn't create a layer object here
                        yield return (plugin.CreateLayerAccessProxy(layer), i++);
                    }
                    else
                    {
                        yield return (null, i++);
                    }
                } else
                {
                    yield return (null, i++);
                }
            }
        }

        public async Task<IEnumerable<CIAttribute>[]> GetAttributes(ICIIDSelection selection, string[] layerIDs, IModelContext trans, TimeThreshold atTime, IAttributeSelection attributeSelection)
        {
            var ret = new IEnumerable<CIAttribute>[layerIDs.Length];
            await foreach (var (proxy, index) in GetAccessProxies(layerIDs, trans))
            {
                if (proxy != null)
                {
                    var attributes = proxy.GetAttributes(selection, atTime, attributeSelection).ToEnumerable();
                    ret[index] = attributes;
                } else
                {
                    ret[index] = Array.Empty<CIAttribute>();
                }
            }
            return ret;
        }

        public async Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layerData = await layerDataModel.GetLayerData(layerID, trans, atTime);
            if (layerData == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layerData.OIAReference, trans);
            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layerData.OIAReference}");
            var layer = Layer.Build(layerData.LayerID); // HACK: we shouldn't create a layer object here
            return await plugin.CreateLayerAccessProxy(layer).GetRelation(fromCIID, toCIID, predicateID, atTime);
        }

        public async IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, string layerID, IModelContext trans, TimeThreshold atTime)
        {
            var layerData = await layerDataModel.GetLayerData(layerID, trans, atTime);
            if (layerData == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layerData.OIAReference, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layerData.OIAReference}");
            var layer = Layer.Build(layerData.LayerID); // HACK: we shouldn't create a layer object here
            await foreach (var relation in plugin.CreateLayerAccessProxy(layer).GetRelations(rl, atTime))
                yield return relation;
        }

        public async Task<CIAttribute?> GetFullBinaryAttribute(string name, string layerID, Guid ciid, IModelContext trans, TimeThreshold atTime)
        {
            var layerData = await layerDataModel.GetLayerData(layerID, trans, atTime);
            if (layerData == null)
                throw new Exception($"Could not find layer with ID {layerID}");
            var plugin = await pluginManager.GetOnlinePluginInstance(layerData.OIAReference, trans);

            if (plugin == null)
                throw new Exception($"Could not load plugin instance {layerData.OIAReference}");
            var layer = Layer.Build(layerData.LayerID); // HACK: we shouldn't create a layer object here
            return await plugin.CreateLayerAccessProxy(layer).GetFullBinaryAttribute(name, ciid, atTime);
        }
    }
}
