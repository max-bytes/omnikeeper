using System;
using System.Collections.Generic;

namespace Omnikeeper.GridView.Response
{
    public class GetDataResponse
    {
        public List<Row> Rows { get; set; }
    }

    public class Row
    {
        public Guid Ciid { get; set; }
        public List<Cell> Cells { get; set; }
    }

    public class Cell
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Changeable { get; set; }
    }
}
