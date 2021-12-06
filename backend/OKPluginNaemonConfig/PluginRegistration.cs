using Microsoft.Extensions.DependencyInjection;
using OKPluginNaemonConfig.Entity;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using System.Collections.Generic;

namespace OKPluginNaemonConfig
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IComputeLayerBrain, NaemonConfig>();
            sc.AddSingleton<GenericTraitEntityModel<NaemonInstance, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Host, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Service, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Category, string>>();

            sc.AddSingleton<GenericTraitEntityModel<HostAction, string>>();
            sc.AddSingleton<GenericTraitEntityModel<ServiceAction, string>>();
            sc.AddSingleton<GenericTraitEntityModel<NaemonInstanceTag, string>>();
            sc.AddSingleton<GenericTraitEntityModel<NaemonProfile, string>>();
            sc.AddSingleton<GenericTraitEntityModel<TimePeriod, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Variable, string>>();
            sc.AddSingleton<GenericTraitEntityModel<ServiceLayer, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Command, string>>();
            sc.AddSingleton<GenericTraitEntityModel<ServiceStatic, long>>();
            sc.AddSingleton<GenericTraitEntityModel<Module, string>>();
            sc.AddSingleton<GenericTraitEntityModel<Interface, string>>();
            
        }

        //public override IEnumerable<RecursiveTrait> DefinedTraits => Traits.RecursiveTraits;
    }
}
