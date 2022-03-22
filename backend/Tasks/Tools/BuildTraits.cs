using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;

namespace Tasks.Tools
{
    [Explicit]
    public class BuildTraits
    {
        [Test]
        public void Build()
        {
            var traits = new RecursiveTrait[]
                {
                    // hosts
                    //new RecursiveTrait("host", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    //    new TraitAttribute("hostname",
                    //        CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    )
                    //}),
                    //new RecursiveTrait("host_windows", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    //    new TraitAttribute("os_family",
                    //        CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                    //            new CIAttributeValueConstraintTextRegex(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                    //    )
                    //}, requiredTraits: new string[] { "host" }),

                    //new RecursiveTrait("host_linux", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    //    new TraitAttribute("os_family",
                    //        CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                    //            new CIAttributeValueConstraintTextRegex(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                    //    )
                    //}, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    //new RecursiveTrait("linux_block_device", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    //    new TraitAttribute("device",
                    //        CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    ),
                    //    new TraitAttribute("mount",
                    //        CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    )
                    //}),

                    //// linux network_interface
                    //new RecursiveTrait("linux_network_interface", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    //    new TraitAttribute("device",
                    //        CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    ),
                    //    new TraitAttribute("type",
                    //        CIAttributeTemplate.BuildFromParams("type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    ),
                    //    new TraitAttribute("active",
                    //        CIAttributeTemplate.BuildFromParams("active", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    )
                    //}),

                    // applications
                    //new RecursiveTrait("application", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    //    new TraitAttribute("name",
                    //        CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //    )
                    //}),

                    // automation / ansible
                    //new RecursiveTrait("ansible_can_deploy_to_it", new TraitOriginV1(TraitOriginType.Data),
                    //    new List<TraitAttribute>() {
                    //        new TraitAttribute("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                    //            CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    //        )
                    //    },
                    //    new List<TraitAttribute>() {
                    //        new TraitAttribute("variables",
                    //            CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                    //        )
                    //    }
                    //    //new List<TraitRelation>() {
                    //    //    new TraitRelation("ansible_groups",
                    //    //        new RelationTemplate("has_ansible_group", 1, null)
                    //    //    )
                    //    //}
                    //),


                    // TSA CMDB
                    //new RecursiveTrait("tsa_cmdb_host", new TraitOriginV1(TraitOriginType.Data),
                    //    new List<TraitAttribute>() {
                    //        new TraitAttribute("hostid",
                    //            CIAttributeTemplate.BuildFromParams("cmdb.hostid", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null))
                    //        )
                    //    }
                    //),
                    //new RecursiveTrait("tsa_cmdb_service", new TraitOriginV1(TraitOriginType.Data),
                    //    new List<TraitAttribute>() {
                    //        new TraitAttribute("svcid",
                    //            CIAttributeTemplate.BuildFromParams("cmdb.svcid", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null))
                    //        )
                    //    }
                    //),
                    //new RecursiveTrait("tsa_cmdb_interface", new TraitOriginV1(TraitOriginType.Data),
                    //    new List<TraitAttribute>() {
                    //        new TraitAttribute("ifid",
                    //            CIAttributeTemplate.BuildFromParams("cmdb.ifid", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null))
                    //        )
                    //    }
                    //),

                    new RecursiveTrait("trait_with_relation", new TraitOriginV1(TraitOriginType.Data),
                        new List<TraitAttribute>() {
                            new TraitAttribute("hostid",
                                CIAttributeTemplate.BuildFromParams("cmdb.host.id", AttributeValueType.Text, false, true, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },optionalRelations: new List<TraitRelation>()
                        {
                            new TraitRelation("interfaces",
                                new RelationTemplate("has_interface", true)
                            )
                        }
                    ),
                };

            foreach (var trait in traits)
            {
                var s = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.Objects
                };
                s.Converters.Add(new StringEnumConverter());
                Console.WriteLine(JsonConvert.SerializeObject(trait, Formatting.Indented, s));

            }
        }
    }
}
