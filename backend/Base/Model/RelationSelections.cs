using System;
using System.Collections.Generic;
using System.Text;

namespace Landscape.Base.Model
{
    public interface IRelationSelection
    {

    }
    public class RelationSelectionFromTo : IRelationSelection
    {
        public readonly Guid? fromCIID;
        public readonly Guid? toCIID;

        public RelationSelectionFromTo(Guid? fromCIID, Guid? toCIID)
        {
            this.fromCIID = fromCIID;
            this.toCIID = toCIID;
        }
    }
    public class RelationSelectionEitherFromOrTo : IRelationSelection
    {
        public readonly Guid ciid;

        public RelationSelectionEitherFromOrTo(Guid ciid)
        {
            this.ciid = ciid;
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
