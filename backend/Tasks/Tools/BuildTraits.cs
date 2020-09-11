using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using NUnit.Framework;
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
            var json = TraitsProvider.TraitSetSerializer.SerializeToString(traits);

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
                    RecursiveTrait.Build("host", new List<TraitAttribute>() {
                        TraitAttribute.Build("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    RecursiveTrait.Build("windows_host", new List<TraitAttribute>() {
                        TraitAttribute.Build("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                CIAttributeValueConstraintTextRegex.Build(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    RecursiveTrait.Build("linux_host", new List<TraitAttribute>() {
                        TraitAttribute.Build("os_family",
                            CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                                CIAttributeValueConstraintTextRegex.Build(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                        )
                    }, requiredTraits: new string[] { "host" }),

                    // linux disk devices
                    RecursiveTrait.Build("linux_block_device", new List<TraitAttribute>() {
                        TraitAttribute.Build("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("mount",
                            CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // linux network_interface
                    RecursiveTrait.Build("linux_network_interface", new List<TraitAttribute>() {
                        TraitAttribute.Build("device",
                            CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("type",
                            CIAttributeTemplate.BuildFromParams("type", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("active",
                            CIAttributeTemplate.BuildFromParams("active", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // applications
                    RecursiveTrait.Build("application", new List<TraitAttribute>() {
                        TraitAttribute.Build("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    RecursiveTrait.Build("ansible_can_deploy_to_it",
                        new List<TraitAttribute>() {
                            TraitAttribute.Build("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
                                CIAttributeTemplate.BuildFromParams("ipAddress",    AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        },
                        new List<TraitAttribute>() {
                            TraitAttribute.Build("variables",
                                CIAttributeTemplate.BuildFromParams("automation.ansible_variables", AttributeValueType.JSON, false)
                            )
                        },
                        new List<TraitRelation>() {
                            TraitRelation.Build("ansible_groups",
                                RelationTemplate.Build("has_ansible_group", 1, null)
                            )
                        }
                    ),
                };

            return RecursiveTraitSet.Build(traits);
        }
    }
}
