using Landscape.Base;
using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly IDictionary<string, Trait> traits = new Dictionary<string, Trait>();

        public void Register(string source, Trait[] t)
        { // TODO: consider source
            foreach (var trait in t)
                traits.Add(trait.Name, trait);
        }

        public IImmutableDictionary<string, Trait> GetTraits()
        {
            return traits.ToImmutableDictionary();
        }
    }

    public class CachedTraitsProvider : ITraitsProvider
    {
        private readonly ITraitsProvider TP;
        private readonly IMemoryCache memoryCache;

        public CachedTraitsProvider(ITraitsProvider tp, IMemoryCache memoryCache)
        {
            TP = tp;
            this.memoryCache = memoryCache;
        }
        public IImmutableDictionary<string, Trait> GetTraits()
        {
            return memoryCache.GetOrCreate("traits", (ce) =>
            {
                return TP.GetTraits();
            });
        }

        public void Register(string source, Trait[] t)
        {
            TP.Register(source, t);
        }
    }

    public class TraitsSetup {
        private readonly IServiceProvider sp;

        public TraitsSetup(IServiceProvider sp)
        {
            this.sp = sp;
        }

        public void Setup()
        {
            var computeLayerBrains = sp.GetServices<IComputeLayerBrain>();
            var traitsProvider = sp.GetService<ITraitsProvider>();
            traitsProvider.Register("default", DefaultTraits.Get());
            foreach (var clb in computeLayerBrains)
                traitsProvider.Register($"CLB-{clb.Name}", clb.DefinedTraits);
        }
    }

    public static class DefaultTraits
    {
        public static Trait[] Get()
        {
            var traits = new Trait[]
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
                                RelationTemplate.Build("has_ansible_group", new string[] {"Ansible Host Group" }, 1, null)
                            )
                        }
                    ),
                };

            return traits;
        }
    }
}
