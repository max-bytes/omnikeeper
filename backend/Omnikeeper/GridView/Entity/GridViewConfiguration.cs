﻿using Omnikeeper.Entity.AttributeValues;
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
    }

    public class GridViewColumn
    {
        public string SourceAttributeName { get; set; }
        public string ColumnDescription { get; set; }
        public AttributeValueType? ValueType { get; set; }
        public string? WriteLayer { get; set; }

        public GridViewColumn(string SourceAttributeName, string ColumnDescription, string? WriteLayer, AttributeValueType? valueType)
        {
            this.SourceAttributeName = SourceAttributeName;
            this.ColumnDescription = ColumnDescription;
            this.WriteLayer = WriteLayer;
            ValueType = valueType;
        }
    }
}
