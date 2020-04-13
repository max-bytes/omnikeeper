using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Landscape.Base.Entity.DTO
{
    public class LayerDTO
    {
        [Required] public string Name { get; set; }
        [Required] public long ID { get; set; }

        private LayerDTO() { }

        public static LayerDTO Build(Layer l)
        {
            return new LayerDTO()
            {
                Name = l.Name,
                ID = l.ID
            };
        }
    }
}
