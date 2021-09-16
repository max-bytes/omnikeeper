using System;

namespace Omnikeeper.Base.Model
{
    public interface IRelationSelection
    {
    }
    public class RelationSelectionFrom : IRelationSelection
    {
        public readonly Guid[] fromCIIDs;

        public RelationSelectionFrom(params Guid[] fromCIIDs)
        {
            this.fromCIIDs = fromCIIDs;
        }
    }
    public class RelationSelectionTo : IRelationSelection
    {
        public readonly Guid[] toCIIDs;

        public RelationSelectionTo(params Guid[] toCIIDs)
        {
            this.toCIIDs = toCIIDs;
        }
    }
    public class RelationSelectionWithPredicate : IRelationSelection
    {
        public readonly string predicateID;

        public RelationSelectionWithPredicate(string predicateID)
        {
            this.predicateID = predicateID;
        }
    }
    public class RelationSelectionAll : IRelationSelection
    {
    }

}
