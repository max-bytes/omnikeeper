using System;
using System.ComponentModel.DataAnnotations;

namespace Landscape.Base.Entity.DTO
{
    public class RelatedCIDTO
    {
        [Required] public Guid FromCIID { get; set; }
        [Required] public CIDTO ToCI { get; set; }
        [Required] public string PredicateID { get; set; }
        [Required] public RelationState State { get; set; }

        public static RelatedCIDTO Build(MergedRelatedCI relatedCI)
        {
            return new RelatedCIDTO
            {
                FromCIID = relatedCI.FromCIID,
                ToCI = CIDTO.Build(relatedCI.CI),
                PredicateID = relatedCI.PredicateID,
                State = relatedCI.RelationState
            };
        }
    }

    public class RelationDTO
    {
        [Required] public Guid ID { get; set; }
        [Required] public Guid FromCIID { get; set; }
        [Required] public Guid ToCIID { get; set; }
        [Required] public string PredicateID { get => Predicate.ID; }
        [Required] public PredicateDTO Predicate { get; set; }
        [Required] public RelationState State { get; set; }

        public static RelationDTO Build(MergedRelation r)
        {
            return new RelationDTO
            {
                ID = r.Relation.ID,
                FromCIID = r.Relation.FromCIID,
                ToCIID = r.Relation.ToCIID,
                Predicate = PredicateDTO.Build(r.Relation.Predicate),
                State = r.Relation.State
            };
        }
    }

}
