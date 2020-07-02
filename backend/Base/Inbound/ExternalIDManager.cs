using Landscape.Base.Model;
using Landscape.Base.Utils;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Landscape.Base.Inbound
{
    /// <summary>
    /// the purpose of this manager is 
    /// a) to ensure that the external CIs exist also internally and have a proper CIID
    /// b) keep the mapping table between internal and external IDs up-to-date
    /// </summary>
    public abstract class ExternalIDManager : IExternalIDManager
    {
        private readonly ScopedExternalIDMapper mapper;

        public TimeSpan PreferredUpdateRate { get; }

        public ExternalIDManager(ScopedExternalIDMapper mapper, TimeSpan preferredUpdateRate)
        {
            this.mapper = mapper;
            PreferredUpdateRate = preferredUpdateRate;
        }

        protected abstract Task<IEnumerable<IExternalItem>> GetExternalItems();

        public async Task Update(ICIModel ciModel, NpgsqlConnection conn, ILogger logger)
        {
            await mapper.Setup();

            var externalItems = await GetExternalItems();

            var changes = false;

            // remove all mappings that do not have an external item (anymore)
            var removedExternalIDs = mapper.RemoveAllExceptExternalIDs(externalItems.Select(ei => ei.ID));
            if (!removedExternalIDs.IsEmpty())
            {
                logger.LogInformation("Removed the folloing external IDs from mapping: ");
                logger.LogInformation(string.Join(", ", removedExternalIDs));

                changes = true;
            }

            using var trans = conn.BeginTransaction();

            // add any (new) CIs that don't exist yet, and add to mapper
            foreach (var externalItem in externalItems)
            {
                var externalID = externalItem.ID;
                if (!mapper.ExistsInternally(externalID))
                {
                    logger.LogInformation($"CI with external ID {externalID} does not exist internally, creating...");
                    var ciid = await ciModel.CreateCI(trans);
                    logger.LogInformation($"Created CI with CIID {ciid}");
                    /**
                     * TODO: actually, we should check first if an existing CI may already be a fitting candidate
                     * this would probably be a similar process as when identifying CIs when doing an ingest 
                     **/
                    mapper.Add(ciid, externalID);

                    changes = true;
                }
            }

            // ensure that all CIs that have a mapping actually exist internally
            var existingCIIDs = await ciModel.GetCIIDs(trans);
            var missingCIIDs = mapper.GetAllCIIDs().Except(existingCIIDs);
            foreach (var missingCIID in missingCIIDs)
            {
                logger.LogInformation($"CI with existing mapping to internal CIID {missingCIID} does not exist internally, creating...");
                await ciModel.CreateCI(trans, missingCIID);
                logger.LogInformation($"Created CI with CIID {missingCIID}");

                changes = true;
            }

            if (changes)
            {
                trans.Commit();
                await mapper.Persist();
            } else
            {
                trans.Rollback();
            }
        }
    }

}
