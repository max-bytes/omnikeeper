using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Runtime.Serialization;

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

        private GetSchemaResponse() { }
    }

    public class Column
    {
        [DataMember(Name = "id")]
        public string ID { get; set; }
        public string Description { get; set; }
        public AttributeValueType ValueType { get; set; }
        public bool Writable { get; set; }

        public Column(string id, string Description, AttributeValueType attributeValueType, bool writable)
        {
            this.ID = id;
            this.Description = Description;
            ValueType = attributeValueType;
            this.Writable = writable;
        }

        private Column() { }
    }
}
