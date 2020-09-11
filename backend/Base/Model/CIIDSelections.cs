using Landscape.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Landscape.Base.Model
{
    public interface ICIIDSelection
    {
        public bool Contains(Guid ciid);
    }

    public class SpecificCIIDsSelection : ICIIDSelection
    {
        public Guid[] CIIDs { get; }
        private SpecificCIIDsSelection(IEnumerable<Guid> ciids)
        {
            CIIDs = ciids.ToArray();
        }

        public bool Contains(Guid ciid) => CIIDs.Contains(ciid);

        public static SpecificCIIDsSelection Build(IEnumerable<Guid> ciids)
        {
            if (ciids.IsEmpty()) throw new Exception("Empty SpecificCIIDsSelection not allowed");
            return new SpecificCIIDsSelection(ciids);
        }
        public static SpecificCIIDsSelection Build(params Guid[] ciids)
        {
            if (ciids.IsEmpty()) throw new Exception("Empty SpecificCIIDsSelection not allowed");
            return new SpecificCIIDsSelection(ciids);
        }
    }

    public class AllCIIDsSelection : ICIIDSelection
    {
        public bool Contains(Guid ciid) => true;
    }
}
