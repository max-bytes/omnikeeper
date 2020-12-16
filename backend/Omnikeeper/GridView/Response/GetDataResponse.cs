using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetDataResponse
    {
        public List<Row> Rows { get; set; }

        public GetDataResponse(List<Row> Rows)
        {
            this.Rows = Rows;
        }
    }

    public class Row
    {
        public Guid Ciid { get; set; }
        public List<Cell> Cells { get; set; }

        public Row(Guid Ciid, List<Cell> Cells)
        {
            this.Ciid = Ciid;
            this.Cells = Cells;
        }
    }

    public class Cell
    {
        public string Name { get; set; }
        public string? Value { get; set; }
        public bool Changeable { get; set; }

        public Cell(string Name, string? Value, bool Changeable)
        {
            this.Name = Name;
            this.Value = Value;
            this.Changeable = Changeable;
        }
    }
}
