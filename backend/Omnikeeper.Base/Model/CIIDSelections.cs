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

        public static ICIIDSelection Build(ISet<Guid> ciids)
        {
            if (ciids.IsEmpty()) return new NoCIIDsSelection();
            return new SpecificCIIDsSelection(ciids);
        }
        public static ICIIDSelection Build(params Guid[] ciids)
        {
            if (ciids.IsEmpty()) return new NoCIIDsSelection();
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

    public class NoCIIDsSelection : ICIIDSelection
    {
        public bool Contains(Guid ciid) => false;
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
                NoCIIDsSelection _ => new Guid[0],
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
                NoCIIDsSelection _ => new Guid[0],
                _ => throw new NotImplementedException()
            };
        }
        public static async Task<int> CountAsync(this ICIIDSelection selection, Func<Task<IEnumerable<Guid>>> getAllCIIDs)
        {
            return selection switch
            {
                AllCIIDsSelection _ => (await getAllCIIDs()).Count(),
                SpecificCIIDsSelection specific => specific.CIIDs.Count,
                AllCIIDsExceptSelection allExcept => (await getAllCIIDs()).Count() - allExcept.ExceptCIIDs.Count,
                NoCIIDsSelection _ => 0,
                _ => throw new NotImplementedException()
            };
        }

        // TODO: write tests
        public static ICIIDSelection Except(this ICIIDSelection selection, ICIIDSelection other)
        {
            return selection switch
            {
                AllCIIDsSelection _ => other switch
                {
                    AllCIIDsSelection _ => new NoCIIDsSelection(),
                    SpecificCIIDsSelection specific => AllCIIDsExceptSelection.Build(specific.CIIDs),
                    AllCIIDsExceptSelection allExcept => SpecificCIIDsSelection.Build(allExcept.ExceptCIIDs),
                    NoCIIDsSelection _ => selection,
                    _ => throw new NotImplementedException()
                },
                SpecificCIIDsSelection s => other switch
                {
                    AllCIIDsSelection _ => new NoCIIDsSelection(),
                    SpecificCIIDsSelection specific => SpecificCIIDsSelection.Build(s.CIIDs.Except(specific.CIIDs).ToHashSet()),
                    AllCIIDsExceptSelection allExcept => SpecificCIIDsSelection.Build(s.CIIDs.Intersect(allExcept.ExceptCIIDs).ToHashSet()),
                    NoCIIDsSelection _ => selection,
                    _ => throw new NotImplementedException()
                },
                AllCIIDsExceptSelection e => other switch
                {
                    AllCIIDsSelection _ => new NoCIIDsSelection(),
                    SpecificCIIDsSelection specific => AllCIIDsExceptSelection.Build(e.ExceptCIIDs.Union(specific.CIIDs).ToHashSet()),
                    AllCIIDsExceptSelection allExcept => SpecificCIIDsSelection.Build(allExcept.ExceptCIIDs.Except(e.ExceptCIIDs).ToHashSet()),
                    NoCIIDsSelection _ => selection,
                    _ => throw new NotImplementedException()
                },
                NoCIIDsSelection _ => other switch
                {
                    AllCIIDsSelection _ => selection,
                    SpecificCIIDsSelection _ => selection,
                    AllCIIDsExceptSelection _ => selection,
                    NoCIIDsSelection _ => selection,
                    _ => throw new NotImplementedException()
                },
                _ => throw new NotImplementedException(),
            };
        }

        // TODO: write tests
        public static ICIIDSelection Intersect(this ICIIDSelection selection, ICIIDSelection other)
        {
            return selection switch
            {
                AllCIIDsSelection _ => other switch
                {
                    AllCIIDsSelection _ => selection,
                    SpecificCIIDsSelection specific => specific,
                    AllCIIDsExceptSelection allExcept => allExcept,
                    NoCIIDsSelection n => n,
                    _ => throw new NotImplementedException()
                },
                SpecificCIIDsSelection s => other switch
                {
                    AllCIIDsSelection _ => s,
                    SpecificCIIDsSelection specific => SpecificCIIDsSelection.Build(s.CIIDs.Intersect(specific.CIIDs).ToHashSet()),
                    AllCIIDsExceptSelection allExcept => SpecificCIIDsSelection.Build(s.CIIDs.Except(allExcept.ExceptCIIDs).ToHashSet()),
                    NoCIIDsSelection n => n,
                    _ => throw new NotImplementedException()
                },
                AllCIIDsExceptSelection e => other switch
                {
                    AllCIIDsSelection _ => e,
                    SpecificCIIDsSelection specific => SpecificCIIDsSelection.Build(specific.CIIDs.Except(e.ExceptCIIDs).ToHashSet()),
                    AllCIIDsExceptSelection allExcept => SpecificCIIDsSelection.Build(allExcept.ExceptCIIDs.Union(e.ExceptCIIDs).ToHashSet()),
                    NoCIIDsSelection n => n,
                    _ => throw new NotImplementedException()
                },
                NoCIIDsSelection _ => other switch
                {
                    AllCIIDsSelection _ => selection,
                    SpecificCIIDsSelection _ => selection,
                    AllCIIDsExceptSelection _ => selection,
                    NoCIIDsSelection _ => selection,
                    _ => throw new NotImplementedException()
                },
                _ => throw new NotImplementedException(),
            };
        }
    }

}
