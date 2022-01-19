
namespace Omnikeeper.Base.Entity
{
    //[ProtoContract(SkipConstructor = true)]
    public class RelationTemplate
    {
        //[ProtoMember(1)] 
        public readonly string PredicateID;

        //[ProtoMember(2)] 
        public readonly bool DirectionForward;

        //[ProtoMember(3)] 
        public readonly int? MinCardinality;
        //[ProtoMember(4)] 
        public readonly int? MaxCardinality;

        public RelationTemplate(string predicateID, bool directionForward, int? minCardinality, int? maxCardinality)
        {
            PredicateID = predicateID;
            DirectionForward = directionForward;
            MinCardinality = minCardinality;
            MaxCardinality = maxCardinality;
        }
    }
}
