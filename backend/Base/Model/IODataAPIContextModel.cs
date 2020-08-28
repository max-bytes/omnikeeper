using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IODataAPIContextModel
    {
        Task<IEnumerable<ODataAPIContext>> GetContexts(NpgsqlTransaction trans);
        Task<ODataAPIContext> GetContextByID(string id, NpgsqlTransaction trans);
        Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, NpgsqlTransaction trans);
        Task<ODataAPIContext> Delete(string id, NpgsqlTransaction transaction);

        Task<LayerSet> GetReadLayersetFromContext(string contextID, NpgsqlTransaction trans);
        Task<long> GetWriteLayerIDFromContext(string contextID, NpgsqlTransaction trans);
    }
}
