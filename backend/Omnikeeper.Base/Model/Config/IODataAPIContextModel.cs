using Npgsql;
using Omnikeeper.Base.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface IODataAPIContextModel
    {
        Task<IEnumerable<ODataAPIContext>> GetContexts(NpgsqlTransaction trans);
        Task<ODataAPIContext> GetContextByID(string id, NpgsqlTransaction trans);
        Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, NpgsqlTransaction trans);
        Task<ODataAPIContext> Delete(string id, NpgsqlTransaction trans);
    }
}
