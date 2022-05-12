using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;

namespace OKPluginCLBNaemonVariableResolution
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IComputeLayerBrain, CLBNaemonVariableResolution>();
        }

        public override IEnumerable<RecursiveTrait> DefinedTraits => new List<RecursiveTrait>() {
            GenericTraitEntityHelper.Class2RecursiveTrait<NaemonV1Variable>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<TargetHost>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<TargetService>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<Profile>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<Category>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<Customer>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<Interface>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<ServiceAction>(),
            GenericTraitEntityHelper.Class2RecursiveTrait<Group>(),
        };
    }
}
