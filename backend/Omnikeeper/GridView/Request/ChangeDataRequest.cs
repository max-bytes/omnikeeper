using Omnikeeper.Base.Entity.DTO;
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
        public Guid Ciid { get; set; }
        public List<ChangeDataCell> Cells { get; set; }

        public SparseRow(Guid Ciid, List<ChangeDataCell> Cells)
        {
            this.Ciid = Ciid;
            this.Cells = Cells;
        }
    }

    public class ChangeDataCell
    {
        public string ID { get; set; }
        public AttributeValueDTO Value { get; set; }
        public bool Changeable { get; set; } // TODO: needed?

        public ChangeDataCell(string id, AttributeValueDTO Value, bool Changeable)
        {
            this.ID = id;
            this.Value = Value;
            this.Changeable = Changeable;
        }
    }
}
