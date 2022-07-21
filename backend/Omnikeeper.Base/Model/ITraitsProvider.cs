using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Omnikeeper.Base.Model
{
    public interface ITraitsProvider
    {
        Task<IDictionary<string, ITrait>> GetActiveTraits(IModelContext trans, TimeThreshold timeThreshold, Action<string> errorF);
        Task<ITrait?> GetActiveTrait(string traitID, IModelContext trans, TimeThreshold timeThreshold);

        Task<IDictionary<string, ITrait>> GetActiveTraitsByIDs(IEnumerable<string> IDs, IModelContext trans, TimeThreshold timeThreshold);

        Task<DateTimeOffset?> GetLatestChangeToActiveDataTraits(IModelContext trans, TimeThreshold timeThreshold);
    }

    public static class TraitsProviderExtensions
    {
        public static async Task<IDictionary<string, ITrait>> GetActiveTraits(this ITraitsProvider traitsProvider, IModelContext trans, TimeThreshold timeThreshold)
        {
            return await traitsProvider.GetActiveTraits(trans, timeThreshold, (_) => { });
        }
    }
}
