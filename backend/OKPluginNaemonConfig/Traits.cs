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
            new TraitAttribute("naemon_instance.id",
                CIAttributeTemplate.BuildFromParams("naemon_instance.id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonInstanceFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonInstance);

        public static readonly RecursiveTrait HCis = new RecursiveTrait("host", traitOrigin, new List<TraitAttribute>() {
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

        public static readonly RecursiveTrait NaemonModules = new RecursiveTrait("module", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("name",
            CIAttributeTemplate.BuildFromParams("naemon_module.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });
        public static readonly GenericTrait NaemonModulesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonModules);

        public static readonly RecursiveTrait HostsCategories = new RecursiveTrait("hosts_category", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("cmdb.host_category_categoryid",
            CIAttributeTemplate.BuildFromParams("cmdb.host_category_categoryid", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait HostsCategoriesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(HostsCategories);

        public static readonly RecursiveTrait ServicesCategories = new RecursiveTrait("services_category", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("cmdb.service_category_categoryid",
            CIAttributeTemplate.BuildFromParams("cmdb.service_category_categoryid", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait ServicesCategoriesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ServicesCategories);

        public static readonly RecursiveTrait HostActions = new RecursiveTrait("host_action", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("cmdb.host_action_id",
            CIAttributeTemplate.BuildFromParams("cmdb.host_action_id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait HostActionsFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(HostActions);

        public static readonly RecursiveTrait ServiceActions = new RecursiveTrait("service_action", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("cmdb.service_action_id",
            CIAttributeTemplate.BuildFromParams("cmdb.service_action_id", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait ServiceActionsFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ServiceActions);

        public static readonly RecursiveTrait Interfaces = new RecursiveTrait("interface", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("cmdb.ifid",
            CIAttributeTemplate.BuildFromParams("cmdb.ifid", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait InterfacesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ServiceActions);

        public static readonly RecursiveTrait NaemonProfiles = new RecursiveTrait("profile", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("naemon_profile.name",
            CIAttributeTemplate.BuildFromParams("naemon_profile.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonProfilesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonProfiles);

        public static readonly RecursiveTrait NaemonInstancesTags = new RecursiveTrait("instances_tag", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("naemon_instance_tag.tag",
            CIAttributeTemplate.BuildFromParams("naemon_instance_tag.tag", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait NaemonInstancesTagsFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(NaemonInstancesTags);

        public static readonly RecursiveTrait Commands = new RecursiveTrait("command", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("naemon_command.name",
            CIAttributeTemplate.BuildFromParams("naemon_command.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait CommandsFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Commands);

        // monman-timeperiod.id
        public static readonly RecursiveTrait TimePeriods = new RecursiveTrait("timeperiod", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("naemon_timeperiod.name",
            CIAttributeTemplate.BuildFromParams("naemon_timeperiod.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait TimePeriodsFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(TimePeriods);

        public static readonly RecursiveTrait Variables = new RecursiveTrait("variable", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("naemon_variable.type",
            CIAttributeTemplate.BuildFromParams("naemon_variable.type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait VariablesFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(Variables);

        public static readonly RecursiveTrait ServiceLayers = new RecursiveTrait("service_layer", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("name",
            CIAttributeTemplate.BuildFromParams("naemon_service_layer.name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait ServiceLayersFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ServiceLayers);

        public static readonly RecursiveTrait ServicesStatic = new RecursiveTrait("service_static", traitOrigin, new List<TraitAttribute>() {
        new TraitAttribute("name",
            CIAttributeTemplate.BuildFromParams("naemon_services_static.servicename", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
            )
        });

        public static readonly GenericTrait ServicesStaticFlattened = RecursiveTraitService.FlattenSingleRecursiveTrait(ServicesStatic);

        public static readonly IEnumerable<RecursiveTrait> RecursiveTraits = new List<RecursiveTrait>() 
        { 
            HCis, 
            ACis, 
            HostsCategories, 
            ServicesCategories, 
            HostActions,
            ServiceActions,
            Interfaces,
            NaemonInstance,
            NaemonModules, 
            NaemonProfiles, 
            NaemonInstancesTags, 
            Commands, 
            TimePeriods,
            Variables,
            ServiceLayers,
            ServicesStatic,
        };
    }
}
