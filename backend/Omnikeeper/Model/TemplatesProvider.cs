using Omnikeeper.Base.Entity;
using Omnikeeper.Base.Model;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;
using System.Threading.Tasks;

namespace Omnikeeper.Model
{
    public class TemplatesProvider : ITemplatesProvider
    {
        public async Task<Templates> GetTemplates(NpgsqlTransaction trans)
        {
            return await Templates.Build();
        }
    }

    public class CachedTemplatesProvider : ITemplatesProvider
    {
        private readonly ITemplatesProvider TP;
        private readonly IMemoryCache memoryCache;
        public CachedTemplatesProvider(ITemplatesProvider tp, IMemoryCache memoryCache)
        {
            TP = tp;
            this.memoryCache = memoryCache;
        }
        public async Task<Templates> GetTemplates(NpgsqlTransaction trans)
        {
            return await memoryCache.GetOrCreateAsync("templates", async (ce) =>
            {
                return await TP.GetTemplates(trans);
            });
        }
    }
}
