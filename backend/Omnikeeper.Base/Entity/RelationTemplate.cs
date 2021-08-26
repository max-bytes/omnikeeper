using ProtoBuf;

namespace Omnikeeper.Base.Entity
{
    [ProtoContract(SkipConstructor = true)]
    public class RelationTemplate
    {
        [ProtoMember(1)] public readonly string PredicateID;

        [ProtoMember(2)] public readonly int? MinCardinality;
        [ProtoMember(3)] public readonly int? MaxCardinality;

        // TODO: directionality: forward, backward, both

        public RelationTemplate(string predicateID, int? minCardinality, int? maxCardinality)
        {
            PredicateID = predicateID;
            MinCardinality = minCardinality;
            MaxCardinality = maxCardinality;
        }
    }
}
