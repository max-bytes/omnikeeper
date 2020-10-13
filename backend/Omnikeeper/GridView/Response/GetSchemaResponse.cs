using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetSchemaResponse
    {
        public bool ShowCIIDColumn { get; set; }
        public List<Column> Columns { get; set; }
    }

    public class Column
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
