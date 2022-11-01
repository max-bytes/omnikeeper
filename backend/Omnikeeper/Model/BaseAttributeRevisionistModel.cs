using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class BaseAttributeRevisionistModel : IBaseAttributeRevisionistModel
    {
        public async Task<int> DeleteAllAttributes(ICIIDSelection ciidSelection, string layerID, IModelContext trans)
        {

            string CIIDSelection2WhereClause(ICIIDSelection selection)
            {
                return selection switch
                {
                    AllCIIDsSelection _ => "1=1",
                    SpecificCIIDsSelection _ => "(a.ci_id = ANY(@ciids))",
                    AllCIIDsExceptSelection _ => "(NOT a.ci_id = ANY(@ciids))",
                    NoCIIDsSelection _ => "1=0",
                    _ => throw new NotImplementedException()
                };
            }
            IEnumerable<NpgsqlParameter> CIIDSelection2Parameters(ICIIDSelection selection)
            {
                switch (selection)
                {
                    case AllCIIDsSelection _:
                        break;
                    case NoCIIDsSelection _:
                        break;
                    case SpecificCIIDsSelection n:
                        yield return new NpgsqlParameter("ciids", n.CIIDs.ToArray());
                        break;
                    case AllCIIDsExceptSelection n:
                        yield return new NpgsqlParameter("ciids", n.ExceptCIIDs.ToArray());
                        break;
                    default:
                        throw new NotImplementedException();
                };
            }
            bool ShouldRunPrepare(ICIIDSelection selection)
            {
                return selection switch
                {
                    AllCIIDsSelection _ => true,
                    SpecificCIIDsSelection _ => false,
                    AllCIIDsExceptSelection _ => false,
                    NoCIIDsSelection _ => true,
                    _ => throw new NotImplementedException()
                };
            }

            using var _ = await trans.WaitAsync();
            using var commandLatest = new NpgsqlCommand(@$"delete from attribute_latest a where a.layer_id = @layer_id and {CIIDSelection2WhereClause(ciidSelection)}", trans.DBConnection, trans.DBTransaction);
            commandLatest.Parameters.AddWithValue("layer_id", layerID);
            foreach (var p in CIIDSelection2Parameters(ciidSelection)) commandLatest.Parameters.Add(p);
            if (ShouldRunPrepare(ciidSelection))
                commandLatest.Prepare();
            await commandLatest.ExecuteNonQueryAsync();

            using var commandHistoric = new NpgsqlCommand(@$"delete from attribute a where a.layer_id = @layer_id and {CIIDSelection2WhereClause(ciidSelection)}", trans.DBConnection, trans.DBTransaction);
            commandHistoric.Parameters.AddWithValue("layer_id", layerID);
            foreach (var p in CIIDSelection2Parameters(ciidSelection)) commandHistoric.Parameters.Add(p);
            if (ShouldRunPrepare(ciidSelection))
                commandHistoric.Prepare();
            var numDeleted = await commandHistoric.ExecuteNonQueryAsync();

            return numDeleted;
        }

        public async Task<int> DeleteOutdatedAttributesOlderThan(string[] layerIDs, IModelContext trans, DateTimeOffset threshold, TimeThreshold atTime)
        {
            using var _ = await trans.WaitAsync();

            // query inspired by https://stackoverflow.com/questions/15959061/delete-records-which-do-not-have-a-match-in-another-table
            var query = @"DELETE FROM attribute a
                    USING (SELECT a2.id FROM attribute a2 WHERE a2.timestamp < @delete_threshold AND a2.layer_id = ANY(@layer_ids)
                        AND NOT EXISTS(
                        SELECT 1 FROM attribute_latest l WHERE l.id = a2.id
                    )) i
	                WHERE i.id = a.id";

            using var command = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);

            var now = TimeThreshold.BuildLatest();
            command.Parameters.AddWithValue("delete_threshold", threshold.ToUniversalTime());
            command.Parameters.AddWithValue("now", atTime.Time.ToUniversalTime());
            command.Parameters.AddWithValue("layer_ids", layerIDs);
            command.Prepare();

            var numDeleted = await command.ExecuteNonQueryAsync();

            return numDeleted;
        }
    }
}
