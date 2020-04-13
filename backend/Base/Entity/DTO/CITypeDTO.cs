using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Landscape.Base.Entity.DTO
{
    public class CITypeDTO
    {
        [Required] public string ID { get; set; }

        private CITypeDTO() {}

        public static CITypeDTO Build(CIType t)
        {
            return new CITypeDTO()
            {
                ID = t.ID
            };
        }
    }
}
