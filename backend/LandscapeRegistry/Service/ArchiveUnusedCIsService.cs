using Landscape.Base.Inbound;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Service
{
    // TODO: make non-static and use interface
    public static class ArchiveUnusedCIsService
    {
        public static async Task<int> ArchiveUnusedCIs(IExternalIDMapPersister externalIDMapPersister, NpgsqlConnection conn, ILogger logger)
        {
            // prefetch a list of CIIDs that do not have any attributes nor any relations (also historic)
            var unusedCIIDs = new HashSet<Guid>();
            var queryUnusedCIIDs = @"SELECT id FROM ci ci WHERE
                NOT EXISTS (SELECT 1 FROM attribute a WHERE a.ci_id = ci.id) AND 
                NOT EXISTS (SELECT 1 FROM relation r WHERE r.from_ci_id = ci.id OR r.to_ci_id = ci.id)";
            using (var commandUnusedCIIDs = new NpgsqlCommand(queryUnusedCIIDs, conn, null))
            {
                using var s = await commandUnusedCIIDs.ExecuteReaderAsync();
                while (await s.ReadAsync())
                    unusedCIIDs.Add(s.GetGuid(0));
            }

            // NOTE: we check for the existence of the CIIDs in foreign data mapping tables here.
            // We could also just rely on the foreign key constraints of the database, so that it does not let us delete CIs that are still in use.
            // But that is hacky and could lead to disaster -> better be safe
            // also, trying to delete CIs with active foreign key constraints adds a lot of unneccessary exception output to the console
            var mappedCIIDs = await externalIDMapPersister.GetAllMappedCIIDs(conn, null);
            unusedCIIDs = unusedCIIDs.Except(mappedCIIDs).ToHashSet();

            var deleted = 0;
            if (unusedCIIDs.Count > 0)
            {
                using var commandDelete = new NpgsqlCommand(@"DELETE FROM ci WHERE id = @id RETURNING *", conn, null);
                commandDelete.Parameters.Add("id", NpgsqlDbType.Uuid);
                foreach (var ciid in unusedCIIDs)
                {
                    try
                    {

                        using var trans = conn.BeginTransaction();
                        commandDelete.Parameters[0].Value = ciid;
                        var d = await commandDelete.ExecuteScalarAsync();
                        deleted++;
                        trans.Commit();
                    }
                    catch (PostgresException e)
                    {
                        logger.LogWarning(e, $"Could not delete unused CI \"{ciid}\"");
                    }
                }
            }

            return deleted;
        }
    }
}
