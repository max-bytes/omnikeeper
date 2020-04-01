using LandscapeRegistry.Entity.Template;
using LandscapeRegistry.Model.Cached;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LandscapeRegistry.Model
{
    public interface ITemplatesProvider
    {
        public Task<Templates> GetTemplates(NpgsqlTransaction trans);
    }

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
            return await Templates.Build(scope.ServiceProvider.GetRequiredService<CIModel>(), scope.ServiceProvider.GetRequiredService<CachedLayerModel>(), trans);
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
