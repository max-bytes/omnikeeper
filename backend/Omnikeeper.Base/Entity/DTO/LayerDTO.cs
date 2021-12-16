using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO
{
    public class LayerDTO
    {
        [Required] public string ID { get; set; }

        private LayerDTO(string id)
        {
            ID = id;
        }

        public static LayerDTO Build(Layer l)
        {
            return new LayerDTO(l.ID);
        }
    }
}
