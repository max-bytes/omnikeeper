using System;
using System.Collections.Generic;
using System.Text;

namespace Landscape.Base.Model
{
    public interface IRelationSelection
    {

    }
    public class RelationSelectionFrom : IRelationSelection
    {
        public readonly Guid fromCIID;

        public RelationSelectionFrom(Guid fromCIID)
        {
            this.fromCIID = fromCIID;
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
