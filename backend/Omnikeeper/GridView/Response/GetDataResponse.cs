using Omnikeeper.Base.Entity.DTO;
using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetDataResponse
    {
        public IEnumerable<Row> Rows { get; set; }

        public GetDataResponse(IEnumerable<Row> Rows)
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
        public string ColumnID { get; set; }
        public AttributeValueDTO Value { get; set; }
        public bool Changeable { get; set; }

        public Cell(string columnID, AttributeValueDTO Value, bool Changeable)
        {
            ColumnID = columnID;
            this.Value = Value;
            this.Changeable = Changeable;
        }
    }
}
