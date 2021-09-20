using Newtonsoft.Json;
using Omnikeeper.Base.Utils;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Entity
{
    public class GridViewConfiguration
    {
        public bool ShowCIIDColumn { get; set; }
        public string WriteLayer { get; set; }
        public List<string> ReadLayerset { get; set; }
        public List<GridViewColumn> Columns { get; set; }
        public string Trait { get; set; }

        public GridViewConfiguration(bool ShowCIIDColumn, string WriteLayer, List<string> ReadLayerset, List<GridViewColumn> Columns, string Trait)
        {
            this.ShowCIIDColumn = ShowCIIDColumn;
            this.WriteLayer = WriteLayer;
            this.ReadLayerset = ReadLayerset;
            this.Columns = Columns;
            this.Trait = Trait;
        }

        public static MyJSONSerializer<GridViewConfiguration> Serializer = new MyJSONSerializer<GridViewConfiguration>(new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            MissingMemberHandling = MissingMemberHandling.Error
        });
    }

    public class GridViewColumn
    {
        public string? SourceAttributeName { get; set; } // TODO: remove, is obsolete
        public string[]? SourceAttributePath { get; set; }
        public string ColumnDescription { get; set; }
        public AttributeValueType? ValueType { get; set; }
        public string? WriteLayer { get; set; }

        public GridViewColumn(string? SourceAttributeName, string[]? SourceAttributePath, string ColumnDescription, string? WriteLayer, AttributeValueType? valueType)
        {
            this.SourceAttributeName = SourceAttributeName;
            this.SourceAttributePath = SourceAttributePath;
            this.ColumnDescription = ColumnDescription;
            this.WriteLayer = WriteLayer;
            ValueType = valueType;
        }

        public static string GenerateColumnID(GridViewColumn column)
        {
            if (column.SourceAttributeName != null && column.SourceAttributePath == null)
                return $"columnID_{column.SourceAttributeName}";
            else if (column.SourceAttributeName == null && column.SourceAttributePath != null)
                return $"columnID_{string.Join(",", column.SourceAttributePath)}";
            throw new Exception("Invalid source attribute configuration for column detected");
        }
    }
}
