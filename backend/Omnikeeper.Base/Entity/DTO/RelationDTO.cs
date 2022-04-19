using System;

namespace Omnikeeper.Base.Entity.DTO
{
    public class RelationDTO
    {
        //private RelationDTO(Guid iD, Guid fromCIID, Guid toCIID, string predicateID, bool mask)
        //{
        //    ID = iD;
        //    FromCIID = fromCIID;
        //    ToCIID = toCIID;
        //    PredicateID = predicateID;
        //    Mask = mask;
        //}

        public Guid ID { get; set; }
        public Guid FromCIID { get; set; }
        public Guid ToCIID { get; set; }
        public string PredicateID { get; set; } = "";
        public bool Mask { get; set; }

        public static RelationDTO BuildFromMergedRelation(MergedRelation r)
        {
            return new RelationDTO()
            {
                ID = r.Relation.ID,
                FromCIID = r.Relation.FromCIID,
                ToCIID = r.Relation.ToCIID,
                PredicateID = r.Relation.PredicateID,
                Mask = r.Relation.Mask
            };
        }

        public static RelationDTO BuildFromRelation(Relation r)
        {
            return new RelationDTO()
            {
                ID = r.ID,
                FromCIID = r.FromCIID,
                ToCIID = r.ToCIID,
                PredicateID = r.PredicateID,
                Mask = r.Mask
            };
        }
    }

}
