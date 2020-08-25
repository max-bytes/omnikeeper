namespace Landscape.Base.Entity
{
    public class PredicateDTO
    {
        public string ID { get; private set; }
        public string WordingFrom { get; private set; }
        public string WordingTo { get; private set; }

        private PredicateDTO() { }

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
