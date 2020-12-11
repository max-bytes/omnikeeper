using System.Collections.Generic;

namespace Omnikeeper.GridView.Entity
{
    public class GridViewConfiguration
    {
        public bool ShowCIIDColumn { get; set; }
        public long WriteLayer { get; set; }
        public List<long> ReadLayerset { get; set; }
        public List<GridViewColumn> Columns { get; set; }
        public string Trait { get; set; }
    }

    public class GridViewColumn
    {
        public string SourceAttributeName { get; set; }
        public string ColumnDescription { get; set; }
        public long? WriteLayer { get; set; }
    }
}
