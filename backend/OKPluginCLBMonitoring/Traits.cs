using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Plugins;
using Omnikeeper.Base.Service;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text;

namespace OKPluginCLBMonitoring
{
    public static class Traits
    {
        private static readonly TraitOriginV1 traitOrigin = PluginRegistrationBase.GetTraitOrigin(typeof(Traits).Assembly);

        public static readonly RecursiveTrait Module = new RecursiveTrait(null, "naemon_service_module", traitOrigin, new List<TraitAttribute>() {
            new TraitAttribute("template",
                CIAttributeTemplate.BuildFromParams("naemon.config_template", AttributeValueType.MultilineText, null, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });
        public static readonly GenericTrait ModuleFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Module);

        public static readonly RecursiveTrait NaemonInstance = new RecursiveTrait(null, "naemon_instance", traitOrigin, new List<TraitAttribute>() {
            new TraitAttribute("name",
                CIAttributeTemplate.BuildFromParams("naemon.instance_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        }, optionalAttributes: new List<TraitAttribute>()
        {
            new TraitAttribute("config",
                CIAttributeTemplate.BuildFromParams("naemon.config", AttributeValueType.JSON, true)
            ),
            new TraitAttribute("requirements",
                CIAttributeTemplate.BuildFromParams("naemon.requirements", AttributeValueType.Text, true, CIAttributeValueConstraintTextLength.Build(1, null))
            ),
            new TraitAttribute("capabilities",
                CIAttributeTemplate.BuildFromParams("naemon.capabilities", AttributeValueType.Text, true, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });
        public static readonly GenericTrait NaemonInstanceFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonInstance);

        public static readonly RecursiveTrait Contactgroup = new RecursiveTrait(null, "naemon_contactgroup", traitOrigin, new List<TraitAttribute>() {
            new TraitAttribute("name",
                CIAttributeTemplate.BuildFromParams("naemon.contactgroup_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });
        public static readonly GenericTrait ContactgroupFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Contactgroup);

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { Module, NaemonInstance, Contactgroup };
    }
}
