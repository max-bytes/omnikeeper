using Npgsql;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class ChangesetStatisticsModel : IChangesetStatisticsModel
    {
        public async Task<ChangesetStatistics> GetStatistics(Guid changesetID, IModelContext trans)
        {
            using var command = new NpgsqlCommand($@"
                SELECT 
                    (SELECT count(*) FROM attribute a WHERE a.changeset_id = @changeset_id) as num_attribute_changes, 
                    (SELECT count(*) FROM relation r WHERE r.changeset_id = @changeset_id) as num_relation_changes
            ", trans.DBConnection, trans.DBTransaction);

            command.Parameters.AddWithValue("changeset_id", changesetID);

            using var s = await command.ExecuteReaderAsync();

            if (!s.Read())
                throw new Exception("Error fetching changeset statistics");

            var numAttributeChanges = s.GetInt64(0);
            var numRelationChanges = s.GetInt64(1);

            return new ChangesetStatistics(changesetID, numAttributeChanges, numRelationChanges);
        }
    }
}
