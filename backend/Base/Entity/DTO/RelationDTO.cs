using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Landscape.Base.Entity.DTO
{
    public class RelationDTO
    {
        [Required] public string FromCIID { get; private set; }
        [Required] public CIDTO ToCI { get; private set; }
        [Required] public string PredicateID { get => Predicate.ID; }
        [Required] public PredicateDTO Predicate { get; private set; }
        [Required] public RelationState State { get; private set; }

        public static RelationDTO Build(Relation r, CIDTO toCI)
        {
            return new RelationDTO
            {
                FromCIID = r.FromCIID,
                ToCI = toCI,
                Predicate = PredicateDTO.Build(r.Predicate),
                State = r.State
            };
        }
    }

}
