using LandscapeRegistry.Entity.AttributeValues;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Landscape.Base.Entity.DTO
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
            return Build(attribute.Attribute.ID, attribute.Attribute.Name, attribute.Attribute.Value.ToDTO(), attribute.Attribute.CIID, attribute.Attribute.State);
        }
        public static CIAttributeDTO Build(Guid id, string name, AttributeValueDTO value, Guid ciid, AttributeState state)
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

        public bool Equals([AllowNull] IAttributeValue other) => Equals(other as AttributeValueDTO);
        public bool Equals([AllowNull] AttributeValueDTO other) => other != null && Values.SequenceEqual(other.Values);
        public override int GetHashCode() => HashCode.Combine(IsArray, Values.GetHashCode());

        public static AttributeValueDTO Build(string[] values, bool isArray, AttributeValueType type)
        {
            if (isArray)
            {
                return Build(values[0], type);
            } else
            {
                return Build(values, type);
            }
        }

        public static AttributeValueDTO Build(string value, AttributeValueType type)
        {
            return new AttributeValueDTO()
            {
                Values = new string[] { value },
                IsArray = false,
                Type = type
            };
        }
        public static AttributeValueDTO Build(string[] values, AttributeValueType type)
        {
            return new AttributeValueDTO()
            {
                Values = values,
                IsArray = true,
                Type = type
            };
        }

        public string Value2DatabaseString()
        {
            if (IsArray)
            {
                return $"A{MarshalValues(Values)}";
            }
            else
            {
                return $"S{Values[0]}";
            }

        }

        public static AttributeValueDTO BuildFromDatabase(string value, AttributeValueType type)
        {
            var multiplicityIndicator = value.Substring(0, 1);
            var finalValue = value.Substring(1);
            if (multiplicityIndicator == "A")
            {
                var finalValues = UnmarshalValues(finalValue);
                return Build(finalValues, type);
            }
            else
                return Build(finalValue, type);
        }
        public static string MarshalValues(string[] values)
        {
            return string.Join(",", values.Select(value => value.Replace("\\", "\\\\").Replace(",", "\\,")));
        }
        public static string[] UnmarshalValues(string value)
        {
            var values = value.Tokenize(',', '\\');
            return values.Select(v => v.Replace("\\\\", "\\")).ToArray();
        }
    }
}
