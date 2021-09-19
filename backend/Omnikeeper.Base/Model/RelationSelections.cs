using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface IRelationSelection
    {
    }
    public class RelationSelectionFrom : IRelationSelection
    {
        public readonly Guid[] fromCIIDs;

        public RelationSelectionFrom(params Guid[] fromCIIDs) // TODO: why force array? couldn't stay at IEnumerable and stay lazy?
        {
            this.fromCIIDs = fromCIIDs;
        }
    }
    public class RelationSelectionTo : IRelationSelection
    {
        public readonly Guid[] toCIIDs;

        public RelationSelectionTo(params Guid[] toCIIDs) // TODO: why force array? couldn't stay at IEnumerable and stay lazy?
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
    //public class RelationSelectionOr : IRelationSelection
    //{
    //    public readonly IEnumerable<IRelationSelection> inners;

    //    public RelationSelectionOr(IEnumerable<IRelationSelection> inners)
    //    {
    //        this.inners = inners;
    //    }
    //}
    //public class RelationSelectionAnd : IRelationSelection
    //{
    //    public readonly IEnumerable<IRelationSelection> inners;

    //    public RelationSelectionAnd(IEnumerable<IRelationSelection> inners)
    //    {
    //        this.inners = inners;
    //    }
    //}

    //public static class RelationSelectionExtensions
    //{
    //    public static IRelationSelection UnionAll(IEnumerable<IRelationSelection> selections)
    //    {
    //        return new RelationSelectionOr(selections); // TODO: simplify
    //    }
    //}

}
