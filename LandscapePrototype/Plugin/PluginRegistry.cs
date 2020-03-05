using Landscape.Base;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Plugin
{
    public class PluginRegistry : IPluginRegistry
    {
        private readonly IDictionary<string, IComputeLayerBrain> ComputeLayerBrains = new Dictionary<string, IComputeLayerBrain>();

        public void RegisterComputeLayerBrains(IEnumerable<IComputeLayerBrain> brains)
        {
            foreach (var brain in brains)
                ComputeLayerBrains.Add(brain.GetType().FullName, brain);
        } 
    }
}
