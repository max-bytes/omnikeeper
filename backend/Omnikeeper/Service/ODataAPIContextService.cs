using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public static class ODataAPIContextService
    {
        public static async Task<LayerSet> GetReadLayersetFromContext(IODataAPIContextModel model, string contextID, IModelContext trans)
        {
            var context = await model.GetContextByID(contextID, trans);
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV3 v3 => new LayerSet(v3.ReadLayerset),
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
        public static async Task<string> GetWriteLayerIDFromContext(IODataAPIContextModel model, string contextID, IModelContext trans)
        {
            var context = await model.GetContextByID(contextID, trans);
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV3 v3 => v3.WriteLayerID,
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
    }
}
