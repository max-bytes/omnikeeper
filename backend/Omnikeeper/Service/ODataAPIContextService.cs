using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    public static class ODataAPIContextService
    {
        public static async Task<LayerSet> GetReadLayersetFromContext(IODataAPIContextModel model, string contextID, NpgsqlTransaction trans)
        {
            var context = await model.GetContextByID(contextID, trans);
            if (context == null) throw new Exception($"Invalid context ID \"{contextID}\"");
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV3 v3 => new LayerSet(v3.ReadLayerset),
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
        public static async Task<long> GetWriteLayerIDFromContext(IODataAPIContextModel model, string contextID, NpgsqlTransaction trans)
        {
            var context = await model.GetContextByID(contextID, trans);
            if (context == null) throw new Exception($"Invalid context ID \"{contextID}\"");
            return context.CConfig switch
            {
                ODataAPIContext.ConfigV3 v3 => v3.WriteLayerID,
                _ => throw new Exception("Invalid OData API context config"),
            };
        }
    }
}
