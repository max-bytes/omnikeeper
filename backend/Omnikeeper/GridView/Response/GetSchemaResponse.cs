﻿using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetSchemaResponse
    {
        public bool ShowCIIDColumn { get; set; }
        public List<Column> Columns { get; set; }

        public GetSchemaResponse(bool ShowCIIDColumn, List<Column> Columns)
        {
            this.ShowCIIDColumn = ShowCIIDColumn;
            this.Columns = Columns;
        }
    }

    public class Column
    {
        public string Name { get; set; }
        public string Description { get; set; }

        public Column(string Name, string Description)
        {
            this.Name = Name;
            this.Description = Description;
        }
    }
}
