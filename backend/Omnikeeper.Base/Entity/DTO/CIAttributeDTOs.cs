using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;

namespace Omnikeeper.Base.Entity.DTO
{
    public class CIAttributeDTO
    {
        [Required] public Guid ID { get; set; }
        [Required] public string Name { get; set; }
        [Required] public AttributeValueDTO Value { get; set; }
        [Required] public Guid CIID { get; set; }
        [Required] public AttributeState State { get; set; }

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
            [Required] public string Name { get; set; }
            [Required] public AttributeValueDTO Value { get; set; }
            [Required] public Guid CIID { get; set; }

            private FragmentDTO() { }
        }

        [Required] public string NamePrefix { get; set; }
        [Required] public long LayerID { get; set; }
        [Required] public FragmentDTO[] Fragments { get; set; }

        private BulkCIAttributeLayerScopeDTO() { }
    }

    public class AttributeValueDTO : IEquatable<AttributeValueDTO>
    {
        [Required] public AttributeValueType Type { get; set; }
        [Required] public bool IsArray { get; set; }
        [Required] public string[] Values { get; set; }

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
    }
}
