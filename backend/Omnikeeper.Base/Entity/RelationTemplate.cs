using ProtoBuf;
using System;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class RelationTemplate
    {
        [ProtoMember(1)] public readonly string PredicateID;
        // TODO: description?
        //public CIType[] FromCITypes { get; private set; }
        //public string[] ToCITypeIDs { get; private set; }

        [ProtoMember(2)] public readonly int? MinCardinality;
        [ProtoMember(3)] public readonly int? MaxCardinality;

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
