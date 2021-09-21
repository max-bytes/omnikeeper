using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Entity.DataOrigin;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static OKPluginOIASharepoint.Config;

namespace OKPluginOIASharepoint
{
    public class LayerAccessProxy : ILayerAccessProxy
    {
        private readonly SharepointClient client;
        private readonly Layer layer;
        private readonly bool useCurrentForHistoric;
        private readonly ScopedExternalIDMapper mapper;

        public static readonly Guid StaticChangesetID = GuidUtility.Create(new Guid("a09018d6-d302-4137-acae-a81f2aa1a243"), "sharepoint");

        private readonly IDictionary<Guid, CachedListConfig> cachedListConfigs;

        public LayerAccessProxy(IDictionary<Guid, CachedListConfig> cachedListConfigs, SharepointClient client, ScopedExternalIDMapper mapper, Layer layer, bool useCurrentForHistoric)
        {
            this.client = client;
            this.layer = layer;
            this.useCurrentForHistoric = useCurrentForHistoric;
            this.mapper = mapper;

            this.cachedListConfigs = cachedListConfigs;
        }

        public CIAttribute BuildAttributeFromValue(string name, string value, Guid ciid)
        {
            // create a deterministic, dependent guid from the ciid + attribute name + value
            var id = GuidUtility.Create(ciid, name + layer.ID.ToString() + value); // NOTE: id must change when the value changes
            return new CIAttribute(id, name, ciid, new AttributeScalarValueText(value), AttributeState.New, StaticChangesetID);
        }

        public Task<CIAttribute?> GetFullBinaryAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            return Task.FromResult<CIAttribute?>(null); // TODO: not implemented
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime, string? nameRegexFilter = null)
        {
            if (!atTime.IsLatest && !useCurrentForHistoric) yield break; // we don't have historic information

            var ciids = selection.GetCIIDs(() => mapper.GetAllCIIDs()).ToHashSet();
            var idPairs = mapper.GetIDPairs(ciids);

            var nameRegex = (nameRegexFilter != null) ? new Regex(nameRegexFilter) : null;

            await foreach (var a in GetAttributes(idPairs))
                if (nameRegex == null || nameRegex.IsMatch(a.Name))
                    yield return a;
        }
        public async IAsyncEnumerable<CIAttribute> GetAttributes(IEnumerable<(Guid ciid, SharepointExternalListItemID externalID)> idPairs)
        {
            var listIDGroups = idPairs.GroupBy(f => f.externalID.listID);

            foreach (var listIDGroup in listIDGroups)
            {
                var listID = listIDGroup.Key;
                var listItemID2CIIDMap = listIDGroup.ToDictionary(l => l.externalID.itemID, l => l.ciid);

                if (!cachedListConfigs.TryGetValue(listID, out var listConfig))
                    continue; // list is not configured (anymore)

                IEnumerable<(Guid itemGuid, System.Dynamic.ExpandoObject data)> items;
                try
                {
                    items = await client.GetListItems(listID, listConfig.columnNames).ToListAsync();
                }
                catch (Exception)
                { // TODO: handle
                    continue;
                }

                foreach (var (itemGuid, itemColumns) in items)
                {
                    if (!listItemID2CIIDMap.TryGetValue(itemGuid, out var ciid))
                        continue; // the external item does not have a mapping to a CI

                    foreach (var column in itemColumns)
                    {
                        var columnName = column.Key;
                        var columnValue = column.Value;
                        var attributeValue = (columnValue as string);
                        if (columnValue == null) continue; // TODO: handle
                        if (attributeValue == null) continue; // TODO: handle
                        var attributeNames = listConfig.ColumnName2AttributeNames(columnName);
                        foreach (var attributeName in attributeNames)
                            yield return BuildAttributeFromValue(attributeName, attributeValue, ciid);
                    }
                }
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string attributeName, ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest && !useCurrentForHistoric) yield break; // we don't have historic information

            var ciids = selection.GetCIIDs(() => mapper.GetAllCIIDs()).ToHashSet();
            var idPairs = mapper.GetIDPairs(ciids);

            var listIDGroups = idPairs.GroupBy(f => f.externalID.listID);

            foreach (var listIDGroup in listIDGroups)
            {
                var listID = listIDGroup.Key;
                var listItemID2CIIDMap = listIDGroup.ToDictionary(l => l.externalID.itemID, l => l.ciid);

                if (!cachedListConfigs.TryGetValue(listID, out var listConfig))
                    continue; // list is not configured (anymore)

                var columnName = listConfig.AttributeName2ColumnName(attributeName);
                if (columnName == null)
                    continue; // attribute name is not mapped for this list -> ignore

                IEnumerable<(Guid itemGuid, System.Dynamic.ExpandoObject data)> items;
                try
                {
                    // TODO: restrict the items to get to the ones where the guid is requested (or should we simply fetch all and discard later?)
                    items = await client.GetListItems(listID, new string[] { columnName }).ToListAsync();
                }
                catch (Exception)
                { // TODO: handle
                    continue;
                }

                foreach (var (itemGuid, itemColumns) in items)
                {
                    if (!listItemID2CIIDMap.TryGetValue(itemGuid, out var ciid))
                        continue; // the external item does not have a mapping to a CI

                    if (!ciids.Contains(ciid))
                        continue; // we got an item that is not actually requested, discard

                    if (!((IDictionary<string, object>)itemColumns).TryGetValue(columnName, out var columnValue))
                        continue; // the external item does not actually have the column we requested

                    var attributeValue = columnValue as string;
                    if (columnValue == null) continue; // TODO: handle
                    if (attributeValue == null) continue; // TODO: handle
                    yield return BuildAttributeFromValue(attributeName, attributeValue, ciid);
                }
            }
        }

        public IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }

        public Task<Relation?> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
        {
            return Task.FromResult<Relation?>(null);// TODO: implement
        }
    }
}
