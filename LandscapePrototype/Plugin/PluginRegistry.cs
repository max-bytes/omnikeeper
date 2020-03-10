//using Landscape.Base;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Plugin
//{
//    public class PluginRegistry : IPluginRegistry
//    {
//        private readonly IDictionary<string, IComputeLayerBrain> computeLayerBrains = new Dictionary<string, IComputeLayerBrain>();

//        public void RegisterComputeLayerBrains(IEnumerable<IComputeLayerBrain> brains)
//        {
//            foreach (var brain in brains)
//                computeLayerBrains.Add(brain.Name, brain);
//        }

//        public IDictionary<string, IComputeLayerBrain> ComputeLayerBrains { get => computeLayerBrains; }
//    }
//}
