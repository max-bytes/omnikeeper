using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using Omnikeeper.Entity.AttributeValues;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Integration.Model.Mocks
{
    public class MockedTraitsProvider : ITraitsProvider
    {
        public async Task<ITrait?> GetActiveTrait(string traitName, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ts = await GetActiveTraits(trans, timeThreshold);

            if (ts.TryGetValue(traitName, out var trait))
                return trait;
            return null;
        }

        public Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold)
        {
            var r = new List<RecursiveTrait>() {
                new RecursiveTrait("test_trait_1", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
                {
                    new TraitAttribute("a4",
                        CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                new RecursiveTrait("test_trait_2", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
                {
                    new TraitAttribute("a4",
                        CIAttributeTemplate.BuildFromParams("a4", AttributeValueType.Text, false, false)
                    ),
                    new TraitAttribute("a2",
                        CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                new RecursiveTrait("test_trait_3", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
                {
                    new TraitAttribute("a1",
                        CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false, false)
                    )
                }, new List<TraitAttribute>() { }, new List<TraitRelation>() { }),
                new RecursiveTrait("test_trait_4", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
                {
                    new TraitAttribute("a1",
                        CIAttributeTemplate.BuildFromParams("a1", AttributeValueType.Text, false, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_1" }),
                new RecursiveTrait("test_trait_5", new TraitOriginV1(TraitOriginType.Data), new List<TraitAttribute>()
                {
                    new TraitAttribute("a2",
                        CIAttributeTemplate.BuildFromParams("a2", AttributeValueType.Text, false, false)
                    )
                }, requiredTraits: new List<string>() { "test_trait_4" })
            };

            // TODO: should we really flatten here in a mocked class?
            var t = RecursiveTraitService.FlattenRecursiveTraits(r);
            var tt = (IDictionary<string, ITrait>)t.ToDictionary(t => t.Key, t => (ITrait)t.Value);
            return Task.FromResult(tt);
        }

        public async Task<IDictionary<string, ITrait>> GetActiveTraitsByIDs(IEnumerable<string> IDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            var ts = await GetActiveTraits(trans, timeThreshold);

            var foundTraits = ts.Where(t => IDs.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);
            if (foundTraits.Count() < IDs.Count())
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", IDs.Except(foundTraits.Select(t => t.Key)))}");
            return foundTraits;
        }
    }
}
