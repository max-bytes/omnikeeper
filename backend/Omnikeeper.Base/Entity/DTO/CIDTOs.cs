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

        public CIDTO (Guid ciid, IEnumerable<CIAttributeDTO> attributes)
        {
            ID = ciid;
            Attributes = attributes.ToDictionary(a => a.Name);
        }

        public static CIDTO BuildFromMergedCI(MergedCI ci)
        {
            return new CIDTO(
                ci.ID,
                ci.MergedAttributes.Values.Select(ma => CIAttributeDTO.Build(ma))
            );
        }
    }

}
