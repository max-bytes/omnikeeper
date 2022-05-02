using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.Config;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Model.Config;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public static class ODataAPIContextService
    {
        public static async Task<LayerSet> GetReadLayersetFromContext(ODataAPIContextModel model, IMetaConfigurationModel metaConfigurationModel, string contextID, IModelContext trans, TimeThreshold timeThreshold)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var (context, _) = await model.GetSingleByDataID(contextID, metaConfiguration.ConfigLayerset, trans, timeThreshold);
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV3 v3 => new LayerSet(v3.ReadLayerset),
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
        public static async Task<string> GetWriteLayerIDFromContext(ODataAPIContextModel model, IMetaConfigurationModel metaConfigurationModel, string contextID, IModelContext trans, TimeThreshold timeThreshold)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var (context, _) = await model.GetSingleByDataID(contextID, metaConfiguration.ConfigLayerset, trans, timeThreshold);
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV3 v3 => v3.WriteLayerID,
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
    }
}
