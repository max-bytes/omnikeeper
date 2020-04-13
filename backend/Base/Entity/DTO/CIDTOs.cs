using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Landscape.Base.Entity.DTO
{
    public class CIDTO
    {
        [Required] public string Identity { get; private set; }
        [Required] public CITypeDTO Type { get; private set; }
        [Required] public IDictionary<string, CIAttributeDTO> Attributes { get; private set; }

        public static CIDTO Build(string CIIdentity, CITypeDTO type, IEnumerable<CIAttributeDTO> attributes)
        {
            return new CIDTO
            {
                Type = type,
                Identity = CIIdentity,
                Attributes = attributes.ToDictionary(a => a.Name)
            };
        }

        public static CIDTO Build(MergedCI ci)
        {
            return new CIDTO
            {
                Identity = ci.Identity,
                Type = CITypeDTO.Build(ci.Type),
                Attributes = ci.MergedAttributes.Select(ma =>
                    CIAttributeDTO.Build(ma.Attribute.Name, ma.Attribute.Value.ToGeneric(), ma.Attribute.State)
                ).ToDictionary(a => a.Name)
            };
        }
    }

}
