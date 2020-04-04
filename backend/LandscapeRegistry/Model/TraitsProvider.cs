using Landscape.Base.Entity;
using Landscape.Base.Model;
using LandscapeRegistry.Entity;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Model.Cached;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TraitsProvider : ITraitsProvider
    {
        private readonly IServiceProvider SP;
        public TraitsProvider(IServiceProvider sp)
        {
            SP = sp;
        }

        public async Task<Traits> GetTraits(NpgsqlTransaction trans)
        {
            using var scope = SP.CreateScope();

            // TODO: move somewhere else
            var traits = new List<Trait>()
                {
                    Trait.Build("ansible_can_deploy_to_it", new List<TraitAttribute>()
                    {
                        TraitAttribute.Build(
                            CIAttributeTemplate.BuildFromParams("ipAddress", "this is a description", AttributeValueType.Text, false, CIAttributeValueConstraintTextLength.Build(1, null))
                        )
                    })
                };
            return await Traits.Build(traits, trans);
        }
    }

    public class CachedTraitsProvider : ITraitsProvider
    {
        private readonly TraitsProvider TP;
        private Traits cached;
        public CachedTraitsProvider(TraitsProvider tp)
        {
            TP = tp;
            cached = null;
        }
        public async Task<Traits> GetTraits(NpgsqlTransaction trans)
        {
            if (cached == null)
            {
                cached = await TP.GetTraits(trans);
            }
            return cached;
        }
    }
}
