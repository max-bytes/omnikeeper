using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO
{
    public class LayerDTO
    {
        [Required] public string ID { get; set; }
        [Required] public string Description { get; set; }

        private LayerDTO(string id, string description)
        {
            ID = id;
            Description = description;
        }

        public static LayerDTO Build(Layer l)
        {
            return new LayerDTO(l.ID, l.Description);
        }
    }
}
