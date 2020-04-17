namespace Landscape.Base.Entity
{
    public class RelationTemplate
    {
        public Predicate Predicate { get; private set; }
        // TODO: description?
        //public CIType[] FromCITypes { get; private set; }
        public CIType[] ToCITypes { get; private set; }

        public int? MinCardinality { get; private set; }
        public int? MaxCardinality { get; private set; }

        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)

        public static RelationTemplate Build(Predicate predicate, CIType[] toCITypes, int? minCardinality, int? maxCardinality)
        {
            return new RelationTemplate()
            {
                Predicate = predicate,
                //FromCITypes = fromCITypes,
                ToCITypes = toCITypes,
                MinCardinality = minCardinality,
                MaxCardinality = maxCardinality
            };
        }
    }
}
