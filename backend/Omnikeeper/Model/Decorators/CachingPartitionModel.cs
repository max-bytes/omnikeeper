using Omnikeeper.Base.Model;
using Omnikeeper.Base.Service;
using Omnikeeper.Base.Utils;
using Omnikeeper.Base.Utils.ModelContext;
using System;
using System.Threading.Tasks;

namespace Omnikeeper.Model.Decorators
{
    public class CachingPartitionModel : IPartitionModel
    {
        private IPartitionModel Model { get; }

        public CachingPartitionModel(IPartitionModel model)
        {
            Model = model;
        }

        public async Task<DateTimeOffset> GetLatestPartitionIndex(TimeThreshold timeThreshold, IModelContext trans)
        {
            // HACK: we are using a single-item tuple to get ref type semantics
            var (item, hit) = await trans.GetOrCreateCachedValueAsync(CacheKeyService.LatestPartitionIndex(), async () =>
            {
                return Tuple.Create(await Model.GetLatestPartitionIndex(timeThreshold, trans));
            });
            return item.Item1;
        }

        public async Task StartNewPartition(TimeThreshold timeThreshold, IModelContext trans)
        {
            await Model.StartNewPartition(timeThreshold, trans);

            // nuke the whole cache!
            // TODO, HACK, NOTE: we'd like to be more specific here and only clear attributes and relations from the cache... 
            // but that's not supported by the cache :(
            trans.ClearCache();
        }
    }
}

