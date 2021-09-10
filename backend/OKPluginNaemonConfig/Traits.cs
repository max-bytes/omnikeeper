using Omnikeeper.Base.Entity;
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

        public static readonly RecursiveTrait NaemonInstance = new RecursiveTrait("naemon_instance", traitOrigin, new List<TraitAttribute>() {
            new TraitAttribute("name",
                CIAttributeTemplate.BuildFromParams("monman-instance.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonInstanceFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonInstance);

        public static readonly RecursiveTrait HCis = new RecursiveTrait("hosts", traitOrigin, new List<TraitAttribute>() { 
        new TraitAttribute("hostname",
            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait HCisFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(HCis);

        public static readonly RecursiveTrait ACis = new RecursiveTrait("services", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("cmdb.name",
            CIAttributeTemplate.BuildFromParams("cmdb.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait ACisFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ACis);

        public static readonly RecursiveTrait NaemonModules = new RecursiveTrait("modules", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("name",
            CIAttributeTemplate.BuildFromParams("monman-module.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonModulesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonModules);

        public static readonly RecursiveTrait NaemonProfiles = new RecursiveTrait("profiles", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("name",
            CIAttributeTemplate.BuildFromParams("monman-profile.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonProfilesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonProfiles);

        public static readonly RecursiveTrait NaemonInstancesTags = new RecursiveTrait("instances_tags", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("name",
            CIAttributeTemplate.BuildFromParams("monman-instance_tag.tag", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonInstancesTagsFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonInstancesTags);

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() { NaemonInstance, HCis, ACis, NaemonModules, NaemonProfiles, NaemonInstancesTags };
    }
}
