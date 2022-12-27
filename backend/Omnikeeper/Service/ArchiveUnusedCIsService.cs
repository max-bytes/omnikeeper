using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Service
{
    // TODO: make non-static and use interface
    public static class ArchiveUnusedCIsService
    {
        public static async Task<int> ArchiveUnusedCIs(IModelContextBuilder modelContextBuilder, ILogger logger)
        {
            var trans = modelContextBuilder.BuildImmediate();

            // prefetch a list of CIIDs that do not have any attributes nor any relations (also historic)
            var unusedCIIDs = new HashSet<Guid>();
            var queryUnusedCIIDs = @"select id from ci ci WHERE 
	            ci.id not in (select distinct ci_id from attribute) and
	            ci.id not in (select distinct to_ci_id from relation) and
	            ci.id not in (select distinct from_ci_id from relation)";
            using (var commandUnusedCIIDs = new NpgsqlCommand(queryUnusedCIIDs, trans.DBConnection, null))
            {
                using var s = await commandUnusedCIIDs.ExecuteReaderAsync();
                while (await s.ReadAsync())
                    unusedCIIDs.Add(s.GetGuid(0));
            }

            var deleted = 0;
            if (unusedCIIDs.Count > 0)
            {
                // try to delete in bulk
                try
                {
                    using var transD = modelContextBuilder.BuildDeferred();
                    // postgres queries in the form " = ANY(@ciids)" are slow when the list of ciids is large
                    // therefore we use a CTE instead and join that
                    // see here for a discussion about the options https://stackoverflow.com/questions/17037508/sql-when-it-comes-to-not-in-and-not-equal-to-which-is-more-efficient-and-why/17038097#17038097
                    var query = @$"
                        WITH to_delete(ci_id) AS (VALUES 
                        {string.Join(",", unusedCIIDs.Select(ciid => $"('{ciid}'::uuid)"))}
                        )
                        DELETE FROM ci c
                        USING to_delete t
                        WHERE t.ci_id = c.id";
                    using var commandDeleteBulk = new NpgsqlCommand(query, trans.DBConnection, trans.DBTransaction);

                    var d = await commandDeleteBulk.ExecuteNonQueryAsync();
                    deleted = d;
                    transD.Commit();
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, $"Could not delete unused CIs in bulk");

                    // TODO: should we try to delete CIs one-by-one as a fallback?
                    // example code:
                    //using var commandDelete = new NpgsqlCommand(@"DELETE FROM ci WHERE id = @id RETURNING *", trans.DBConnection, null);
                    //commandDelete.Parameters.Add("id", NpgsqlDbType.Uuid);
                    //foreach (var ciid in unusedCIIDs)
                    //{
                    //    try
                    //    {
                    //        using var transD = modelContextBuilder.BuildDeferred();
                    //        commandDelete.Parameters[0].Value = ciid;
                    //        var d = await commandDelete.ExecuteScalarAsync();
                    //        deleted++;
                    //        transD.Commit();
                    //    }
                    //    catch (PostgresException e)
                    //    {
                    //        logger.LogWarning(e, $"Could not delete unused CI \"{ciid}\"");
                    //    }
                    //}
                }

            }

            return deleted;
        }
    }
}
