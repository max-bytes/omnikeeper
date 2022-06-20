using System.Text.RegularExpressions;

namespace OKPluginCLBNaemonVariableResolution
{
    public class HostOrService
    {
        private readonly TargetHost? host;
        private readonly TargetService? service;

        public HostOrService(TargetHost? host, TargetService? service, List<string> profiles, List<Category> categories, Customer customer)
        {
            this.host = host;
            this.service = service;
            Customer = customer;
            Profiles = profiles;
            Categories = categories;
            Variables = new SortedDictionary<string, List<Variable>>();
            Tags = new HashSet<string>();
            UseDirective = new List<string>();
        }

        private R Get<R>(Func<TargetHost, R> hostF, Func<TargetService, R> serviceF)
        {
            if (host != null) return hostF(host);
            else return serviceF(service!);
        }

        public string? Status => Get(h => h.Status, s => s.Status);
        public Guid[] MemberOfCategories => Get(h => h.MemberOfCategories, s => s.MemberOfCategories);
        public string ID => Get(h => h.ID, s => s.ID);
        public string? Name => Get(h => h.Hostname, s => s.Name);
        public Guid? CustomerCIID => Get(h => h.Customer, s => s.Customer);
        public Guid? OSSupportGroup => Get(h => h.OSSupportGroup, s => s.OSSupportGroup);
        public Guid? AppSupportGroup => Get(h => h.AppSupportGroup, s => s.AppSupportGroup);
        public string? Environment => Get(h => h.Environment, s => s.Environment);
        public string? Criticality => Get(h => h.Criticality, s => s.Criticality);
        public string? Instance => Get(h => h.Instance, s => s.Instance);
        public string? ForeignSource => Get(h => h.ForeignSource, s => s.ForeignSource);
        public string? ForeignKey => Get(h => h.ForeignKey, s => s.ForeignKey);
        public string? Location => Get(h => h.Location, s => "00EMPTY"); // TODO: services have locations too
        public string? OS => Get(h => h.OS, s => null);
        public string? Platform => Get(h => h.Platform, s => null);
        public string? MonIPAddress => Get(h => h.MonIPAddress, s => s.MonIPAddress);
        public string? MonIPPort => Get(h => h.MonIPPort, s => s.MonIPPort);

        public TargetHost? Host => host;
        public TargetService? Service => service;

        // additional data
        public List<string> Profiles { get; set; }
        public List<Category> Categories { get; }
        public Customer Customer { get; }
        public SortedDictionary<string, List<Variable>> Variables { get; }

        public HashSet<string> Tags {get; }
        public List<string> UseDirective { get; set; }

        private static VariableComparer VariableComparer = new VariableComparer();

        // helper methods
        public void AddVariable(Variable v)
        {
            if (Variables.TryGetValue(v.Name, out var l))
            {
                int x = l.BinarySearch(v, VariableComparer);
                l.Insert((x >= 0) ? x : ~x, v); // taken from https://stackoverflow.com/a/46294791
            }
            else
            {
                Variables.Add(v.Name, new List<Variable>() { v });
            }
        }
        public void AddVariables(params Variable[] variables)
        {
            foreach (var vv in variables)
                AddVariable(vv);
        }
        public string? GetVariableValue(string name)
        {
            if (Variables.TryGetValue(name, out var list))
                return list.FirstOrDefault()?.Value;
            return null;
        }
        public bool HasProfile(StringComparison stringComparison, string profile) => Profiles.Any(p => p.Equals(profile, stringComparison));
        public bool HasProfileMatchingRegex(string pattern, RegexOptions regexOptions = RegexOptions.None) => Profiles.Any(p => Regex.IsMatch(p, pattern, regexOptions));
        public bool HasAnyProfileOf(StringComparison stringComparison, params string[] profiles) => Profiles.Any(pp => profiles.Any(p => p.Equals(pp, stringComparison)));
    }


    class VariableComparer : IComparer<Variable>
    {
        public int Compare(Variable? v1, Variable? v2)
        {
            if (v1 == null && v2 == null) return 0;
            if (v1 == null) return -1;
            if (v2 == null) return 1;
            if (v1.RefType == v2.RefType)
            {
                if (v1.Precedence > v2.Precedence)
                    return -1;
                else if (v1.Precedence < v2.Precedence)
                    return 1;
                else
                {
                    // we cannot sort "naturally", use the id as the final decider
                    if (v1.ExternalID < v2.ExternalID)
                        return -1;
                    else
                        return 1;
                }
            }
            else
            {
                var refType1 = RefType2Int(v1.RefType);
                var refType2 = RefType2Int(v2.RefType);
                if (refType1 < refType2)
                    return -1;
                else
                    return 1;
            }
        }
        private static int RefType2Int(string refType)
        {
            return refType switch
            {
                "FIXED" => -1,
                "CI" => 0,
                "PROFILE" => 1,
                "CUST" => 2,
                "GLOBAL" => 3,
                "INIT" => 4,
                _ => 5, // must not happen, other refTypes should have been filtered out by now
            };
        }
    }

    public class Variable
    {
        public readonly string Name;
        public readonly string Value;
        public readonly string RefType;
        public readonly long Precedence;
        public readonly long ExternalID;

        public Variable(string name, string refType, string value, long precedence = 0, long externalID = 0L)
        {
            Name = name;
            Value = value;
            RefType = refType;
            Precedence = precedence;
            ExternalID = externalID;
        }
    }
}
