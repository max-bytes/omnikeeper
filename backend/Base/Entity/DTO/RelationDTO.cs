using System;
using System.ComponentModel.DataAnnotations;

namespace Landscape.Base.Entity.DTO
{
    public class RelatedCIDTO
    {
        [Required] public Guid FromCIID { get; private set; }
        [Required] public CIDTO ToCI { get; private set; }
        [Required] public string PredicateID { get; private set; }
        [Required] public RelationState State { get; private set; }

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

    //public class RelationDTO
    //{
    //    [Required] public Guid FromCIID { get; private set; }
    //    [Required] public CIDTO ToCI { get; private set; }
    //    [Required] public string PredicateID { get => Predicate.ID; }
    //    [Required] public PredicateDTO Predicate { get; private set; }
    //    [Required] public RelationState State { get; private set; }

    //    public static RelationDTO Build(Relation r, CIDTO toCI)
    //    {
    //        return new RelationDTO
    //        {
    //            FromCIID = r.FromCIID,
    //            ToCI = toCI,
    //            Predicate = PredicateDTO.Build(r.Predicate),
    //            State = r.State
    //        };
    //    }
    //}

}
