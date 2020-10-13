using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Request
{
    public class ChangeDataRequest
    {
        public List<SparseRow> SparseRows { get; set; }
    }

    public class SparseRow
    {
        public Guid Ciid { get; set; }
        public List<ChangeDataCell> Cells { get; set; }
    }

    public class ChangeDataCell
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Changeable { get; set; }
    }
}
