using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
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
