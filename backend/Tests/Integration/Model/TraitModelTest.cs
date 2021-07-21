﻿using FluentAssertions;
using NUnit.Framework;
using Omnikeeper.Base.Entity;
using Omnikeeper.Entity.AttributeValues;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Tests.Integration.Model
{
    class TraitModelTest // TODO: this (TraitModelTest) is not the best name
    {
        [Test]
        public void TestTraitSerialization()
        {
            var traits = new List<RecursiveTrait>() {
                new RecursiveTrait("host", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    new TraitAttribute("hostname",
                        CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    )
                }),
                new RecursiveTrait("host_windows", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    new TraitAttribute("os_family",
                        CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                            new CIAttributeValueConstraintTextRegex(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                    )
                }, requiredTraits: new string[] { "host" }),

                new RecursiveTrait("host_linux", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    new TraitAttribute("os_family",
                        CIAttributeTemplate.BuildFromParams("os_family", AttributeValueType.Text, false,
                            new CIAttributeValueConstraintTextRegex(new Regex(@"(RedHat|CentOS|Debian|Suse|Gentoo|Archlinux|Mandrake)", RegexOptions.IgnoreCase)))
                    )
                }, requiredTraits: new string[] { "host" }),
                new RecursiveTrait("linux_block_device", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
                    new TraitAttribute("device",
                        CIAttributeTemplate.BuildFromParams("device", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    ),
                    new TraitAttribute("mount",
                        CIAttributeTemplate.BuildFromParams("mount", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                    )
                }),
                new RecursiveTrait("linux_network_interface", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>() {
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
                new RecursiveTrait("ansible_can_deploy_to_it", new TraitOriginV1(TraitOriginType.Data),
                    new List<TraitAttribute>() {
                        new TraitAttribute("hostname",
                            CIAttributeTemplate.BuildFromParams("ipAddress", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
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
                )
            };

            foreach(var rt in traits)
            {
                var json = RecursiveTrait.Serializer.SerializeToString(rt);
                var x = RecursiveTrait.Serializer.Deserialize(json);
                x.Should().BeEquivalentTo(rt);
            }
        }
    }
}
