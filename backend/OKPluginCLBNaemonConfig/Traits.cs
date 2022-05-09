using OKPluginNaemonConfig.Entity;
using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model.TraitBased;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginNaemonConfig
{
    public static class Traits
    {
        private static readonly TraitOriginV1 traitOrigin = PluginRegistrationBase.GetTraitOrigin(typeof(Traits).Assembly);

        public static readonly RecursiveTrait NaemonInstance = GenericTraitEntityHelper.Class2RecursiveTrait<NaemonInstance>();

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { NaemonInstance };
        //public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() 
        //{ 
        //    HCis, 
        //    ACis, 
        //    HostsCategories, 
        //    ServicesCategories, 
        //    HostActions,
        //    ServiceActions,
        //    Interfaces,
        //    NaemonInstance,
        //    NaemonModules, 
        //    NaemonProfiles, 
        //    NaemonInstancesTags, 
        //    Commands, 
        //    TimePeriods,
        //    Variables,
        //    ServiceLayers,
        //    ServicesStatic,
        //};
    }
}
