using System.Collections.Generic;

namespace Omnikeeper.GridView.Model
{
    public class GridViewConfiguration
    {
        public bool ShowCIIDColumn { get; set; }
        public int WriteLayer { get; set; }
        public List<int> ReadLayerset { get; set; }
        public List<GridViewColumn> Columns { get; set; }
        public List<string> Traitset { get; set; }
    }

    public class GridViewColumn
    {
        public string SourceAttributeName { get; set; }
        public string ColumnDescription { get; set; }
        public int? WriteLayer { get; set; }
    }
}
