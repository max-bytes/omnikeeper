using Microsoft.Extensions.DependencyInjection;
using Omnikeeper.Base.CLB;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Plugins;
using System.Collections.Generic;

namespace OKPluginCLBDummy
{
    public class PluginRegistration : PluginRegistrationBase
    {
        public override void RegisterServices(IServiceCollection sc)
        {
            sc.AddSingleton<IComputeLayerBrain, CLBDummy>();
        }
    }
}
