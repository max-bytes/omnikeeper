using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Inbound;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using Newtonsoft.Json;
using OKPluginOIASharepoint;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
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
            var id = GuidUtility.Create(ciid, name + layer.ID.ToString());// TODO: determine if we need to factor in value or not
            return CIAttribute.Build(id, name, ciid, AttributeScalarValueText.BuildFromString(value), AttributeState.New, StaticChangesetID);
        }

        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            if (!atTime.IsLatest && !useCurrentForHistoric) return null; // we don't have historic information

            var externalID = mapper.GetExternalID(ciid);
            if (!externalID.HasValue)
                return null;

            return await GetAttribute(name, ciid, externalID.Value);
        }
        public async Task<CIAttribute> GetAttribute(string name, Guid ciid, SharepointExternalListItemID externalID)
        {
            if (!cachedListConfigs.TryGetValue(externalID.listID, out var listConfig))
                return null; // list is not configured (anymore)

            var columnName = listConfig.AttributeName2ColumnName(name);
            if (columnName == null)
                return null; // column is not configured

            try
            {
                var item = await client.GetListItem(externalID.listID, externalID.itemID, new string[] { columnName });

                var value = item.GetOr(columnName, null) as string;
                if (value == null)
                    return null; // attribute is not present in list item

                return BuildAttributeFromValue(name, value, ciid);
            }
            catch (Exception e)
            { // TODO: handle
                return null;
            }
        }

        public Task<CIAttribute> GetFullBinaryAttribute(string name, Guid ciid, TimeThreshold atTime)
        {
            return Task.FromResult<CIAttribute>(null); // TODO: not implemented
        }

        public async IAsyncEnumerable<CIAttribute> GetAttributes(ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest && !useCurrentForHistoric) yield break; // we don't have historic information

            var ciids = GetCIIDsFromSelections(selection).ToHashSet();
            var idPairs = mapper.GetIDPairs(ciids);

            await foreach (var a in GetAttributes(idPairs))
                yield return a;
        }
        public async IAsyncEnumerable<CIAttribute> GetAttributes(IEnumerable<(Guid ciid, SharepointExternalListItemID externalID)> idPairs)
        {
            var listIDGroups = idPairs.GroupBy(f => f.externalID.listID);

            foreach(var listIDGroup in listIDGroups)
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
                catch (Exception e)
                { // TODO: handle
                    continue;
                }

                foreach (var (itemGuid, itemColumns) in items)
                {
                    if (!listItemID2CIIDMap.TryGetValue(itemGuid, out var ciid))
                        continue; // the external item does not have a mapping to a CI

                    foreach (var column in itemColumns) {
                        var columnName = column.Key;
                        var columnValue = column.Value;
                        var attributeValue = columnValue as string;
                        if (columnValue == null) continue; // TODO: handle
                        var attributeNames = listConfig.ColumnName2AttributeNames(columnName);
                        foreach(var attributeName in attributeNames)
                            yield return BuildAttributeFromValue(attributeName, attributeValue, ciid);
                    }
                }
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByFullName(string attributeName, ICIIDSelection selection, TimeThreshold atTime)
        {
            if (!atTime.IsLatest && !useCurrentForHistoric) yield break; // we don't have historic information

            var ciids = GetCIIDsFromSelections(selection).ToHashSet();
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
                catch (Exception e)
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
                    yield return BuildAttributeFromValue(attributeName, attributeValue, ciid);
                }
            }
        }

        public async IAsyncEnumerable<CIAttribute> FindAttributesByName(string regex, ICIIDSelection selection, TimeThreshold atTime)
        {
            //if (!atTime.IsLatest) yield break; // we don't have historic information


            yield break; // TODO: implement
        }

        public IAsyncEnumerable<Relation> GetRelations(IRelationSelection rl, TimeThreshold atTime)
        {
            return AsyncEnumerable.Empty<Relation>();// TODO: implement
        }

        public async Task<Relation> GetRelation(Guid fromCIID, Guid toCIID, string predicateID, TimeThreshold atTime)
        {
            return null;// TODO: implement
        }


        private IEnumerable<Guid> GetCIIDsFromSelections(ICIIDSelection selection)
        {
            return selection switch
            {
                AllCIIDsSelection _ => mapper.GetAllCIIDs(),
                SpecificCIIDsSelection multiple => multiple.CIIDs,
                _ => null,// must not be
            };
        }
    }
}
