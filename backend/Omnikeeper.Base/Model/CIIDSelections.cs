using Omnikeeper.Base.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public class AllCIIDsExceptSelection : ICIIDSelection
    {
        public ISet<Guid> ExceptCIIDs { get; }
        private AllCIIDsExceptSelection(ISet<Guid> ciids)
        {
            ExceptCIIDs = ciids;
        }

        public bool Contains(Guid ciid) => !ExceptCIIDs.Contains(ciid);

        public static AllCIIDsExceptSelection Build(ISet<Guid> ciids)
        {
            if (ciids.IsEmpty()) throw new Exception("Empty except list for AllCIIDsExceptSelection not allowed");
            return new AllCIIDsExceptSelection(ciids);
        }
        public static AllCIIDsExceptSelection Build(params Guid[] ciids)
        {
            if (ciids.IsEmpty()) throw new Exception("Empty except list for AllCIIDsExceptSelection not allowed");
            return new AllCIIDsExceptSelection(ciids.ToHashSet());
        }
    }

    public class AllCIIDsSelection : ICIIDSelection
    {
        public bool Contains(Guid ciid) => true;
    }

    public static class CIIDSelectionExtensions
    {
        public static IEnumerable<Guid> GetCIIDs(this ICIIDSelection selection, Func<IEnumerable<Guid>> getAllCIIDs)
        {
            return selection switch
            {
                AllCIIDsSelection _ => getAllCIIDs(),
                SpecificCIIDsSelection specific => specific.CIIDs,
                AllCIIDsExceptSelection allExcept => getAllCIIDs().Except(allExcept.ExceptCIIDs),
                _ => throw new NotImplementedException()
            };
        }
        public static async Task<IEnumerable<Guid>> GetCIIDsAsync(this ICIIDSelection selection, Func<Task<IEnumerable<Guid>>> getAllCIIDs)
        {
            return selection switch
            {
                AllCIIDsSelection _ => await getAllCIIDs(),
                SpecificCIIDsSelection specific => specific.CIIDs,
                AllCIIDsExceptSelection allExcept => (await getAllCIIDs()).Except(allExcept.ExceptCIIDs),
                _ => throw new NotImplementedException()
            };
        }
    }

}
