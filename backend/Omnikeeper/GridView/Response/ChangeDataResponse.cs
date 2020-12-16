using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class ChangeDataResponse
    {
        public List<ChangeDataRow> Rows { get; set; }

        public ChangeDataResponse(List<ChangeDataRow> Rows)
        {
            this.Rows = Rows;
        }
    }

    public class ChangeDataRow
    {
        public Guid Ciid { get; set; }
        public List<ChangeDataCell> Cells { get; set; }
        public ChangeDataRow(Guid Ciid, List<ChangeDataCell> Cells)
        {
            this.Ciid = Ciid;
            this.Cells = Cells;
        }
    }

    public class ChangeDataCell
    {
        public string Name { get; set; }
        public string? Value { get; set; }
        public bool Changeable { get; set; }

        public ChangeDataCell(string Name, string? Value, bool Changeable)
        {
            this.Name = Name;
            this.Value = Value;
            this.Changeable = Changeable;
        }
    }
}
