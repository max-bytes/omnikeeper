using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginNaemonConfig
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IComputeLayerBrain, NamonConfig>();
        }

        //public override IEnumerable<RecursiveTrait> DefinedTraits => Traits.RecursiveTraits;
    }
}
