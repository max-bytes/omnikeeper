using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Omnikeeper.Base.Entity.DTO
{
    public class CIDTO
    {
        [Required] public Guid ID { get; set; }
        [Required] public IDictionary<string, CIAttributeDTO> Attributes { get; set; }

        public static CIDTO Build(Guid ciid, IEnumerable<CIAttributeDTO> attributes)
        {
            return new CIDTO
            {
                ID = ciid,
                Attributes = attributes.ToDictionary(a => a.Name)
            };
        }

        public static CIDTO Build(MergedCI ci)
        {
            return new CIDTO
            {
                ID = ci.ID,
                Attributes = ci.MergedAttributes.Values.Select(ma => CIAttributeDTO.Build(ma)
                ).ToDictionary(a => a.Name)
            };
        }
    }

}
