using Omnikeeper.Entity.AttributeValues;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;

namespace Omnikeeper.Base.Entity.DTO
{
    public class CIAttributeDTO
    {
        public Guid ID { get; set; } = default;
        public string Name { get; set; } = "";
        public AttributeValueDTO Value { get; set; } = default;
        public Guid CIID { get; set; } = default;

        public static CIAttributeDTO Build(MergedCIAttribute attribute)
        {
            var DTOValue = AttributeValueDTO.Build(attribute.Attribute.Value);
            return Build(attribute.Attribute.ID, attribute.Attribute.Name, DTOValue, attribute.Attribute.CIID);
        }
        public static CIAttributeDTO Build(CIAttribute attribute)
        {
            var DTOValue = AttributeValueDTO.Build(attribute.Value);
            return Build(attribute.ID, attribute.Name, DTOValue, attribute.CIID);
        }
        private static CIAttributeDTO Build(Guid id, string name, AttributeValueDTO value, Guid ciid)
        {
            return new CIAttributeDTO
            {
                ID = id,
                Name = name,
                Value = value,
                CIID = ciid
            };
        }
    }


    public class BulkCIAttributeLayerScopeDTO
    {
        public class FragmentDTO
        {
            public string Name { get; set; } = "";
            public AttributeValueDTO Value { get; set; } = null;
            public Guid CIID { get; set; } = default;

            private FragmentDTO() { }
        }

        public string NamePrefix { get; set; } = "";
        public string LayerID { get; set; } = default;
        public FragmentDTO[] Fragments { get; set; } = default;

        private BulkCIAttributeLayerScopeDTO() { }
    }

    public class AttributeValueDTO : IEquatable<AttributeValueDTO>
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("type")]
        public AttributeValueType Type { get; set; } = default;
        [JsonPropertyName("isArray")]
        public bool IsArray { get; set; } = default;
        [JsonPropertyName("values")]
        public string[] Values { get; set; } = default;

        public override bool Equals([AllowNull] object other) => Equals(other as AttributeValueDTO);
        public bool Equals([AllowNull] AttributeValueDTO other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => HashCode.Combine(IsArray, Values.GetHashCode());

        public static AttributeValueDTO Build(IAttributeValue a)
        {
            return new AttributeValueDTO()
            {
                Values = a.ToRawDTOValues(),
                IsArray = a.IsArray,
                Type = a.Type
            };
        }

        public static AttributeValueDTO BuildEmpty(AttributeValueType valueType, bool array)
        {
            return new AttributeValueDTO()
            {
                Values = new string[0],
                IsArray = array,
                Type = valueType
            };
        }
    }
}
