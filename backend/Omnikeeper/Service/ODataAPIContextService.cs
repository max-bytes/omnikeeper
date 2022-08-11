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
                ODataAPIContext.ConfigV4 v4 => new LayerSet(v4.ReadLayerset),
                _ => throw new Exception("Invalid OData API context config"),
            };
        }

        public static async Task<ODataAPIContext.IContextAuth> GetAuthConfigFromContext(ODataAPIContextModel model, IMetaConfigurationModel metaConfigurationModel, string contextID, IModelContext trans, TimeThreshold timeThreshold)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var (context, _) = await model.GetSingleByDataID(contextID, metaConfiguration.ConfigLayerset, trans, timeThreshold);
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV4 v4 => v4.ContextAuth,
                _ => throw new Exception("Invalid OData API context config"),
            };
        }

        public static async Task<string> GetWriteLayerIDFromContext(ODataAPIContextModel model, IMetaConfigurationModel metaConfigurationModel, string contextID, IModelContext trans, TimeThreshold timeThreshold)
        {
            var metaConfiguration = await metaConfigurationModel.GetConfigOrDefault(trans);
            var (context, _) = await model.GetSingleByDataID(contextID, metaConfiguration.ConfigLayerset, trans, timeThreshold);
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV4 v4 => v4.WriteLayerID,
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
    }
}
