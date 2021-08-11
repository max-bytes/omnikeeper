#nullable disable // TODO

using Newtonsoft.Json;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Omnikeeper.Base.Entity.DTO
{
    public class CIAttributeDTO
    {
        [Required] public Guid ID { get; set; } = default;
        [Required] public string Name { get; set; } = "";
        [Required] public AttributeValueDTO Value { get; set; } = default;
        [Required] public Guid CIID { get; set; } = default;
        [Required] public AttributeState State { get; set; } = default;

        private CIAttributeDTO() { }

        public static CIAttributeDTO Build(MergedCIAttribute attribute)
        {
            var DTOValue = AttributeValueDTO.Build(attribute.Attribute.Value);
            return Build(attribute.Attribute.ID, attribute.Attribute.Name, DTOValue, attribute.Attribute.CIID, attribute.Attribute.State);
        }
        private static CIAttributeDTO Build(Guid id, string name, AttributeValueDTO value, Guid ciid, AttributeState state)
        {
            return new CIAttributeDTO
            {
                ID = id,
                Name = name,
                Value = value,
                CIID = ciid,
                State = state
            };
        }
    }


    public class BulkCIAttributeLayerScopeDTO
    {
        public class FragmentDTO
        {
            [Required] public string Name { get; set; } = "";
            [Required] public AttributeValueDTO Value { get; set; } = null;
            [Required] public Guid CIID { get; set; } = default;

            private FragmentDTO() { }
        }

        [Required] public string NamePrefix { get; set; } = "";
        [Required] public string LayerID { get; set; } = default;
        [Required] public FragmentDTO[] Fragments { get; set; } = default;

        private BulkCIAttributeLayerScopeDTO() { }
    }

    public class AttributeValueDTO : IEquatable<AttributeValueDTO>
    {
        [Required] public AttributeValueType Type { get; set; } = default;
        [Required] public bool IsArray { get; set; } = default;
        [Required] public string[] Values { get; set; } = default;

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
