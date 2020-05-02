using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        public async Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            Console.WriteLine("Getting traits...");
            // TODO: move somewhere else
            // TODO: consider time
            var traits = new List<Trait>()
                {
                // hosts
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

                    // applications
                    Trait.Build("application", new List<TraitAttribute>() {
                        TraitAttribute.Build("name",
                            CIAttributeTemplate.BuildFromParams("application_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),

                    // automation / ansible
                    Trait.Build("ansible_can_deploy_to_it",
                        new List<TraitAttribute>() {
                            TraitAttribute.Build("hostname", // TODO: make this an anyOf[CIAttributeTemplate], or use dependent trait host
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
                                RelationTemplate.Build("has_ansible_group", new string[] {"Ansible Host Group" }, 1, null)
                            )
                        }
                    ),

                    // monitoring / naemon
                    Trait.Build("monitoring_check_module", new List<TraitAttribute>() {
                            TraitAttribute.Build("commands",
                                CIAttributeTemplate.BuildFromParams("monitoring.commands", AttributeValueType.Text, true, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        }),
                    Trait.Build("naemon_instance", new List<TraitAttribute>() {
                            TraitAttribute.Build("name",
                                CIAttributeTemplate.BuildFromParams("monitoring.naemon.instance_name", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                            )
                        })
                };
            return traits.ToImmutableDictionary(t => t.Name);
        }
    }

    public class CachedTraitsProvider : ITraitsProvider
    {
        private readonly TraitsProvider TP;
        private IImmutableDictionary<string, Trait> cached;
        public CachedTraitsProvider(TraitsProvider tp)
        {
            TP = tp;
            cached = null;
            Console.WriteLine("Created cached traits provider");
        }
        public async Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            Console.WriteLine("Getting cached traits...");
            // TODO: consider time
            if (cached == null)
            {
                Console.WriteLine("Cache miss");
                cached = await TP.GetTraits(trans, timeThreshold);
            }
            return cached;
        }
    }
}
