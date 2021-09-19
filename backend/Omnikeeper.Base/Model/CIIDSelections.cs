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

    public class SpecificCIIDsSelection : ICIIDSelection, IEquatable<SpecificCIIDsSelection>
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
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                foreach (var ciid in CIIDs)
                    hash = (hash * 16777619) ^ ciid.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as SpecificCIIDsSelection);
        public bool Equals(SpecificCIIDsSelection? other) => other != null && CIIDs.SetEquals(other.CIIDs);
    }

    public class AllCIIDsExceptSelection : ICIIDSelection, IEquatable<AllCIIDsExceptSelection>
    {
        public ISet<Guid> ExceptCIIDs { get; }
        private AllCIIDsExceptSelection(ISet<Guid> ciids)
        {
            ExceptCIIDs = ciids;
        }

        public bool Contains(Guid ciid) => !ExceptCIIDs.Contains(ciid);

        public static ICIIDSelection Build(ISet<Guid> ciids)
        {
            if (ciids.IsEmpty()) return new AllCIIDsSelection();
            return new AllCIIDsExceptSelection(ciids);
        }
        public static ICIIDSelection Build(params Guid[] ciids)
        {
            if (ciids.IsEmpty()) return new AllCIIDsSelection();
            return new AllCIIDsExceptSelection(ciids.ToHashSet());
        }
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = (int)2166136261;
                foreach (var ciid in ExceptCIIDs)
                    hash = (hash * 16777619) ^ ciid.GetHashCode();
                return hash;
            }
        }
        public override bool Equals(object? obj) => Equals(obj as AllCIIDsExceptSelection);
        public bool Equals(AllCIIDsExceptSelection? other) => other != null && ExceptCIIDs.SetEquals(other.ExceptCIIDs);
    }

    public class AllCIIDsSelection : ICIIDSelection, IEquatable<AllCIIDsSelection>
    {
        public bool Contains(Guid ciid) => true;

        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => Equals(obj as AllCIIDsSelection);
        public bool Equals(AllCIIDsSelection? other) => other != null;
    }

    public class NoCIIDsSelection : ICIIDSelection, IEquatable<NoCIIDsSelection>
    {
        public bool Contains(Guid ciid) => false;

        public override int GetHashCode() => 0;
        public override bool Equals(object? obj) => Equals(obj as NoCIIDsSelection);
        public bool Equals(NoCIIDsSelection? other) => other != null;
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

        public static ICIIDSelection UnionAll(IEnumerable<ICIIDSelection> selections)
        {
            var specific = new HashSet<Guid>();
            HashSet<Guid>? allExcept = null;
            foreach(var selection in selections)
            {
                switch (selection)
                {
                    case AllCIIDsSelection a:
                        return a;
                    case SpecificCIIDsSelection s:
                        specific.UnionWith(s.CIIDs);
                        break;
                    case NoCIIDsSelection _:
                        break;
                    case AllCIIDsExceptSelection e:
                        if (allExcept == null)
                            allExcept = new HashSet<Guid>(e.ExceptCIIDs);
                        else
                            allExcept.IntersectWith(e.ExceptCIIDs);
                        break;
                }
            }

            if (allExcept != null)
            {
                return AllCIIDsExceptSelection.Build(allExcept.Except(specific).ToArray());
            } else
            {
                return SpecificCIIDsSelection.Build(specific);
            }
        }

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
                    AllCIIDsExceptSelection allExcept => AllCIIDsExceptSelection.Build(allExcept.ExceptCIIDs.Union(e.ExceptCIIDs).ToHashSet()),
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
