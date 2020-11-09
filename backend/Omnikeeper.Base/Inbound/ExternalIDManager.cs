using Microsoft.Extensions.Logging;
using Npgsql;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Inbound
{
    /// <summary>
    /// the purpose of this manager is 
    /// a) to ensure that the external CIs exist also internally and have a proper CIID
    /// b) keep the mapping table between internal and external IDs up-to-date
    /// </summary>
    public abstract class ExternalIDManager<EID> : IExternalIDManager where EID : struct, IExternalID
    {
        private readonly ScopedExternalIDMapper<EID> mapper;

        public TimeSpan PreferredUpdateRate { get; }
        public string PersisterScope => mapper.PersisterScope;

        public ExternalIDManager(ScopedExternalIDMapper<EID> mapper, TimeSpan preferredUpdateRate)
        {
            this.mapper = mapper;
            PreferredUpdateRate = preferredUpdateRate;
        }

        protected abstract Task<IEnumerable<(EID externalID, ICIIdentificationMethod idMethod)>> GetExternalIDs();

        public async Task<bool> Update(ICIModel ciModel, IAttributeModel attributeModel, CIMappingService ciMappingService, NpgsqlConnection conn, NpgsqlTransaction trans, ILogger logger)
        {
            var externalIDs = await GetExternalIDs();

            var changes = false;

            // remove all mappings that do not have an external item (anymore) // TODO: make this configurable, there might be data sources that don't want that
            var removedExternalIDs = mapper.RemoveAllExceptExternalIDs(externalIDs.Select(t => t.externalID));
            if (!removedExternalIDs.IsEmpty())
            {
                logger.LogInformation("Removed the following external IDs from mapping: ");
                logger.LogInformation(string.Join(", ", removedExternalIDs));

                changes = true;
            }

            // add any (new) CIs that don't exist yet, and add to mapper
            var ciMappingContext = new CIMappingService.CIMappingContext(attributeModel, TimeThreshold.BuildLatest());
            foreach (var (externalID, idMethod) in externalIDs)
            {
                if (!mapper.ExistsInternally(externalID))
                {
                    logger.LogInformation($"CI with external ID {externalID} does not exist internally, creating new OR mapping to existing...");

                    var foundCIIDs = await ciMappingService.TryToMatch(externalID.SerializeToString(), idMethod, ciMappingContext, trans, logger);

                    Guid ciid;
                    if (!foundCIIDs.IsEmpty())
                    {
                        // we choose a CIID that is not already mapped
                        // TODO, NOTE: this is still dependent on the order in which the found CIIDs are returned
                        var chosenCIID = foundCIIDs.FirstOrDefault(foundCIID => !mapper.ExistsExternally(foundCIID));

                        if (chosenCIID == default)
                        {
                            // despite having suitable CIs that would work, we can't use them because they already map to other external items... we must create a new CI
                            logger.LogWarning($"Cannot map to existing CI because - even though CIs would match - they are all already mapped to other external items");
                            ciid = await ciModel.CreateCI(trans); // creating new CI with new CIID
                            logger.LogInformation($"Created new CI with CIID {ciid}");
                        }
                        else
                        {
                            if (await ciModel.CIIDExists(chosenCIID, trans))
                            { // TODO: performance improvements, do not check every ci separately
                                ciid = chosenCIID;
                                logger.LogInformation($"Mapping to existing CI with CIID {ciid}");
                            }
                            else
                            {
                                ciid = await ciModel.CreateCI(chosenCIID, trans);
                                logger.LogInformation($"Created new CI with CIID {ciid}");
                            }
                        }
                    }
                    else
                    {
                        ciid = await ciModel.CreateCI(trans); // creating new CI with new CIID
                        logger.LogInformation($"Created new CI with CIID {ciid}");
                    }

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
                await ciModel.CreateCI(missingCIID, trans);
                logger.LogInformation($"Created CI with CIID {missingCIID}");

                changes = true;
            }

            if (changes)
                await mapper.Persist(conn, trans); // TODO: handle case when persisting fails... keep trying? or revert whole ID finding process?

            return changes;
        }
    }

}
