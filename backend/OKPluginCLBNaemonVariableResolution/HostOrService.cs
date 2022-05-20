using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OKPluginCLBNaemonVariableResolution
{
    public class HostOrService
    {
        private readonly TargetHost? host;
        private readonly TargetService? service;

        public HostOrService(TargetHost? host, TargetService? service)
        {
            this.host = host;
            this.service = service;
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
        public Guid? Customer => Get(h => h.Customer, s => s.Customer);
        public Guid? OSSupportGroup => Get(h => h.OSSupportGroup, s => s.SupportGroup); // TODO: is this correct for services?
        public Guid? AppSupportGroup => Get(h => h.AppSupportGroup, s => null); // TODO: is this correct for services?
        public string? Environment => Get(h => h.Environment, s => s.Environment);
        public string? Criticality => Get(h => h.Criticality, s => s.Criticality);
        public string? Instance => Get(h => h.Instance, s => s.Instance);
        public string? ForeignSource => Get(h => h.ForeignSource, s => s.ForeignSource);
        public string? ForeignKey => Get(h => h.ForeignKey, s => s.ForeignKey);
        public string? Location => Get(h => h.Location, s => null);
        public string? OS => Get(h => h.OS, s => null);
        public string? Platform => Get(h => h.Platform, s => null);
        public string? MonIPAddress => Get(h => h.MonIPAddress, s => null);
        public string? MonIPPort => Get(h => h.MonIPPort, s => null);

        public TargetHost? Host => host;
        public TargetService? Service => service;
    }
}
