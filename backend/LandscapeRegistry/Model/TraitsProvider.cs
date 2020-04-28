using Landscape.Base.Entity;
using Landscape.Base.Model;
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
        //private readonly IServiceProvider SP;
        //public TraitsProvider(IServiceProvider sp)
        //{
        //    SP = sp;
        //}

        public async Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans)
        {
            //using var scope = SP.CreateScope();
            //var predicateModel = scope.ServiceProvider.GetRequiredService<IPredicateModel>();
            //var ciModel = scope.ServiceProvider.GetRequiredService<ICIModel>();
            //var predicates = await predicateModel.GetPredicates(trans, null, AnchorStateFilter.All);

            // TODO: move somewhere else
            var traits = new List<Trait>()
                {
                    // hosts
                    Trait.Build("host", new List<TraitAttribute>() {
                        TraitAttribute.Build("hostname",
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    }),
                    Trait.Build("windows_host", new List<TraitAttribute>() {
                        TraitAttribute.Build("hostname", // TODO: dependent traits (this ci can only be a windows host if it is also a host
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("system",
                            CIAttributeTemplate.BuildFromParams("system", AttributeValueType.Text, false, CIAttributeValueConstraintTextRegex.Build(new Regex(@"Windows", RegexOptions.IgnoreCase)))
                        )
                    }),
                    Trait.Build("linux_host", new List<TraitAttribute>() {
                        TraitAttribute.Build("hostname", // TODO: dependent traits (this ci can only be a windows host if it is also a host
                            CIAttributeTemplate.BuildFromParams("hostname", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        ),
                        TraitAttribute.Build("system",
                            CIAttributeTemplate.BuildFromParams("system", AttributeValueType.Text, false, CIAttributeValueConstraintTextRegex.Build(new Regex(@"Linux", RegexOptions.IgnoreCase)))
                        )
                    }),

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
        }
        public async Task<IImmutableDictionary<string, Trait>> GetTraits(NpgsqlTransaction trans)
        {
            if (cached == null)
            {
                cached = await TP.GetTraits(trans);
            }
            return cached;
        }
    }
}
