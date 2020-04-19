using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Landscape.Base.Entity.DTO
{
    public class CIDTO
    {
        [Required] public Guid ID { get; private set; }
        [Required] public CITypeDTO Type { get; private set; }
        [Required] public IDictionary<string, CIAttributeDTO> Attributes { get; private set; }

        public static CIDTO Build(Guid ciid, CITypeDTO type, IEnumerable<CIAttributeDTO> attributes)
        {
            return new CIDTO
            {
                Type = type,
                ID = ciid,
                Attributes = attributes.ToDictionary(a => a.Name)
            };
        }

        public static CIDTO Build(MergedCI ci)
        {
            return new CIDTO
            {
                ID = ci.ID,
                Type = CITypeDTO.Build(ci.Type),
                Attributes = ci.MergedAttributes.Select(ma =>
                    CIAttributeDTO.Build(ma.Attribute.Name, ma.Attribute.Value.ToGeneric(), ma.Attribute.State)
                ).ToDictionary(a => a.Name)
            };
        }
    }

}
