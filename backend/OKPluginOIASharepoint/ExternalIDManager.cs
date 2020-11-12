
using DotLiquid.Tags;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OKPluginOIASharepoint.Config;

namespace OKPluginOIASharepoint
{
    public class ExternalIDManager : ExternalIDManager<SharepointExternalListItemID>
    {
        private readonly SharepointClient client;
        private readonly IDictionary<Guid, CachedListConfig> cachedListConfigs;
        private readonly ILogger logger;

        public ExternalIDManager(IDictionary<Guid, CachedListConfig> cachedListConfigs, SharepointClient client, ScopedExternalIDMapper mapper, TimeSpan preferredUpdateRate, ILogger logger) : base(mapper, preferredUpdateRate)
        {
            this.client = client;
            this.cachedListConfigs = cachedListConfigs;
            this.logger = logger;
        }

        protected override async Task<IEnumerable<(SharepointExternalListItemID externalID, ICIIdentificationMethod idMethod)>> GetExternalIDs()
        {
            var r = new List<(SharepointExternalListItemID, ICIIdentificationMethod)>();
            foreach (var lc in cachedListConfigs.Values)
            {
                try
                {
                    var l = client.GetListItems(lc.listID, lc.identifiableColumnsAttributeTuples.Select(t => t.columnName).ToArray());

                    await foreach(var (itemGuid, data) in l)
                    {
                        var identifiableFragments = new List<CICandidateAttributeData.Fragment>();
                        foreach (var (columnName, attributeName) in lc.identifiableColumnsAttributeTuples)
                        {
                            if (!((IDictionary<string, object>)data).TryGetValue(columnName, out var value))
                            {
                                logger.LogWarning($"Could not get (supposedly identifiable) column \"{columnName}\" in list \"{lc.listID}\"");
                                continue;
                            }
                            if (!(value is string valueStr))
                            {
                                logger.LogWarning($"Value of column \"{columnName}\" in list \"{lc.listID}\" cannot be converted to string");
                                continue;
                            }

                            identifiableFragments.Add(new CICandidateAttributeData.Fragment(attributeName, new AttributeScalarValueText(value as string)));
                        }

                        ICIIdentificationMethod idMethod = CIIdentificationMethodNoop.Build();
                        if (identifiableFragments.IsEmpty())
                        {
                            logger.LogWarning($"No identifiable columns/fragments found in list \"{lc.listID}\"");
                        }
                        else
                        {
                            idMethod = CIIdentificationMethodByData.BuildFromFragments(identifiableFragments, lc.searchableLayerSet);
                        }

                        r.Add((new SharepointExternalListItemID(lc.listID, itemGuid), idMethod));
                    }

                } catch (Exception e)
                {
                    logger.LogWarning($"Unable to get external IDs for sharepoint list {lc.listID}", e);
                    throw e; // we must fail and throw, so that we don't run the ExternalIDManager with an empty set of external IDs
                }
            }
            return r;
        }
    }
}
