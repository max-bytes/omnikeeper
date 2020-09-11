using Landscape.Base.Entity;
using Landscape.Base.Model;
using Landscape.Base.Service;
using Landscape.Base.Utils;
using LandscapeRegistry.Entity.AttributeValues;
using LandscapeRegistry.Service;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Tests.Integration.Model.Mocks
{
    public class MockedTraitsProviderWithLoop : ITraitsProvider
    {
        public async Task<TraitSet> GetActiveTraitSet(NpgsqlTransaction trans, TimeThreshold timeThreshold)
        {
            var r = new List<RecursiveTrait>() {
                RecursiveTrait.Build("test_trait_1", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a4",
                        CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_2" }),
                RecursiveTrait.Build("test_trait_2", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a4",
                        CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false)
                    ),
                    TraitAttribute.Build("a2",
                        CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_3" }),
                RecursiveTrait.Build("test_trait_3", new List<TraitAttribute>()
                {
                    TraitAttribute.Build("a1",
                        CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_1" })
            };

            // TODO: should we really flatten here in a mocked class?
            return TraitSet.Build(RecursiveTraitService.FlattenDependentTraits(r.ToImmutableDictionary(r => r.Name)));
        }
    }
}
