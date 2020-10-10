using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class ChangeDataResponse
    {
        public List<ChangeDataRow> Rows { get; set; }
    }

    public class ChangeDataRow
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
