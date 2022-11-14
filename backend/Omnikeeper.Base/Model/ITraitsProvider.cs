using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITraitsProvider
    {
        Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold, Action<string> errorF);

        Task<DateTimeOffset?> GetLatestChangeToActiveDataTraits(IModelContext trans, TimeThreshold timeThreshold);
    }

    public static class TraitsProviderExtensions
    {
        public static async Task<IDictionary<string, ITrait>> GetActiveTraits(this ITraitsProvider traitsProvider, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await traitsProvider.GetActiveTraits(trans, timeThreshold, (_) => { });
        }

        public static async Task<IDictionary<string, ITrait>> GetActiveTraitsByIDs(this ITraitsProvider traitsProvider, IEnumerable<string> IDs, IModelContext trans, TimeThreshold timeThreshold)
        {
            if (IDs.IsEmpty())
                return ImmutableDictionary<string, ITrait>.Empty;

            // TODO: can be done more efficiently?
            var ts = await traitsProvider.GetActiveTraits(trans, timeThreshold, _ => { });

            var foundTraits = ts.Where(t => IDs.Contains(t.Key)).ToDictionary(t => t.Key, t => t.Value);
            if (foundTraits.Count() < IDs.Count())
                throw new Exception($"Encountered unknown trait(s): {string.Join(",", IDs.Except(foundTraits.Select(t => t.Key)))}");
            return foundTraits;
        }

        public static async Task<ITrait?> GetActiveTrait(this ITraitsProvider traitsProvider, string traitID, IModelContext trans, TimeThreshold timeThreshold)
        {
            // TODO: can be done more efficiently? here we get ALL traits, just to select a single one... but the flattening is necessary
            var ts = await traitsProvider.GetActiveTraits(trans, timeThreshold, _ => { });

            if (ts.TryGetValue(traitID, out var trait))
                return trait;
            return null;
        }
    }
}
