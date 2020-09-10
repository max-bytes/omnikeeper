using FluentAssertions;
using Landscape.Base.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tests.Integration.Model 
{
    class TraitModelTest // TODO: this (TraitModelTest) is not the best name
    {
        [Test]
        public void TestTraitSerialization()
        {
            var traitset = TraitSet.Build(
                Trait.Build("host", new List<TraitAttribute>() {
                    TraitAttribute.Build("hostname",
                        CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    )
                }),
                Trait.Build("windows_host", new List<TraitAttribute>() {
                    TraitAttribute.Build("os_family",
                        CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                            CIAttributeValueConstraintTextRegex.Build(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                    )
                }, requiredTraits: new string[] { "host" }),

                Trait.Build("linux_host", new List<TraitAttribute>() {
                    TraitAttribute.Build("os_family",
                        CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                            CIAttributeValueConstraintTextRegex.Build(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                    )
                }, requiredTraits: new string[] { "host" }),
                Trait.Build("linux_block_device", new List<TraitAttribute>() {
                    TraitAttribute.Build("device",
                        CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    ),
                    TraitAttribute.Build("mount",
                        CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    )
                }),
                Trait.Build("linux_network_interface", new List<TraitAttribute>() {
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
                Trait.Build("ansible_can_deploy_to_it",
                    new List<TraitAttribute>() {
                        TraitAttribute.Build("hostname",
                            CIAttributeTemplate.BuildFromParams("ipAddress", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
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
                )
            );

            var json = TraitsProvider.TraitSetSerializer.SerializeToString(traitset);

            var x = TraitsProvider.TraitSetSerializer.Deserialize(json);

            x.Should().BeEquivalentTo(traitset);
        }
    }
}
