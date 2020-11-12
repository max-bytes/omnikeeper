using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO
{
    public class LayerDTO
    {
        [Required] public string Name { get; set; }
        [Required] public long ID { get; set; }

        private LayerDTO(string name, long iD)
        {
            Name = name;
            ID = iD;
        }

        public static LayerDTO Build(Layer l)
        {
            return new LayerDTO(l.Name, l.ID);
        }
    }
}
