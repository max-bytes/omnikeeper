using Landscape.Base.Entity;
using Landscape.Base.Model;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public class TemplatesProvider : ITemplatesProvider
    {
        private readonly IServiceProvider SP;
        public TemplatesProvider(IServiceProvider sp)
        {
            SP = sp;
        }

        public async Task<Templates> GetTemplates(NpgsqlTransaction trans)
        {
            using var scope = SP.CreateScope();
            return await Templates.Build(scope.ServiceProvider.GetRequiredService<CIModel>(), scope.ServiceProvider.GetRequiredService<ITraitsProvider>(), trans);
        }
    }

    public class CachedTemplatesProvider : ITemplatesProvider
    {
        private readonly TemplatesProvider TP;
        private Templates cached;
        public CachedTemplatesProvider(TemplatesProvider tp)
        {
            TP = tp;
            cached = null;
        }
        public async Task<Templates> GetTemplates(NpgsqlTransaction trans)
        {
            if (cached == null)
            {
                cached = await TP.GetTemplates(trans);
            }
            return cached;
        }
    }
}
