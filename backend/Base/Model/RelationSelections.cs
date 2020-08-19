using System;
using System.Collections.Generic;
using System.Text;

namespace Landscape.Base.Model
{
    public interface IRelationSelection
    {
        public string ToHashKey();
    }
    public class RelationSelectionFrom : IRelationSelection
    {
        public readonly Guid fromCIID;

        public RelationSelectionFrom(Guid fromCIID)
        {
            this.fromCIID = fromCIID;
        }

        public string ToHashKey() => $"rsf_{fromCIID}";
    }
    public class RelationSelectionEitherFromOrTo : IRelationSelection
    {
        public readonly Guid ciid;

        public RelationSelectionEitherFromOrTo(Guid ciid)
        {
            this.ciid = ciid;
        }
        public string ToHashKey() => $"efot_{ciid}";
    }
    public class RelationSelectionWithPredicate : IRelationSelection
    {
        public readonly string predicateID;

        public RelationSelectionWithPredicate(string predicateID)
        {
            this.predicateID = predicateID;
        }
        public string ToHashKey() => $"p_{predicateID}";
    }
    public class RelationSelectionAll : IRelationSelection
    {
        public string ToHashKey() => $"all";
    }

}
