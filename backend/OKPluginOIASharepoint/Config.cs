using Landscape.Base.Entity;
using Landscape.Base.Inbound;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace OKPluginOIASharepoint
{
    public class Config : IOnlineInboundAdapter.IConfig
    {
        public class ListColumnConfig
        {
            public readonly string sourceColumn;
            public readonly string targetAttributeName;
            // TODO: types, etc.

            public ListColumnConfig(string sourceColumn, string targetAttributeName)
            {
                this.sourceColumn = sourceColumn;
                this.targetAttributeName = targetAttributeName;
            }
        }
        public class ListConfig
        {
            public readonly Guid listID;
            public readonly ListColumnConfig[] columnConfigs;
            public readonly string[] identifiableAttributes;
            public readonly long[] searchableLayerIDs;

            public ListConfig(Guid listID, ListColumnConfig[] columnConfigs, string[] identifiableAttributes, long[] searchableLayerIDs)
            {
                this.listID = listID;
                this.columnConfigs = columnConfigs;
                this.identifiableAttributes = identifiableAttributes;
                this.searchableLayerIDs = searchableLayerIDs;
            }
        }

        public class CachedListConfig
        {
            private readonly ImmutableDictionary<string, string> attributeName2ColumnNameMap;
            private readonly ImmutableDictionary<string, string[]> columnName2AttributeNamesMap;

            public readonly string[] columnNames;
            public readonly Guid listID;
            public readonly LayerSet searchableLayerSet;
            public readonly (string columnName, string attributeName)[] identifiableColumnsAttributeTuples;

            public CachedListConfig(ListConfig baseConfig)
            {
                listID = baseConfig.listID;
                searchableLayerSet = new LayerSet(baseConfig.searchableLayerIDs);
                attributeName2ColumnNameMap = baseConfig.columnConfigs.ToImmutableDictionary(cc => cc.targetAttributeName, cc => cc.sourceColumn);
                columnName2AttributeNamesMap = baseConfig.columnConfigs.ToLookup(cc => cc.sourceColumn).ToImmutableDictionary(cc => cc.Key, cc => cc.Select(ccc => ccc.targetAttributeName).ToArray());
                columnNames = columnName2AttributeNamesMap.Keys.ToArray();

                identifiableColumnsAttributeTuples = baseConfig.identifiableAttributes.Select(ia => (attributeName2ColumnNameMap[ia], ia)).ToArray();

                // TODO: make validation checks, such as that the identifiable attributes MUST be a subset of the defined columns
            }

            internal string AttributeName2ColumnName(string attributeName)
            {
                if (!attributeName2ColumnNameMap.TryGetValue(attributeName, out var columnNames))
                    return null;
                return columnNames;
            }

            internal string[] ColumnName2AttributeNames(string columnName)
            {
                if (!columnName2AttributeNamesMap.TryGetValue(columnName, out var attributeName))
                    return new string[] { };
                return attributeName;
            }
        }


        public readonly Guid tenantID;
        public readonly string siteDomain;
        public readonly string site;
        public readonly Guid clientID;
        public readonly string clientSecret;
        public readonly bool useCurrentForHistoric;

        public string MapperScope { get; }
        public readonly TimeSpan preferredIDMapUpdateRate;
        public readonly ListConfig[] listConfigs;

        [Newtonsoft.Json.JsonIgnore]
        public string BuilderName { get; } = OnlineInboundAdapter.Builder.StaticName;

        public Config(Guid tenantID, string siteDomain, string site, Guid clientID, string clientSecret, bool useCurrentForHistoric, TimeSpan preferredIDMapUpdateRate, string mapperScope, ListConfig[] listConfigs)
        {
            this.tenantID = tenantID;
            this.siteDomain = siteDomain;
            this.site = site;
            this.clientID = clientID;
            this.clientSecret = clientSecret;
            this.useCurrentForHistoric = useCurrentForHistoric;
            MapperScope = mapperScope;
            this.preferredIDMapUpdateRate = preferredIDMapUpdateRate;
            this.listConfigs = listConfigs;
        }
    }
}
