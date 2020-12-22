using System;

namespace Omnikeeper.Base.Entity
{
    [Serializable]
    public class RelationTemplate
    {
        public readonly string PredicateID;
        // TODO: description?
        //public CIType[] FromCITypes { get; private set; }
        //public string[] ToCITypeIDs { get; private set; }

        public readonly int? MinCardinality;
        public readonly int? MaxCardinality;

        // TODO: status: required(default, other statii: optional, not allowed)
        // TODO: required layer (optional)

        public RelationTemplate(string predicateID, int? minCardinality, int? maxCardinality)
        {
            PredicateID = predicateID;
            //FromCITypes = fromCITypes,
            MinCardinality = minCardinality;
            MaxCardinality = maxCardinality;
        }
    }
}
