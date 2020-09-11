using Landscape.Base.Entity;
using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Landscape.Base.Model
{
    public interface IODataAPIContextModel
    {
        Task<IEnumerable<ODataAPIContext>> GetContexts(NpgsqlTransaction trans);
        Task<ODataAPIContext> GetContextByID(string id, NpgsqlTransaction trans);
        Task<ODataAPIContext> Upsert(string id, ODataAPIContext.IConfig config, NpgsqlTransaction trans);
        Task<ODataAPIContext> Delete(string id, NpgsqlTransaction trans);
    }
}
