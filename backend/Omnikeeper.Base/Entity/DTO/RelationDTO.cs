using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO
{
    public class RelatedCIDTO
    {
        [Required] public Guid FromCIID { get; set; }
        [Required] public CIDTO ToCI { get; set; }
        [Required] public string PredicateID { get; set; }
        [Required] public RelationState State { get; set; }

        public RelatedCIDTO(MergedRelatedCI relatedCI)
        {
            FromCIID = relatedCI.FromCIID;
            ToCI = CIDTO.BuildFromMergedCI(relatedCI.CI);
            PredicateID = relatedCI.PredicateID;
            State = relatedCI.RelationState;
        }
    }

    public class RelationDTO
    {
        [JsonConstructor]
        private RelationDTO(Guid iD, Guid fromCIID, Guid toCIID, PredicateDTO predicate, RelationState state)
        {
            ID = iD;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            Predicate = predicate;
            State = state;
        }

        [Required] public Guid ID { get; set; }
        [Required] public Guid FromCIID { get; set; }
        [Required] public Guid ToCIID { get; set; }
        [Required] public string PredicateID { get => Predicate.ID; }
        [Required] public PredicateDTO Predicate { get; set; }
        [Required] public RelationState State { get; set; }

        public static RelationDTO BuildFromMergedRelation(MergedRelation r)
        {
            return new RelationDTO(
                r.Relation.ID,
                r.Relation.FromCIID,
                r.Relation.ToCIID,
                PredicateDTO.Build(r.Relation.Predicate),
                r.Relation.State
            );
        }
    }

}
