using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseAttributeRevisionistModel : IBaseAttributeRevisionistModel
    {
        public async Task<int> DeleteAllAttributes(long layerID, IModelContext trans)
        {
            var query = @"delete from attribute a where a.layer_id = @layer_id";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);
            command.Parameters.AddWithValue("layer_id", layerID);
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }
    }
}
