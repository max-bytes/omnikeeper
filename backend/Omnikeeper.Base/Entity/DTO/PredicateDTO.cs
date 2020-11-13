namespace Omnikeeper.Base.Entity
{
    public class PredicateDTO
    {
        public string ID { get; set; }
        public string WordingFrom { get; set; }
        public string WordingTo { get; set; }

        private PredicateDTO() { ID = ""; WordingFrom = ""; WordingTo = ""; }

        public static PredicateDTO Build(Predicate p)
        {
            return new PredicateDTO
            {
                ID = p.ID,
                WordingFrom = p.WordingFrom,
                WordingTo = p.WordingTo
            };
        }
    }
}
