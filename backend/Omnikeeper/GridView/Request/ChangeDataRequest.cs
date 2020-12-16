using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Request
{
    public class ChangeDataRequest
    {
        public List<SparseRow> SparseRows { get; set; }

        public ChangeDataRequest(List<SparseRow> SparseRows)
        {
            this.SparseRows = SparseRows;
        }
    }

    public class SparseRow
    {
        public Guid? Ciid { get; set; }
        public List<ChangeDataCell> Cells { get; set; }

        public SparseRow(Guid? Ciid, List<ChangeDataCell> Cells)
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
