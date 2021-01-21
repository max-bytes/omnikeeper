using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tasks.Tools
{
    [Explicit]
    class BuildTraits
    {
        [Test]
        public void Build()
        {
            var traits = DefaultTraits.Get();
            var json = RecursiveTraitSet.Serializer.SerializeToString(traits);

            Console.WriteLine(json);
        }
    }

    public static class DefaultTraits
    {
        public static RecursiveTraitSet Get()
        {
            var traits = new RecursiveTrait[]
                {
                    // hosts
                    new RecursiveTrait("host", new List<TraitAttribute>() {
                        new TraitAttribute("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    new RecursiveTrait("host_windows", new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    new RecursiveTrait("host_linux", new List<TraitAttribute>() {
                        new TraitAttribute("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                new CIAttributeValueConstraintTextRegex(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    new RecursiveTrait("linux_block_device", new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("mount",
                            CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // linux network_interface
                    new RecursiveTrait("linux_network_interface", new List<TraitAttribute>() {
                        new TraitAttribute("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("type",
                            CIAttributeTemplate.BuildFromParams("type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("active",
                            CIAttributeTemplate.BuildFromParams("active", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // applications
                    new RecursiveTrait("application", new List<TraitAttribute>() {
                        new TraitAttribute("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    new RecursiveTrait("ansible_can_deploy_to_it",
                        new List<TraitAttribute>() {
                            new TraitAttribute("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                                CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },
                        new List<TraitAttribute>() {
                            new TraitAttribute("variables",
                                CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                            )
                        }
                        //new List<TraitRelation>() {
                        //    new TraitRelation("ansible_groups",
                        //        new RelationTemplate("has_ansible_group", 1, null)
                        //    )
                        //}
                    ),

                    
                    // timecontrol-timerecord
                    new RecursiveTrait("timecontrol-timerecord", new List<TraitAttribute>() {
                        new TraitAttribute("date",
                            CIAttributeTemplate.BuildFromParams("date", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("from",
                            CIAttributeTemplate.BuildFromParams("from", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("to",
                            CIAttributeTemplate.BuildFromParams("to", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("activity",
                            CIAttributeTemplate.BuildFromParams("activity", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("openproject_id",
                            CIAttributeTemplate.BuildFromParams("openproject_id", AttributeValueType.Integer, false)
                        ),
                        new TraitAttribute("location",
                            CIAttributeTemplate.BuildFromParams("location", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        new TraitAttribute("factor",
                            CIAttributeTemplate.BuildFromParams("factor", AttributeValueType.Integer, false)
                        ),
                        new TraitAttribute("billable",
                            CIAttributeTemplate.BuildFromParams("billable", AttributeValueType.Integer, false)
                        ),
                    }),
                };

            return RecursiveTraitSet.Build(traits);
        }
    }
}
