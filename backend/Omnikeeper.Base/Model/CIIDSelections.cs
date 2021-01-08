using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omnikeeper.Base.Model
{
    public interface ICIIDSelection
    {
        public bool Contains(Guid ciid);
    }

    public class SpecificCIIDsSelection : ICIIDSelection
    {
        public ISet<Guid> CIIDs { get; }
        private SpecificCIIDsSelection(ISet<Guid> ciids)
        {
            CIIDs = ciids;
        }

        public bool Contains(Guid ciid) => CIIDs.Contains(ciid);

        public static SpecificCIIDsSelection Build(ISet<Guid> ciids)
        {
            if (ciids.IsEmpty()) throw new Exception("Empty SpecificCIIDsSelection not allowed");
            return new SpecificCIIDsSelection(ciids);
        }
        public static SpecificCIIDsSelection Build(params Guid[] ciids)
        {
            if (ciids.IsEmpty()) throw new Exception("Empty SpecificCIIDsSelection not allowed");
            return new SpecificCIIDsSelection(ciids.ToHashSet());
        }
    }

    public class AllCIIDsSelection : ICIIDSelection
    {
        public bool Contains(Guid ciid) => true;
    }
}
