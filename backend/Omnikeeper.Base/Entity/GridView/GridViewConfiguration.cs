using System.Collections.Generic;

namespace Omnikeeper.Base.Entity.GridView
{
    public class GridViewConfiguration
    {
        public bool ShowCIIDColumn { get; set; }
        public int WriteLayer { get; set; }
        public List<long> ReadLayerset { get; set; }
        public List<GridViewColumn> Columns { get; set; }
        public string Trait { get; set; }
    }

    public class GridViewColumn
    {
        public string SourceAttributeName { get; set; }
        public string ColumnDescription { get; set; }
        public int? WriteLayer { get; set; }
    }
}
