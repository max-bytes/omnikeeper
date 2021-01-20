using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseRelationRevisionistModel : IBaseRelationRevisionistModel
    {
        public async Task<int> DeleteAllRelations(long layerID, IModelContext trans)
        {
            var query = @"delete from relation r where r.layer_id = @layer_id";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }
    }
}
