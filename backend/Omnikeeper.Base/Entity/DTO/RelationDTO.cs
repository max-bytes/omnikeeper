using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Omnikeeper.Base.Entity.DTO
{
    public class RelatedCIDTO
    {
        [Required] public Guid FromCIID { get; set; }
        [Required] public Guid ToCIID { get; set; }
        [Required] public string PredicateID { get; set; }

        public RelatedCIDTO(CompactRelatedCI relatedCI)
        {
            FromCIID = relatedCI.FromCIID;
            ToCIID = relatedCI.ToCIID;
            PredicateID = relatedCI.PredicateID;
        }
    }

    public class RelationDTO
    {
        [JsonConstructor]
        private RelationDTO(Guid iD, Guid fromCIID, Guid toCIID, string predicateID, RelationState state)
        {
            ID = iD;
            FromCIID = fromCIID;
            ToCIID = toCIID;
            PredicateID = predicateID;
            State = state;
        }

        [Required] public Guid ID { get; set; }
        [Required] public Guid FromCIID { get; set; }
        [Required] public Guid ToCIID { get; set; }
        [Required] public string PredicateID { get; set; }
        [Required] public RelationState State { get; set; }

        public static RelationDTO BuildFromMergedRelation(MergedRelation r)
        {
            return new RelationDTO(
                r.Relation.ID,
                r.Relation.FromCIID,
                r.Relation.ToCIID,
                r.Relation.PredicateID,
                r.Relation.State
            );
        }

        public static RelationDTO BuildFromRelation(Relation r)
        {
            return new RelationDTO(
                r.ID,
                r.FromCIID,
                r.ToCIID,
                r.PredicateID,
                r.State
            );
        }
    }

}
