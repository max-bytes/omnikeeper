using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class CIIDModel : ICIIDModel
    {
        // TODO: caching
        public async Task<IReadOnlySet<Guid>> GetCIIDs(IModelContext trans)
        {
            using var _ = await trans.WaitAsync();
            using var command = new NpgsqlCommand(@"select id from ci", trans.DBConnection, trans.DBTransaction);
            command.Prepare();
            var tmp = new HashSet<Guid>();
            using var s = await command.ExecuteReaderAsync();
            while (await s.ReadAsync())
                tmp.Add(s.GetGuid(0));
            return tmp;
        }

        public async Task<bool> CIIDExists(Guid id, IModelContext trans)
        {
            using var _ = await trans.WaitAsync();
            using var command = new NpgsqlCommand(@"select id from ci WHERE id = @ciid LIMIT 1", trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("ciid", id);
            command.Prepare();

            using var dr = await command.ExecuteReaderAsync();

            if (!await dr.ReadAsync())
                return false;
            return true;
        }
    }
}
