namespace Landscape.Base.Entity
{
    public class RelationTemplate
    {
        public string PredicateID { get; private set; }
        // TODO: description?
        //public CIType[] FromCITypes { get; private set; }
        //public string[] ToCITypeIDs { get; private set; }

        public int? MinCardinality { get; private set; }
        public int? MaxCardinality { get; private set; }

        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)

        public static RelationTemplate Build(string predicateID, int? minCardinality, int? maxCardinality)
        {
            return new RelationTemplate()
            {
                PredicateID = predicateID,
                //FromCITypes = fromCITypes,
                MinCardinality = minCardinality,
                MaxCardinality = maxCardinality
            };
        }
    }
}
